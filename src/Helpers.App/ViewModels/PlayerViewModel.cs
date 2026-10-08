using System.Collections.ObjectModel;
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
    private ReadingSession? _session;
    private ReadingState _state = ReadingState.Idle;
    private int _currentIndex = -1;
    private string _currentText = string.Empty;
    private float _speed = 1.0f;
    private SpeechVoice? _voice;
    private bool _isExpanded;

    public PlayerViewModel(IReadOnlyList<SpeechVoice> voices)
    {
        Voices = voices;
        _voice = voices.Count > 0 ? voices[0] : null;
    }

    public event Action<float>? SpeedChanged;

    public event Action<SpeechVoice>? VoiceChanged;

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
        private set => Set(ref _currentText, value);
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
    }

    public void Detach()
    {
        if (_session is null)
        {
            return;
        }

        _session.StateChanged -= OnStateChanged;
        _session.SegmentChanged -= OnSegmentChanged;
        _session = null;
    }

    /// <summary>Raised when the user wants to play again after the reading ended, with the segment to start from.</summary>
    public event Action<int>? RestartRequested;

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
}
