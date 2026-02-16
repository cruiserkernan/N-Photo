using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using App.Presentation.Controllers;
using Editor.Domain.Graph;

namespace App;

public partial class MainWindow
{
    private void OnPortHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border handle ||
            !e.GetCurrentPoint(handle).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var pointerScreen = e.GetPosition(NodeCanvas);
        if (!TryFindPortLineGrabTarget(pointerScreen, out var lineTarget))
        {
            return;
        }

        var dragTarget = ResolveLineGrabTargetWithModifiers(lineTarget, e.KeyModifiers);
        BeginConnectionDrag(dragTarget.SourcePort, pointerScreen, dragTarget.DetachedEdge);

        NodeCanvas.Focus();
        e.Pointer.Capture(NodeCanvas);
        RenderPortVisuals(_edgeSnapshot);
        e.Handled = true;
    }

    private void OnNodeCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(NodeCanvas);
        var isBackgroundHit = IsCanvasBackgroundSource(e.Source);

        if (pointerPoint.Properties.IsMiddleButtonPressed && isBackgroundHit)
        {
            _isPanningCanvas = true;
            _lastPanPointerScreen = e.GetPosition(NodeCanvas);
            NodeCanvas.Focus();
            e.Pointer.Capture(NodeCanvas);
            e.Handled = true;
            return;
        }

        if (pointerPoint.Properties.IsLeftButtonPressed && isBackgroundHit)
        {
            var pointerScreen = e.GetPosition(NodeCanvas);
            if (TryFindPortLineGrabTarget(pointerScreen, out var lineTarget))
            {
                var dragTarget = ResolveLineGrabTargetWithModifiers(lineTarget, e.KeyModifiers);
                BeginConnectionDrag(dragTarget.SourcePort, pointerScreen, dragTarget.DetachedEdge);
                NodeCanvas.Focus();
                e.Pointer.Capture(NodeCanvas);
                RenderPortVisuals(_edgeSnapshot);
                e.Handled = true;
                return;
            }

            _selectedNodeId = null;
            ApplyNodeSelectionVisuals();
            RefreshPropertiesEditor();
            NodeCanvas.Focus();
            e.Handled = true;
        }
    }

    private void OnNodeCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_isConnectionDragging)
        {
            return;
        }

        if (sender is not Border card ||
            card.Tag is not NodeId nodeId ||
            !e.GetCurrentPoint(card).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var pointerScreen = e.GetPosition(NodeCanvas);
        if (TryFindPortLineGrabTarget(pointerScreen, out var lineTarget))
        {
            var dragTarget = ResolveLineGrabTargetWithModifiers(lineTarget, e.KeyModifiers);
            BeginConnectionDrag(dragTarget.SourcePort, pointerScreen, dragTarget.DetachedEdge);
            NodeCanvas.Focus();
            e.Pointer.Capture(NodeCanvas);
            RenderPortVisuals(_edgeSnapshot);
            e.Handled = true;
            return;
        }

        SetSelectedNode(nodeId);
        NodeCanvas.Focus();

        var pointerPosition = ScreenToWorld(pointerScreen);
        var currentPosition = GetNodeCardPosition(card);
        _activeDragNodeId = nodeId;
        _activeDragOffset = new Point(
            pointerPosition.X - currentPosition.X,
            pointerPosition.Y - currentPosition.Y);

        card.BorderBrush = _nodeCardDragBorder;
        e.Pointer.Capture(card);
        e.Handled = true;
    }

    private void OnNodeCardPointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Border card ||
            card.Tag is not NodeId nodeId ||
            _activeDragNodeId != nodeId)
        {
            return;
        }

        var pointerPosition = ScreenToWorld(e.GetPosition(NodeCanvas));
        var worldPosition = new Point(
            pointerPosition.X - _activeDragOffset.X,
            pointerPosition.Y - _activeDragOffset.Y);

        _nodePositions[nodeId] = worldPosition;
        SetNodeCardPosition(card, worldPosition);
        RenderPortVisuals(_edgeSnapshot);
        e.Handled = true;
    }

    private void OnNodeCardPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not Border card ||
            card.Tag is not NodeId nodeId ||
            _activeDragNodeId != nodeId)
        {
            return;
        }

        e.Pointer.Capture(null);
        CompleteNodeCardDrag(card, nodeId);
        e.Handled = true;
    }

    private void OnNodeCardPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (sender is not Border card ||
            card.Tag is not NodeId nodeId ||
            _activeDragNodeId != nodeId)
        {
            return;
        }

        CompleteNodeCardDrag(card, nodeId);
    }

    private void OnNodeCanvasSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (!_isViewTransformInitialized && e.NewSize.Width > 0 && e.NewSize.Height > 0)
        {
            _isViewTransformInitialized = true;
        }

        if (_isViewTransformInitialized &&
            !_hasAutoFittedInitialView &&
            _nodePositions.Count > 0)
        {
            AutoFitInitialNodeView();
            _hasAutoFittedInitialView = true;
        }

        ApplyGraphTransform();

        if (_nodePositions.Count == 0)
        {
            return;
        }

        foreach (var child in _nodeCards.Values)
        {
            if (child.Tag is not NodeId nodeId || !_nodePositions.TryGetValue(nodeId, out var position))
            {
                continue;
            }

            SetNodeCardPosition(child, position);
        }

        RenderPortVisuals(_edgeSnapshot);
    }

    private void OnNodeCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isConnectionDragging && e.Pointer.Captured == NodeCanvas)
        {
            var pointerScreen = e.GetPosition(NodeCanvas);
            _activeConnectionPointerWorld = ScreenToWorld(pointerScreen);
            if (_activeConnectionSource is PortKey source &&
                TryFindNearestCompatiblePort(source, pointerScreen, out var target, out var targetAnchor))
            {
                _hoverConnectionTarget = target;
                _hoverConnectionTargetAnchor = targetAnchor;
            }
            else
            {
                _hoverConnectionTarget = null;
                _hoverConnectionTargetAnchor = null;
            }

            RenderPortVisuals(_edgeSnapshot);
            e.Handled = true;
            return;
        }

        if (!_isPanningCanvas || e.Pointer.Captured != NodeCanvas)
        {
            return;
        }

        var position = e.GetPosition(NodeCanvas);
        var delta = new Vector(
            position.X - _lastPanPointerScreen.X,
            position.Y - _lastPanPointerScreen.Y);
        _lastPanPointerScreen = position;
        _panOffset += delta;
        ApplyGraphTransform();
        e.Handled = true;
    }

    private void OnNodeCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_isConnectionDragging)
        {
            var pointerScreen = e.GetPosition(NodeCanvas);
            TryCommitDraggedConnection(pointerScreen);
            e.Pointer.Capture(null);
            ResetConnectionDragState();
            e.Handled = true;
            return;
        }

        if (!_isPanningCanvas)
        {
            return;
        }

        e.Pointer.Capture(null);
        StopCanvasPan();
        e.Handled = true;
    }

    private void OnNodeCanvasPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_isConnectionDragging)
        {
            ResetConnectionDragState();
            return;
        }

        if (!_isPanningCanvas)
        {
            return;
        }

        StopCanvasPan();
    }

    private void OnNodeCanvasPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (!PanZoomController.TryZoomAtPointer(
                pointerScreen: e.GetPosition(NodeCanvas),
                wheelDelta: e.Delta.Y,
                currentZoom: _zoomScale,
                currentPan: _panOffset,
                minZoom: MinZoomScale,
                maxZoom: MaxZoomScale,
                zoomStepFactor: ZoomStepFactor,
                out var nextZoom,
                out var nextPan))
        {
            return;
        }

        _zoomScale = nextZoom;
        _panOffset = nextPan;
        ApplyGraphTransform();
        e.Handled = true;
    }
}
