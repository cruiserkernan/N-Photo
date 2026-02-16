using Avalonia;
using Avalonia.Controls;
using App.Presentation.Controllers;
using Editor.Domain.Graph;

namespace App;

public partial class MainWindow
{
    private void EnsureGraphLayer()
    {
        if (!GraphViewportController.EnsureGraphLayer(NodeCanvas, _graphLayerClipHost, _graphLayer))
        {
            return;
        }

        UpdateGraphViewportClip();
        ApplyGraphTransform();
    }

    private bool IsCanvasBackgroundSource(object? source)
    {
        return GraphViewportController.IsCanvasBackgroundSource(source, NodeCanvas, _graphLayer, _graphLayerClipHost);
    }

    private void StopCanvasPan()
    {
        _isPanningCanvas = false;
    }

    private void ApplyGraphTransform()
    {
        UpdateGraphViewportClip();
        GraphViewportController.ApplyGraphTransform(_graphLayer, _zoomScale, _panOffset);
    }

    private void UpdateGraphViewportClip()
    {
        GraphViewportController.UpdateGraphViewportClip(NodeCanvas, _graphLayerClipHost);
    }

    private Point ScreenToWorld(Point screenPoint)
    {
        return GraphInteractionController.ScreenToWorld(screenPoint, _panOffset, _zoomScale);
    }

    private Point WorldToScreen(Point worldPoint)
    {
        return GraphInteractionController.WorldToScreen(worldPoint, _panOffset, _zoomScale);
    }

    private Point GetViewportCenterWorld()
    {
        return GraphViewportController.GetViewportCenterWorld(
            NodeCanvas.Bounds,
            _panOffset,
            _zoomScale,
            GetDefaultNodePosition(_nodePositions.Count));
    }

    private void AutoFitInitialNodeView()
    {
        _panOffset = GraphViewportController.AutoFitInitialNodeView(
            _nodePositions,
            GetCardWidth,
            GetCardHeight,
            NodeCanvas.Bounds,
            _zoomScale,
            _panOffset);
    }

    private static Point GetDefaultNodePosition(int index)
    {
        var column = index % NodeCanvasColumns;
        var row = index / NodeCanvasColumns;
        return new Point(
            NodeCanvasPadding + (column * (NodeCardWidth + 14)),
            NodeCanvasPadding + (row * (NodeCardHeight + 14)));
    }

    private static Point GetNodeCardPosition(Border card)
    {
        var x = Canvas.GetLeft(card);
        var y = Canvas.GetTop(card);
        return new Point(
            double.IsNaN(x) ? NodeCanvasPadding : x,
            double.IsNaN(y) ? NodeCanvasPadding : y);
    }

    private static void SetNodeCardPosition(Border card, Point position)
    {
        Canvas.SetLeft(card, position.X);
        Canvas.SetTop(card, position.Y);
    }
}
