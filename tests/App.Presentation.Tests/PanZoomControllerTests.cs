using App.Presentation.Controllers;
using Avalonia;

namespace App.Presentation.Tests;

public class PanZoomControllerTests
{
    [Fact]
    public void TryZoomAtPointer_AdjustsPanToKeepPointerWorldPositionStable()
    {
        var pointer = new Point(200, 120);
        var startPan = new Vector(10, 6);
        const double startZoom = 1.0;

        var beforeWorld = PanZoomController.ScreenToWorld(pointer, startPan, startZoom);

        var changed = PanZoomController.TryZoomAtPointer(
            pointer,
            wheelDelta: 1,
            currentZoom: startZoom,
            currentPan: startPan,
            minZoom: 0.2,
            maxZoom: 8,
            zoomStepFactor: 1.2,
            out var nextZoom,
            out var nextPan);

        var afterWorld = PanZoomController.ScreenToWorld(pointer, nextPan, nextZoom);

        Assert.True(changed);
        Assert.True(nextZoom > startZoom);
        Assert.Equal(beforeWorld.X, afterWorld.X, 6);
        Assert.Equal(beforeWorld.Y, afterWorld.Y, 6);
    }
}
