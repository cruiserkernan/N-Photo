using Avalonia;

namespace App.Presentation.Controllers;

public static class PanZoomController
{
    public static bool TryZoomAtPointer(
        Point pointerScreen,
        double wheelDelta,
        double currentZoom,
        Vector currentPan,
        double minZoom,
        double maxZoom,
        double zoomStepFactor,
        out double nextZoom,
        out Vector nextPan)
    {
        nextZoom = currentZoom;
        nextPan = currentPan;

        if (Math.Abs(wheelDelta) < double.Epsilon)
        {
            return false;
        }

        var worldAtPointer = ScreenToWorld(pointerScreen, currentPan, currentZoom);
        var zoomFactor = wheelDelta > 0
            ? zoomStepFactor
            : 1 / zoomStepFactor;

        var candidateZoom = Math.Clamp(currentZoom * zoomFactor, minZoom, maxZoom);
        if (Math.Abs(candidateZoom - currentZoom) < 0.0001)
        {
            return false;
        }

        nextZoom = candidateZoom;
        nextPan = new Vector(
            pointerScreen.X - (worldAtPointer.X * nextZoom),
            pointerScreen.Y - (worldAtPointer.Y * nextZoom));
        return true;
    }

    public static Point ScreenToWorld(Point screenPoint, Vector panOffset, double zoomScale)
    {
        return new Point(
            (screenPoint.X - panOffset.X) / zoomScale,
            (screenPoint.Y - panOffset.Y) / zoomScale);
    }

    public static Point WorldToScreen(Point worldPoint, Vector panOffset, double zoomScale)
    {
        return new Point(
            (worldPoint.X * zoomScale) + panOffset.X,
            (worldPoint.Y * zoomScale) + panOffset.Y);
    }
}
