using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Editor.Domain.Graph;

namespace App.Presentation.Controllers;

public static class GraphViewportController
{
    public static bool EnsureGraphLayer(Canvas nodeCanvas, Border graphLayerClipHost, Canvas graphLayer)
    {
        if (nodeCanvas.Children.Count == 1 && ReferenceEquals(nodeCanvas.Children[0], graphLayerClipHost))
        {
            return false;
        }

        nodeCanvas.Children.Clear();
        graphLayerClipHost.Child = graphLayer;
        nodeCanvas.Children.Add(graphLayerClipHost);
        Canvas.SetLeft(graphLayerClipHost, 0);
        Canvas.SetTop(graphLayerClipHost, 0);
        return true;
    }

    public static bool IsCanvasBackgroundSource(object? source, Canvas nodeCanvas, Canvas graphLayer, Border graphLayerClipHost)
    {
        return ReferenceEquals(source, nodeCanvas) ||
               ReferenceEquals(source, graphLayer) ||
               ReferenceEquals(source, graphLayerClipHost);
    }

    public static void ApplyGraphTransform(Canvas graphLayer, double zoomScale, Vector panOffset)
    {
        graphLayer.RenderTransformOrigin = RelativePoint.TopLeft;

        var transforms = new Transforms
        {
            new ScaleTransform(zoomScale, zoomScale),
            new TranslateTransform(panOffset.X, panOffset.Y)
        };

        graphLayer.RenderTransform = new TransformGroup { Children = transforms };
    }

    public static void UpdateGraphViewportClip(Canvas nodeCanvas, Border graphLayerClipHost)
    {
        var viewportWidth = Math.Max(0, nodeCanvas.Bounds.Width);
        var viewportHeight = Math.Max(0, nodeCanvas.Bounds.Height);
        graphLayerClipHost.Width = viewportWidth;
        graphLayerClipHost.Height = viewportHeight;

        nodeCanvas.Clip = new RectangleGeometry(new Rect(0, 0, viewportWidth, viewportHeight));
        graphLayerClipHost.Clip = new RectangleGeometry(new Rect(0, 0, viewportWidth, viewportHeight));
    }

    public static Point GetViewportCenterWorld(Rect canvasBounds, Vector panOffset, double zoomScale, Point fallbackWorld)
    {
        if (canvasBounds.Width <= 0 || canvasBounds.Height <= 0)
        {
            return fallbackWorld;
        }

        var screenCenter = new Point(canvasBounds.Width / 2, canvasBounds.Height / 2);
        return GraphInteractionController.ScreenToWorld(screenCenter, panOffset, zoomScale);
    }

    public static Vector AutoFitInitialNodeView(
        IReadOnlyDictionary<NodeId, Point> nodePositions,
        Func<NodeId, double> getCardWidth,
        Func<NodeId, double> getCardHeight,
        Rect canvasBounds,
        double zoomScale,
        Vector panOffset,
        double minimumMargin = 16)
    {
        if (nodePositions.Count == 0 || canvasBounds.Width <= 0 || canvasBounds.Height <= 0)
        {
            return panOffset;
        }

        var worldBounds = CalculateNodeWorldBounds(nodePositions, getCardWidth, getCardHeight);
        var viewportCenter = new Point(canvasBounds.Width / 2, canvasBounds.Height / 2);
        var worldCenter = new Point(
            worldBounds.X + (worldBounds.Width / 2),
            worldBounds.Y + (worldBounds.Height / 2));

        var adjustedOffset = new Vector(
            viewportCenter.X - (worldCenter.X * zoomScale),
            viewportCenter.Y - (worldCenter.Y * zoomScale));

        var screenTopLeft = GraphInteractionController.WorldToScreen(new Point(worldBounds.X, worldBounds.Y), adjustedOffset, zoomScale);
        var nudge = new Vector(
            Math.Max(0, minimumMargin - screenTopLeft.X),
            Math.Max(0, minimumMargin - screenTopLeft.Y));

        return adjustedOffset + nudge;
    }

    private static Rect CalculateNodeWorldBounds(
        IReadOnlyDictionary<NodeId, Point> nodePositions,
        Func<NodeId, double> getCardWidth,
        Func<NodeId, double> getCardHeight)
    {
        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        foreach (var (nodeId, position) in nodePositions)
        {
            var cardWidth = getCardWidth(nodeId);
            var cardHeight = getCardHeight(nodeId);
            minX = Math.Min(minX, position.X);
            minY = Math.Min(minY, position.Y);
            maxX = Math.Max(maxX, position.X + cardWidth);
            maxY = Math.Max(maxY, position.Y + cardHeight);
        }

        if (minX == double.MaxValue || minY == double.MaxValue)
        {
            return new Rect(0, 0, 0, 0);
        }

        return new Rect(minX, minY, Math.Max(1, maxX - minX), Math.Max(1, maxY - minY));
    }
}
