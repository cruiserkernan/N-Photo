using Avalonia;

namespace App.Presentation.Controllers;

public sealed class GraphInteractionController
{
    public static (Point BendA, Point BendB) ResolveOrthogonalBends(
        Point sourceAnchor,
        Point targetAnchor,
        bool horizontalFinalSegment)
    {
        if (horizontalFinalSegment)
        {
            var middleX = (sourceAnchor.X + targetAnchor.X) / 2;
            return (
                new Point(middleX, sourceAnchor.Y),
                new Point(middleX, targetAnchor.Y));
        }

        var middleY = (sourceAnchor.Y + targetAnchor.Y) / 2;
        return (
            new Point(sourceAnchor.X, middleY),
            new Point(targetAnchor.X, middleY));
    }

    public static (Point BendA, Point BendB) ResolveOrthogonalBends(
        Point sourceAnchor,
        Point targetAnchor,
        GraphPortSide targetSide)
    {
        var horizontalFinalSegment = targetSide == GraphPortSide.Right;
        return ResolveOrthogonalBends(sourceAnchor, targetAnchor, horizontalFinalSegment);
    }

    public static Point ScreenToWorld(Point screenPoint, Vector panOffset, double zoomScale)
    {
        return PanZoomController.ScreenToWorld(screenPoint, panOffset, zoomScale);
    }

    public static Point WorldToScreen(Point worldPoint, Vector panOffset, double zoomScale)
    {
        return PanZoomController.WorldToScreen(worldPoint, panOffset, zoomScale);
    }
}
