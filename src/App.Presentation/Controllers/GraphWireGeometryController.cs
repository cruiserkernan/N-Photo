using Avalonia;
using Editor.Domain.Graph;

namespace App.Presentation.Controllers;

public readonly record struct GraphPortGlyphGeometry(
    double Width,
    double Height,
    Point EdgePoint,
    Point TipPoint);

public static class GraphWireGeometryController
{
    public static GraphPortGlyphGeometry ResolvePortGlyphGeometry(GraphPortSide side, PortDirection direction)
    {
        return side switch
        {
            GraphPortSide.Right => direction == PortDirection.Input
                ? new GraphPortGlyphGeometry(
                    Width: 18,
                    Height: 10,
                    EdgePoint: new Point(1, 5),
                    TipPoint: new Point(17, 5))
                : new GraphPortGlyphGeometry(
                    Width: 12,
                    Height: 10,
                    EdgePoint: new Point(1, 5),
                    TipPoint: new Point(11, 5)),
            GraphPortSide.Bottom => direction == PortDirection.Output
                ? new GraphPortGlyphGeometry(
                    Width: 10,
                    Height: 12,
                    EdgePoint: new Point(5, 1),
                    TipPoint: new Point(5, 11))
                : new GraphPortGlyphGeometry(
                    Width: 10,
                    Height: 18,
                    EdgePoint: new Point(5, 1),
                    TipPoint: new Point(5, 17)),
            _ => direction == PortDirection.Input
                ? new GraphPortGlyphGeometry(
                    Width: 10,
                    Height: 18,
                    EdgePoint: new Point(5, 17),
                    TipPoint: new Point(5, 1))
                : new GraphPortGlyphGeometry(
                    Width: 10,
                    Height: 18,
                    EdgePoint: new Point(5, 17),
                    TipPoint: new Point(5, 7))
        };
    }

    public static IReadOnlyList<Point> ComputeArrowhead(
        Point tip,
        Point from,
        double arrowHeadLength,
        double arrowHeadHalfWidth)
    {
        var dx = tip.X - from.X;
        var dy = tip.Y - from.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 0.001)
        {
            return Array.Empty<Point>();
        }

        var nx = dx / length;
        var ny = dy / length;
        var px = -ny;
        var py = nx;

        var baseCenterX = tip.X - (nx * arrowHeadLength);
        var baseCenterY = tip.Y - (ny * arrowHeadLength);

        return
        [
            tip,
            new Point(baseCenterX + (px * arrowHeadHalfWidth), baseCenterY + (py * arrowHeadHalfWidth)),
            new Point(baseCenterX - (px * arrowHeadHalfWidth), baseCenterY - (py * arrowHeadHalfWidth))
        ];
    }

    public static bool TryResolveLineGrabSegment(
        Point nodeBorderAnchor,
        Point lineFarEnd,
        double maxGrabLength,
        out Point grabStart,
        out Point grabEnd)
    {
        grabStart = default;
        grabEnd = default;

        var dx = lineFarEnd.X - nodeBorderAnchor.X;
        var dy = lineFarEnd.Y - nodeBorderAnchor.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 0.001)
        {
            return false;
        }

        var segmentLength = Math.Min(length, maxGrabLength);
        var scale = segmentLength / length;
        grabStart = nodeBorderAnchor;
        grabEnd = new Point(
            nodeBorderAnchor.X + (dx * scale),
            nodeBorderAnchor.Y + (dy * scale));
        return true;
    }

    public static double ComputeDistanceToSegment(Point point, Point segmentStart, Point segmentEnd)
    {
        var dx = segmentEnd.X - segmentStart.X;
        var dy = segmentEnd.Y - segmentStart.Y;
        var lengthSquared = (dx * dx) + (dy * dy);
        if (lengthSquared < 0.0001)
        {
            return Math.Sqrt(
                ((point.X - segmentStart.X) * (point.X - segmentStart.X)) +
                ((point.Y - segmentStart.Y) * (point.Y - segmentStart.Y)));
        }

        var projection = (((point.X - segmentStart.X) * dx) + ((point.Y - segmentStart.Y) * dy)) / lengthSquared;
        projection = Math.Clamp(projection, 0.0, 1.0);

        var nearestX = segmentStart.X + (dx * projection);
        var nearestY = segmentStart.Y + (dy * projection);
        var deltaX = point.X - nearestX;
        var deltaY = point.Y - nearestY;
        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }

    public static Vector ResolvePortTipOffset(GraphPortSide side, PortDirection direction)
    {
        var geometry = ResolvePortGlyphGeometry(side, direction);
        return new Vector(
            geometry.TipPoint.X - geometry.EdgePoint.X,
            geometry.TipPoint.Y - geometry.EdgePoint.Y);
    }
}
