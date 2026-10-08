using Helpers.Core.Input;

namespace Helpers.Tests.Input;

public class SelectionGestureDetectorTests
{
    private static (SelectionGestureDetector Detector, List<GesturePoint> Selections, int[] Dismissals) Make()
    {
        var detector = new SelectionGestureDetector();
        var selections = new List<GesturePoint>();
        var dismissals = new int[1];
        detector.SelectionMade += p => selections.Add(p);
        detector.Dismissed += () => dismissals[0]++;
        return (detector, selections, dismissals);
    }

    [Fact]
    public void DragSelectsAndReportsTheReleasePoint()
    {
        var (d, selections, _) = Make();

        d.MouseDown(PointerButton.Left, new GesturePoint(100, 100), 0);
        d.MouseUp(PointerButton.Left, new GesturePoint(180, 104), 300);

        Assert.Equal([new GesturePoint(180, 104)], selections);
    }

    [Fact]
    public void ASingleClickIsNotASelection()
    {
        var (d, selections, _) = Make();

        d.MouseDown(PointerButton.Left, new GesturePoint(100, 100), 0);
        d.MouseUp(PointerButton.Left, new GesturePoint(101, 100), 80);

        Assert.Empty(selections);
    }

    [Fact]
    public void DoubleClickSelectsAWord()
    {
        var (d, selections, _) = Make();

        d.MouseDown(PointerButton.Left, new GesturePoint(100, 100), 0);
        d.MouseUp(PointerButton.Left, new GesturePoint(100, 100), 80);
        d.MouseDown(PointerButton.Left, new GesturePoint(101, 100), 200);
        d.MouseUp(PointerButton.Left, new GesturePoint(101, 100), 280);

        Assert.Single(selections);
    }

    [Fact]
    public void TripleClickSelectsAgain()
    {
        var (d, selections, _) = Make();

        for (var i = 0; i < 3; i++)
        {
            d.MouseDown(PointerButton.Left, new GesturePoint(100, 100), i * 200);
            d.MouseUp(PointerButton.Left, new GesturePoint(100, 100), i * 200 + 80);
        }

        Assert.Equal(2, selections.Count);
    }

    [Fact]
    public void TwoSlowClicksAreNotADoubleClick()
    {
        var (d, selections, _) = Make();

        d.MouseDown(PointerButton.Left, new GesturePoint(100, 100), 0);
        d.MouseUp(PointerButton.Left, new GesturePoint(100, 100), 80);
        d.MouseDown(PointerButton.Left, new GesturePoint(100, 100), 900);
        d.MouseUp(PointerButton.Left, new GesturePoint(100, 100), 980);

        Assert.Empty(selections);
    }

    [Fact]
    public void RightButtonDoesNothingButDismiss()
    {
        var (d, selections, dismissals) = Make();

        d.MouseDown(PointerButton.Right, new GesturePoint(100, 100), 0);
        d.MouseUp(PointerButton.Right, new GesturePoint(200, 100), 300);

        Assert.Empty(selections);
        Assert.Equal(1, dismissals[0]);
    }

    [Fact]
    public void AnyPressWheelOrKeyDismisses()
    {
        var (d, _, dismissals) = Make();

        d.MouseDown(PointerButton.Left, new GesturePoint(0, 0), 0);
        d.Wheel();
        d.KeyPressed();

        Assert.Equal(3, dismissals[0]);
    }

    [Fact]
    public void ACancelledPressNeverBecomesADrag()
    {
        var (d, selections, _) = Make();

        d.MouseDown(PointerButton.Left, new GesturePoint(100, 100), 0);
        d.CancelPress();
        d.MouseUp(PointerButton.Left, new GesturePoint(300, 100), 300);

        Assert.Empty(selections);
    }
}
