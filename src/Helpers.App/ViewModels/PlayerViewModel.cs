using System.Collections.ObjectModel;
using System.Diagnostics;
using Avalonia.Threading;
using Helpers.Core.Speech;
using Helpers.Core.Text;

namespace Helpers.App.ViewModels;

/// <summary>One line of the reading view.</summary>
public sealed class SegmentItem(int index, SpeechSegment segment) : ObservableObject
{
    private bool _isCurrent;
    private bool _isDone;

    public int Index { get; } = index;

    public string Display { get; } = segment.Display;

    public bool IsHeading { get; } = segment.Kind == SegmentKind.Heading;

    public bool IsListItem { get; } = segment.Kind == SegmentKind.ListItem;

    public bool IsCurrent
    {
        get => _isCurrent;
        set => Set(ref _isCurrent, value);
    }

    public bool IsDone
    {
        get => _isDone;
        set => Set(ref _isDone, value);
    }
}

/// <summary>What the player shows. Driven by a <see cref="ReadingSession"/>; all updates land on the UI thread.</summary>
public sealed class PlayerViewModel : ObservableObject
{
    private readonly DispatcherTimer _wordClock;
    private readonly Stopwatch _elapsed = new();
    private ReadingSession? _session;
    private ReadingState _state = ReadingState.Idle;
    private int _currentIndex = -1;
    private string _currentText = string.Empty;
    private string[] _words = [];
    private double[] _wordEnds = [];
    private TimeSpan _clipDuration;
    private int _currentWordIndex = -1;
    private float _speed = 1.0f;
    private SpeechVoice? _voice;
    private bool _isExpanded;

