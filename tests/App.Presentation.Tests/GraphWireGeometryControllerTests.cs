using App.Presentation.Controllers;
using Avalonia;
using Editor.Domain.Graph;

namespace App.Presentation.Tests;

public class GraphWireGeometryControllerTests
{
    [Fact]
    public void ComputeArrowhead_UsesTipAsFirstPoint()
    {
        var points = GraphWireGeometryController.ComputeArrowhead(
            tip: new Point(10, 5),
            from: new Point(0, 5),
            arrowHeadLength: 5,
            arrowHeadHalfWidth: 3);

        Assert.Equal(3, points.Count);
        Assert.Equal(new Point(10, 5), points[0]);
    }

    [Fact]
    public void TryResolveLineGrabSegment_TruncatesToMaxLength()
    {
        var resolved = GraphWireGeometryController.TryResolveLineGrabSegment(
            nodeBorderAnchor: new Point(0, 0),
            lineFarEnd: new Point(100, 0),
            maxGrabLength: 30,
            out var start,
            out var end);

        Assert.True(resolved);
        Assert.Equal(new Point(0, 0), start);
        Assert.Equal(new Point(30, 0), end);
    }

    [Fact]
    public void ComputeDistanceToSegment_ReturnsExpectedDistance()
    {
        var onSegment = GraphWireGeometryController.ComputeDistanceToSegment(
            point: new Point(5, 0),
            segmentStart: new Point(0, 0),
            segmentEnd: new Point(10, 0));
        var offSegment = GraphWireGeometryController.ComputeDistanceToSegment(
            point: new Point(5, 4),
            segmentStart: new Point(0, 0),
            segmentEnd: new Point(10, 0));

        Assert.Equal(0, onSegment, precision: 6);
        Assert.Equal(4, offSegment, precision: 6);
    }

    [Fact]
    public void ResolvePortTipOffset_UsesConfiguredGlyphGeometry()
    {
        var inputTopOffset = GraphWireGeometryController.ResolvePortTipOffset(GraphPortSide.Top, PortDirection.Input);
        var outputBottomOffset = GraphWireGeometryController.ResolvePortTipOffset(GraphPortSide.Bottom, PortDirection.Output);

        Assert.Equal(new Vector(0, -16), inputTopOffset);
        Assert.Equal(new Vector(0, 10), outputBottomOffset);
    }
}
