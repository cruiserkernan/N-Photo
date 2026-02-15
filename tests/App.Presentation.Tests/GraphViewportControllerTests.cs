using App.Presentation.Controllers;
using Avalonia;
using Avalonia.Controls;
using Editor.Domain.Graph;

namespace App.Presentation.Tests;

public class GraphViewportControllerTests
{
    [Fact]
    public void EnsureGraphLayer_AddsClipHostAndGraphLayer()
    {
        var nodeCanvas = new Canvas();
        var clipHost = new Border();
        var graphLayer = new Canvas();

        var changed = GraphViewportController.EnsureGraphLayer(nodeCanvas, clipHost, graphLayer);

        Assert.True(changed);
        Assert.Single(nodeCanvas.Children);
        Assert.Same(clipHost, nodeCanvas.Children[0]);
        Assert.Same(graphLayer, clipHost.Child);
    }

    [Fact]
    public void GetViewportCenterWorld_UsesFallbackWhenViewportIsInvalid()
    {
        var fallback = new Point(42, 21);

        var world = GraphViewportController.GetViewportCenterWorld(new Rect(0, 0, 0, 0), new Vector(0, 0), 1.0, fallback);

        Assert.Equal(fallback, world);
    }

    [Fact]
    public void AutoFitInitialNodeView_RecentersNodeBounds()
    {
        var nodeId = NodeId.New();
        var positions = new Dictionary<NodeId, Point>
        {
            [nodeId] = new Point(0, 0)
        };

        var nextOffset = GraphViewportController.AutoFitInitialNodeView(
            positions,
            _ => 20,
            _ => 20,
            new Rect(0, 0, 100, 100),
            zoomScale: 1.0,
            panOffset: new Vector(0, 0));

        Assert.NotEqual(new Vector(0, 0), nextOffset);
    }
}
