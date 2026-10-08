namespace Helpers.App.Services;

/// <summary>One scale factor for every overlay, so the whole app can be made smaller or larger at once.</summary>
public static class UiScale
{
    public static readonly double[] Choices = [0.6, 0.7, 0.8, 0.9, 1.0, 1.1, 1.25];

    public static double Current { get; private set; } = 0.9;

    public static event Action<double>? Changed;

    public static void Set(double scale)
    {
        var clamped = Math.Clamp(scale, 0.5, 1.5);
        if (Math.Abs(clamped - Current) < 0.001)
        {
            return;
        }

        Current = clamped;
        Changed?.Invoke(clamped);
    }
}
