using Helpers.Core.Text;

namespace Helpers.Core.Speech;

public enum ReadingState
{
    Idle,
    Loading,
    Playing,
    Paused,
    Finished,
    Stopped,
    Failed,
}

/// <summary>
/// Reads a list of segments aloud: synthesises one sentence ahead of the one
/// being played, pauses between segments as the pipeline asked, and supports
/// pause, resume, skip and jump from any thread.
/// </summary>
public sealed class ReadingSession : IDisposable
{
    /// <summary>How many segments to synthesise ahead of playback. One keeps memory down.</summary>
    public const int LookAhead = 1;

    private readonly ISpeechEngine _engine;
    private readonly IAudioOutput _output;
    private readonly IReadOnlyList<SpeechSegment> _segments;
    private readonly CancellationTokenSource _sessionCts = new();
    private readonly Dictionary<int, Task<AudioClip>> _ahead = [];
    private readonly object _sync = new();

    private CancellationTokenSource _itemCts;
    private TaskCompletionSource _resumed = Completed();
    private int _index;
    private int? _requested;
    private SpeechVoice _voice;
    private float _speed;
    private ReadingState _state = ReadingState.Idle;

    public ReadingSession(ISpeechEngine engine, IAudioOutput output, IReadOnlyList<SpeechSegment> segments, SpeechVoice voice, float speed, int startIndex = 0)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _segments = segments ?? throw new ArgumentNullException(nameof(segments));
        _voice = voice ?? throw new ArgumentNullException(nameof(voice));
        _speed = speed;
        _index = Math.Clamp(startIndex, 0, Math.Max(0, segments.Count - 1));
        _itemCts = CancellationTokenSource.CreateLinkedTokenSource(_sessionCts.Token);
    }

    /// <summary>True once the session can no longer play: finished, stopped or failed.</summary>
    public bool IsOver => _state is ReadingState.Finished or ReadingState.Stopped or ReadingState.Failed;

    public event Action<ReadingState>? StateChanged;

    /// <summary>Raised with the index of the segment about to be spoken.</summary>
    public event Action<int>? SegmentChanged;

    public event Action<string>? Failed;

    public IReadOnlyList<SpeechSegment> Segments => _segments;

    public ReadingState State => _state;

    public int CurrentIndex
    {
        get
        {
            lock (_sync)
            {
                return _index;
            }
        }
    }

    public SpeechVoice Voice
    {
        get => _voice;
        set
        {
            lock (_sync)
            {
                if (_voice == value)
                {
                    return;
                }

                _voice = value;
                _ahead.Clear();
            }
        }
    }

    public float Speed
    {
        get => _speed;
        set
        {
            lock (_sync)
            {
                if (Math.Abs(_speed - value) < 0.001f)
                {
                    return;
                }

                _speed = value;
                _ahead.Clear();
            }
        }
    }

    /// <summary>Runs the whole reading. Completes when it finishes, is stopped, or fails.</summary>
    public async Task RunAsync()
    {
        var token = _sessionCts.Token;
        try
        {
            if (!_engine.IsLoaded)
            {
                SetState(ReadingState.Loading);
                await _engine.LoadAsync(null, token).ConfigureAwait(false);
            }

            _output.Start(_engine.SampleRate);
            SetState(ReadingState.Playing);

            while (!token.IsCancellationRequested)
            {
                int index;
                lock (_sync)
                {
                    if (_requested is { } target)
                    {
                        _index = Math.Clamp(target, 0, _segments.Count);
                        _requested = null;
                    }

                    index = _index;
                }

                if (index >= _segments.Count)
                {
                    break;
                }

                SegmentChanged?.Invoke(index);

                Task<AudioClip> clipTask;
                lock (_sync)
                {
                    clipTask = GetOrStart(index, token);
                    for (var ahead = 1; ahead <= LookAhead; ahead++)
                    {
                        if (index + ahead < _segments.Count)
                        {
                            GetOrStart(index + ahead, token);
                        }
                    }
                }

                AudioClip clip;
                try
                {
                    clip = await clipTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    continue;
                }

                var itemToken = NewItemToken();
                try
                {
                    await WaitWhilePausedAsync(itemToken).ConfigureAwait(false);
                    await _output.PlayAsync(clip, itemToken).ConfigureAwait(false);
                    await WaitWhilePausedAsync(itemToken).ConfigureAwait(false);
                    await Task.Delay(_segments[index].PauseAfterMs, itemToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    // Skipped or jumped: the loop picks up the requested index.
                }

                lock (_sync)
                {
                    _ahead.Remove(index);
                    if (_requested is null)
                    {
                        _index = index + 1;
                    }
                }
            }

            SetState(token.IsCancellationRequested ? ReadingState.Stopped : ReadingState.Finished);
        }
        catch (OperationCanceledException)
        {
            SetState(ReadingState.Stopped);
        }
        catch (Exception ex)
        {
            Failed?.Invoke(ex.Message);
            SetState(ReadingState.Failed);
        }
        finally
        {
            _output.Stop();
        }
    }

    public void Pause()
    {
        lock (_sync)
        {
            if (_state != ReadingState.Playing)
            {
                return;
            }

            if (_resumed.Task.IsCompleted)
            {
                _resumed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        _output.Pause();
        SetState(ReadingState.Paused);
    }

    public void Resume()
    {
        TaskCompletionSource gate;
        lock (_sync)
        {
            if (_state != ReadingState.Paused)
            {
                return;
            }

            gate = _resumed;
        }

        _output.Resume();
        SetState(ReadingState.Playing);
        gate.TrySetResult();
    }

    public void TogglePause()
    {
        if (_state == ReadingState.Paused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void SkipForward() => JumpTo(CurrentIndex + 1);

    public void SkipBack() => JumpTo(CurrentIndex - 1);

    /// <summary>Moves to a segment. Out-of-range values clamp to the ends.</summary>
    public void JumpTo(int index)
    {
        CancellationTokenSource toCancel;
        lock (_sync)
        {
            _requested = Math.Clamp(index, 0, _segments.Count);
            toCancel = _itemCts;
        }

        if (_state == ReadingState.Paused)
        {
            Resume();
        }

        toCancel.Cancel();
    }

    public void Stop()
    {
        _sessionCts.Cancel();
        _resumed.TrySetResult();
    }

    public void Dispose()
    {
        Stop();
        _sessionCts.Dispose();
    }

    private Task<AudioClip> GetOrStart(int index, CancellationToken token)
    {
        if (_ahead.TryGetValue(index, out var existing))
        {
            return existing;
        }

        var request = new SynthesisRequest(_segments[index].Speak, _voice, _speed);
        var task = _engine.SynthesiseAsync(request, token);
        _ahead[index] = task;
        return task;
    }

    private CancellationToken NewItemToken()
    {
        lock (_sync)
        {
            _itemCts.Dispose();
            _itemCts = CancellationTokenSource.CreateLinkedTokenSource(_sessionCts.Token);
            return _itemCts.Token;
        }
    }

    private async Task WaitWhilePausedAsync(CancellationToken token)
    {
        Task gate;
        lock (_sync)
        {
            gate = _resumed.Task;
        }

        if (!gate.IsCompleted)
        {
            await gate.WaitAsync(token).ConfigureAwait(false);
        }
    }

    private void SetState(ReadingState state)
    {
        bool changed;
        lock (_sync)
        {
            changed = _state != state;
            _state = state;
        }

        if (changed)
        {
            StateChanged?.Invoke(state);
        }
    }

    private static TaskCompletionSource Completed()
    {
        var source = new TaskCompletionSource();
        source.SetResult();
        return source;
    }
}
