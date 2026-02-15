using Avalonia;

namespace App.Presentation.Controllers;

public sealed class GraphInteractionController
{
    public static (Point BendA, Point BendB) ResolveOrthogonalBends(
        Point sourceAnchor,
        Point targetAnchor,
        bool isHorizontalDominant)
    {
        if (isHorizontalDominant)
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
