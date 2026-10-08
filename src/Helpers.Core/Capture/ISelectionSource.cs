namespace Helpers.Core.Capture;

public enum SelectionOutcome
{
    /// <summary>Text was captured.</summary>
    Text,

    /// <summary>The app in front had nothing selected, or gave nothing back.</summary>
    Nothing,

    /// <summary>The app in front runs as administrator and this app doesn't, so Windows blocks the read.</summary>
    Elevated,

    /// <summary>The focused control is a password field. Never read those.</summary>
    Password,

    /// <summary>Something went wrong; the message says what.</summary>
    Failed,
}

/// <param name="Outcome">What happened.</param>
/// <param name="Text">The selected text when the outcome is <see cref="SelectionOutcome.Text"/>.</param>
/// <param name="Source">Which route produced it, such as "UI Automation" or "clipboard", for the milestone log.</param>
/// <param name="Message">Detail for a toast when something went wrong.</param>
public sealed record SelectionResult(SelectionOutcome Outcome, string? Text = null, string? Source = null, string? Message = null)
{
    public static SelectionResult Nothing() => new(SelectionOutcome.Nothing);

    public static SelectionResult Elevated() => new(SelectionOutcome.Elevated);

    public static SelectionResult Password() => new(SelectionOutcome.Password);

    public static SelectionResult Failed(string message) => new(SelectionOutcome.Failed, Message: message);

    public static SelectionResult Found(string text, string source) => new(SelectionOutcome.Text, text, source);
}

/// <summary>Reads whatever text is selected in the app that has focus.</summary>
public interface ISelectionSource
{
    Task<SelectionResult> CaptureAsync(CancellationToken cancellationToken);
}