    public PlayerViewModel(IReadOnlyList<SpeechVoice> voices)
    {
        Voices = voices;
        _voice = voices.Count > 0 ? voices[0] : null;
        _wordClock = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Background, (_, _) => TickWord());
    }

    public event Action<float>? SpeedChanged;

    public event Action<SpeechVoice>? VoiceChanged;

    /// <summary>Raised when the user wants to play again after the reading ended, with the segment to start from.</summary>
    public event Action<int>? RestartRequested;

    public IReadOnlyList<SpeechVoice> Voices { get; }

    public ObservableCollection<SegmentItem> Segments { get; } = [];

    public ReadingState State
    {
        get => _state;
        private set
        {
            if (Set(ref _state, value))
            {
                Raise(nameof(ShowPause));
                Raise(nameof(ShowPlay));
                Raise(nameof(IsLoading));
                Raise(nameof(StatusText));
                if (value == ReadingState.Loading)
                {
                    CurrentText = "Loading voice…";
                }

                if (value == ReadingState.Playing)
                {
                    _elapsed.Start();
                }
                else
                {
                    _elapsed.Stop();
                }
            }
        }
    }

    public bool ShowPause => State == ReadingState.Playing;

    public bool ShowPlay => !ShowPause;

    public bool IsLoading => State == ReadingState.Loading;

    public int CurrentIndex
    {
        get => _currentIndex;
        private set
        {
            if (Set(ref _currentIndex, value))
            {
                Raise(nameof(StatusText));
            }
        }
    }

    public string CurrentText
    {
        get => _currentText;
        private set
        {
            if (Set(ref _currentText, value))
            {
                _words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                _wordEnds = WordEnds(_words);
                CurrentWordIndex = -1;
            }
        }
    }

    /// <summary>The words of the current sentence, split on spaces.</summary>
    public IReadOnlyList<string> Words => _words;

    /// <summary>Which word the voice is estimated to be on, or -1 before the audio starts.</summary>
    public int CurrentWordIndex
    {
        get => _currentWordIndex;
        private set => Set(ref _currentWordIndex, value);
    }

    public string StatusText =>
        State switch
        {
            ReadingState.Loading => "Loading voice…",
            ReadingState.Finished => "Finished",
            ReadingState.Failed => "Couldn't read that",
            _ when Segments.Count > 0 && CurrentIndex >= 0 => $"Sentence {CurrentIndex + 1} of {Segments.Count}",
            _ => string.Empty,
        };

    public float Speed
    {
        get => _speed;
        set
        {
            var rounded = MathF.Round(Math.Clamp(value, 0.5f, 2.0f), 1);
            if (Set(ref _speed, rounded))
            {
                Raise(nameof(SpeedText));
                if (_session is not null)
                {
                    _session.Speed = rounded;
                }

                SpeedChanged?.Invoke(rounded);
            }
        }
    }

    public string SpeedText => $"{Speed:0.0}×";

    public SpeechVoice? Voice
    {
        get => _voice;
        set
        {
            if (value is not null && Set(ref _voice, value))
            {
                if (_session is not null)
                {
                    _session.Voice = value;
                }

                VoiceChanged?.Invoke(value);
            }
        }
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => Set(ref _isExpanded, value);
    }

    public void Attach(ReadingSession session)
    {
        Detach();
        _session = session;

        Segments.Clear();
        for (var i = 0; i < session.Segments.Count; i++)
        {
            Segments.Add(new SegmentItem(i, session.Segments[i]));
        }

        CurrentIndex = -1;
        CurrentText = string.Empty;
        State = session.State;

        session.StateChanged += OnStateChanged;
        session.SegmentChanged += OnSegmentChanged;
        session.PlaybackStarted += OnPlaybackStarted;
        _wordClock.Start();
    }

    public void Detach()
    {
        _wordClock.Stop();
        _elapsed.Reset();
        if (_session is null)
        {
            return;
        }

        _session.StateChanged -= OnStateChanged;
        _session.SegmentChanged -= OnSegmentChanged;
        _session.PlaybackStarted -= OnPlaybackStarted;
        _session = null;
    }

    private bool IsOver => _session is null || _session.IsOver;

    public void TogglePause()
    {
        if (IsOver)
        {
            if (Segments.Count > 0)
            {
                RestartRequested?.Invoke(0);
            }

            return;
        }

        _session!.TogglePause();
    }

    public void Stop() => _session?.Stop();

    public void SkipBack()
    {
        if (IsOver)
        {
            if (Segments.Count > 0)
            {
                RestartRequested?.Invoke(Math.Max(0, Segments.Count - 1));
            }

            return;
        }

        _session!.SkipBack();
    }

    public void SkipForward()
    {
        if (!IsOver)
        {
            _session!.SkipForward();
        }
    }

    public void JumpTo(int index)
    {
        if (IsOver)
        {
            RestartRequested?.Invoke(index);
            return;
        }

        _session!.JumpTo(index);
    }

    private void OnStateChanged(ReadingState state) =>
        Dispatcher.UIThread.Post(() =>
        {
            State = state;
            if (state == ReadingState.Finished)
            {
                foreach (var item in Segments)
                {
                    item.IsCurrent = false;
                    item.IsDone = true;
                }

                CurrentWordIndex = -1;
            }
        });

    private void OnSegmentChanged(int index) =>
        Dispatcher.UIThread.Post(() =>
        {
            CurrentIndex = index;
            for (var i = 0; i < Segments.Count; i++)
            {
                Segments[i].IsCurrent = i == index;
                Segments[i].IsDone = i < index;
            }

            if (index >= 0 && index < Segments.Count)
            {
                CurrentText = Segments[index].Display;
            }
        });

    private void OnPlaybackStarted(int index, TimeSpan duration) =>
        Dispatcher.UIThread.Post(() =>
        {
            _clipDuration = duration;
            _elapsed.Restart();
            if (State != ReadingState.Playing)
            {
                _elapsed.Stop();
            }

            CurrentWordIndex = _words.Length > 0 ? 0 : -1;
        });

    /// <summary>Estimates the word being spoken from time elapsed, weighting words by length and punctuation.</summary>
    private void TickWord()
    {
        if (_words.Length == 0 || _clipDuration <= TimeSpan.Zero || !_elapsed.IsRunning)
        {
            return;
        }

        var fraction = Math.Clamp(_elapsed.Elapsed.TotalSeconds / _clipDuration.TotalSeconds, 0, 1);
        var index = Array.FindIndex(_wordEnds, end => fraction < end);
        if (index < 0)
        {
            index = _words.Length - 1;
        }

        if (index != _currentWordIndex)
        {
            CurrentWordIndex = index;
        }
    }

    /// <summary>Cumulative fraction of the clip at which each word is expected to end.</summary>
    private static double[] WordEnds(string[] words)
    {
        if (words.Length == 0)
        {
            return [];
        }

        var weights = new double[words.Length];
        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i];
            var letters = word.Count(char.IsLetterOrDigit);
            var pause = word.EndsWith('.') || word.EndsWith('!') || word.EndsWith('?') || word.EndsWith(':') ? 4
                : word.EndsWith(',') || word.EndsWith(';') ? 2
                : 0;
            weights[i] = Math.Max(1, letters) + 1 + pause;
        }

        var total = weights.Sum();
        var ends = new double[words.Length];
        var running = 0.0;
        for (var i = 0; i < words.Length; i++)
        {
            running += weights[i];
            ends[i] = running / total;
        }

        return ends;
    }
}
