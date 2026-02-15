using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using App.Presentation.Controllers;
using Editor.Domain.Graph;
using Polygon = Avalonia.Controls.Shapes.Polygon;
using Polyline = Avalonia.Controls.Shapes.Polyline;

namespace App;

public partial class MainWindow
{
    private const double NodeCardWidth = 170;
    private const double NodeCardHeight = 78;
    private const double NodeCanvasPadding = 8;
    private const int NodeCanvasColumns = 3;
    private const double MinZoomScale = 0.35;
    private const double MaxZoomScale = 2.5;
    private const double ZoomStepFactor = 1.1;
    private const double PortHandleHitSize = 14;
    private const double InputPortOutsideOffset = 24;
    private const double OutputPortOutsideOffset = 18;
    private const double PortGlyphPadding = 2;
    private const double PortAnchorEdgePadding = 18;
    private const double PortSnapDistancePixels = 18;
    private const double WireThickness = 2;
    private const double WireArrowLength = 9;
    private const double WireArrowWidth = 5;
    private const double ElbowNodeDiameter = 24;

    private IBrush _nodeCardBackground = Brushes.Transparent;
    private IBrush _nodeCardBorder = Brushes.Transparent;
    private IBrush _nodeCardSelectedBorder = Brushes.Transparent;
    private IBrush _nodeCardDragBorder = Brushes.Transparent;
    private IBrush _nodeCardTitleForeground = Brushes.White;
    private IBrush _nodeCardDetailForeground = Brushes.White;
    private IBrush _wireStroke = Brushes.White;
    private IBrush _wireArrowFill = Brushes.White;
    private IBrush _portInputConnected = Brushes.White;
    private IBrush _portInputUnconnected = Brushes.White;
    private IBrush _portOutputConnected = Brushes.White;
    private IBrush _portOutputUnconnected = Brushes.White;
    private IBrush _portMaskConnected = Brushes.White;
    private IBrush _portMaskUnconnected = Brushes.White;
    private IBrush _portBorderConnected = Brushes.White;
    private IBrush _portBorderUnconnected = Brushes.White;
    private IBrush _portBorderHover = Brushes.White;

    private readonly Dictionary<NodeId, Node> _nodeLookup = new();
    private readonly Dictionary<NodeId, Point> _nodePositions = new();
    private readonly Dictionary<NodeId, Border> _nodeCards = new();
    private readonly Dictionary<PortKey, PortHandleVisual> _portHandles = new();
    private readonly List<Control> _wireVisuals = new();
    private readonly Canvas _graphLayer = new();
    private readonly Border _graphLayerClipHost = new() { ClipToBounds = true };

    private IReadOnlyList<Edge> _edgeSnapshot = Array.Empty<Edge>();
    private NodeId? _activeDragNodeId;
    private NodeId? _selectedNodeId;
    private PortKey? _activeConnectionSource;
    private PortKey? _hoverConnectionTarget;
    private Point _activeConnectionPointerWorld;
    private bool _isPanningCanvas;
    private bool _isConnectionDragging;
    private bool _isViewTransformInitialized;
    private bool _hasAutoFittedInitialView;
    private Point _lastPanPointerScreen;
    private Point _activeDragOffset;
    private Vector _panOffset;
    private double _zoomScale = 1.0;

    private Canvas NodeCanvas => GraphPanelView.NodeCanvasControl;

    private void InitializeGraphVisualResources()
    {
        _nodeCardBackground = ResolveBrush("Brush.NodeCard.Background", "#2B3038");
        _nodeCardBorder = ResolveBrush("Brush.NodeCard.Border", "#4F5D73");
        _nodeCardSelectedBorder = ResolveBrush("Brush.NodeCard.BorderSelected", "#59B5FF");
        _nodeCardDragBorder = ResolveBrush("Brush.NodeCard.BorderDragging", "#E8B874");
        _nodeCardTitleForeground = ResolveBrush("Brush.Text.Primary", "#E5EAF2");
        _nodeCardDetailForeground = ResolveBrush("Brush.Text.Muted", "#95A1B3");
        _wireStroke = ResolveBrush("Brush.Graph.Wire", "#E9BA67");
        _wireArrowFill = ResolveBrush("Brush.Graph.Wire", "#E9BA67");
        _portInputConnected = ResolveBrush("Brush.PortHandle.Input.Connected", "#E8C47D");
        _portInputUnconnected = ResolveBrush("Brush.PortHandle.Input.Unconnected", "#6D5B34");
        _portOutputConnected = ResolveBrush("Brush.PortHandle.Output.Connected", "#F0C47A");
        _portOutputUnconnected = ResolveBrush("Brush.PortHandle.Output.Unconnected", "#726245");
        _portMaskConnected = ResolveBrush("Brush.PortHandle.Mask.Connected", "#7CB786");
        _portMaskUnconnected = ResolveBrush("Brush.PortHandle.Mask.Unconnected", "#3A6442");
        _portBorderConnected = ResolveBrush("Brush.PortHandle.Border.Connected", "#F6D08C");
        _portBorderUnconnected = ResolveBrush("Brush.PortHandle.Border.Unconnected", "#2D333C");
        _portBorderHover = ResolveBrush("Brush.PortHandle.Border.Hover", "#FFF2C8");
    }

    private void RefreshGraphBindings()
    {
        var snapshot = _editorSession.GetSnapshot();
        var nodes = snapshot.Nodes;
        var edges = snapshot.Edges;
        _edgeSnapshot = edges;

        _nodeLookup.Clear();
        foreach (var node in nodes)
        {
            _nodeLookup[node.Id] = node;
        }

        RefreshNodeCanvas(nodes, edges);
        PruneSelectionAndPreviewSlots(nodes);
        RefreshPropertiesEditor();

        UndoButton.IsEnabled = snapshot.CanUndo;
        RedoButton.IsEnabled = snapshot.CanRedo;
        RequestPreviewForActiveSlot();
    }

    private void RefreshNodeCanvas(IReadOnlyList<Node> nodes, IReadOnlyList<Edge> edges)
    {
        var liveNodeIds = nodes.Select(node => node.Id).ToHashSet();
        foreach (var staleNodeId in _nodePositions.Keys.Where(nodeId => !liveNodeIds.Contains(nodeId)).ToArray())
        {
            _nodePositions.Remove(staleNodeId);
        }

        if (_activeDragNodeId is NodeId activeNodeId && !liveNodeIds.Contains(activeNodeId))
        {
            _activeDragNodeId = null;
        }

        var layoutIndex = _nodePositions.Count;
        foreach (var node in nodes)
        {
            if (_nodePositions.ContainsKey(node.Id))
            {
                continue;
            }

            _nodePositions[node.Id] = GetDefaultNodePosition(layoutIndex);
            layoutIndex++;
        }

        EnsureGraphLayer();
        _graphLayer.Children.Clear();
        _wireVisuals.Clear();
        _nodeCards.Clear();
        _portHandles.Clear();
        foreach (var node in nodes)
        {
            var card = CreateNodeCard(node);
            SetNodeCardPosition(card, _nodePositions[node.Id]);
            _graphLayer.Children.Add(card);
            _nodeCards[node.Id] = card;
        }

        if (_activeConnectionSource is PortKey sourceKey && !liveNodeIds.Contains(sourceKey.NodeId))
        {
            ResetConnectionDragState();
        }

        RenderWireVisuals(edges);
        ApplyNodeSelectionVisuals();
    }

    private Border CreateNodeCard(Node node)
    {
        var nodeType = _editorSession.GetNodeTypeDefinition(node.Type);
        if (string.Equals(node.Type, NodeTypes.Elbow, StringComparison.Ordinal))
        {
            return CreateElbowNodeCard(node, nodeType);
        }

        var standardInputs = nodeType.Inputs.Where(input => !GraphPortLayoutController.IsMaskPort(input)).ToArray();
        var maskInputs = nodeType.Inputs.Where(GraphPortLayoutController.IsMaskPort).ToArray();

        var title = new TextBlock
        {
            Text = node.Type,
            FontWeight = FontWeight.SemiBold,
            Foreground = _nodeCardTitleForeground,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };
        var detail = new TextBlock
        {
            Text = node.Id.ToString()[..8],
            Foreground = _nodeCardDetailForeground,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        };

        var details = new StackPanel
        {
            Spacing = 3,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        details.Children.Add(title);
        details.Children.Add(detail);

        var inputPorts = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Margin = new Thickness(14, -InputPortOutsideOffset, 14, 0)
        };
        foreach (var input in standardInputs)
        {
            inputPorts.Children.Add(CreatePortHandle(node.Id, input.Name, PortDirection.Input, input.Role));
        }

        var outputPorts = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
            Margin = new Thickness(14, 0, 14, -OutputPortOutsideOffset)
        };
        foreach (var output in nodeType.Outputs)
        {
            outputPorts.Children.Add(CreatePortHandle(node.Id, output.Name, PortDirection.Output, output.Role));
        }

        var maskPorts = new StackPanel
        {
            Orientation = Avalonia.Layout.Orientation.Vertical,
            Spacing = 8,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            Margin = new Thickness(0, 14, -InputPortOutsideOffset, 14)
        };
        foreach (var maskInput in maskInputs)
        {
            maskPorts.Children.Add(CreatePortHandle(node.Id, maskInput.Name, PortDirection.Input, maskInput.Role));
        }

        var content = new Grid();
        content.Children.Add(details);
        if (inputPorts.Children.Count > 0)
        {
            content.Children.Add(inputPorts);
        }

        if (outputPorts.Children.Count > 0)
        {
            content.Children.Add(outputPorts);
        }

        if (maskPorts.Children.Count > 0)
        {
            content.Children.Add(maskPorts);
        }

        var card = new Border
        {
            Tag = node.Id,
            Width = NodeCardWidth,
            Height = NodeCardHeight,
            Padding = new Thickness(10, 9),
            Background = _nodeCardBackground,
            BorderBrush = _nodeCardBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Cursor = new Cursor(StandardCursorType.SizeAll),
            Child = content
        };
        card.Classes.Add("node-card");

        card.PointerPressed += OnNodeCardPointerPressed;
        card.PointerMoved += OnNodeCardPointerMoved;
        card.PointerReleased += OnNodeCardPointerReleased;
        card.PointerCaptureLost += OnNodeCardPointerCaptureLost;
        return card;
    }

    private Border CreateElbowNodeCard(Node node, NodeTypeDefinition nodeType)
    {
        var host = new Grid();
        var hub = new Border
        {
            Width = ElbowNodeDiameter,
            Height = ElbowNodeDiameter,
            CornerRadius = new CornerRadius(ElbowNodeDiameter / 2),
            Background = ResolveBrush("Brush.NodeCard.Elbow.Background", "#B9894A"),
            BorderBrush = ResolveBrush("Brush.NodeCard.Elbow.Border", "#E0BE88"),
            BorderThickness = new Thickness(1),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        host.Children.Add(hub);

        var input = nodeType.Inputs.FirstOrDefault();
        if (input is not null)
        {
            var inputPorts = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
                Margin = new Thickness(0, -InputPortOutsideOffset, 0, 0)
            };
            inputPorts.Children.Add(CreatePortHandle(node.Id, input.Name, PortDirection.Input, input.Role));
            host.Children.Add(inputPorts);
        }

        var output = nodeType.Outputs.FirstOrDefault();
        if (output is not null)
        {
            var outputPorts = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, -OutputPortOutsideOffset)
            };
            outputPorts.Children.Add(CreatePortHandle(node.Id, output.Name, PortDirection.Output, output.Role));
            host.Children.Add(outputPorts);
        }

        var card = new Border
        {
            Tag = node.Id,
            Width = ElbowNodeDiameter,
            Height = ElbowNodeDiameter,
            Background = Brushes.Transparent,
            BorderBrush = _nodeCardBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(ElbowNodeDiameter / 2),
            Cursor = new Cursor(StandardCursorType.SizeAll),
            Child = host
        };
        card.Classes.Add("node-card");
        card.Classes.Add("node-card-elbow");

        card.PointerPressed += OnNodeCardPointerPressed;
        card.PointerMoved += OnNodeCardPointerMoved;
        card.PointerReleased += OnNodeCardPointerReleased;
        card.PointerCaptureLost += OnNodeCardPointerCaptureLost;
        return card;
    }

    private Border CreatePortHandle(NodeId nodeId, string portName, PortDirection direction, NodePortRole role)
    {
        var key = new PortKey(nodeId, portName, direction);
        var side = ResolvePortSideForHandle(nodeId, portName, direction);
        var glyphGeometry = ResolvePortGlyphGeometry(side, direction);
        var hitWidth = Math.Max(PortHandleHitSize, glyphGeometry.Width + (PortGlyphPadding * 2));
        var hitHeight = Math.Max(PortHandleHitSize, glyphGeometry.Height + (PortGlyphPadding * 2));
        var glyphOffsetX = (hitWidth - glyphGeometry.Width) / 2;
        var glyphOffsetY = (hitHeight - glyphGeometry.Height) / 2;
        var anchorLocalPoint = new Point(
            glyphOffsetX + glyphGeometry.AnchorPoint.X,
            glyphOffsetY + glyphGeometry.AnchorPoint.Y);

        var arrow = new Polygon
        {
            IsHitTestVisible = false,
            Points = glyphGeometry.Points,
            Fill = ResolvePortHandleFill(direction, role, connected: false),
            Stroke = _portBorderUnconnected,
            StrokeThickness = 1
        };

        var handle = new Border
        {
            Tag = key,
            Width = hitWidth,
            Height = hitHeight,
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = new Viewbox
            {
                Width = glyphGeometry.Width,
                Height = glyphGeometry.Height,
                Stretch = Stretch.Fill,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Child = arrow
            }
        };
        ToolTip.SetTip(handle, role == NodePortRole.Mask ? $"Mask: {portName}" : $"{direction}: {portName}");
        handle.PointerPressed += OnPortHandlePressed;

        _portHandles[key] = new PortHandleVisual(handle, arrow, role, anchorLocalPoint);
        return handle;
    }

    private void OnPortHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border handle ||
            handle.Tag is not PortKey key ||
            !e.GetCurrentPoint(handle).Properties.IsLeftButtonPressed)
        {
            return;
        }

        _activeConnectionSource = key;
        _hoverConnectionTarget = null;
        _isConnectionDragging = true;

        if (!TryGetPortAnchor(key, out _activeConnectionPointerWorld))
        {
            _activeConnectionPointerWorld = ScreenToWorld(e.GetPosition(NodeCanvas));
        }

        NodeCanvas.Focus();
        e.Pointer.Capture(NodeCanvas);
        RenderWireVisuals(_edgeSnapshot);
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

        SetSelectedNode(nodeId);
        NodeCanvas.Focus();

        var pointerPosition = ScreenToWorld(e.GetPosition(NodeCanvas));
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
        RenderWireVisuals(_edgeSnapshot);
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

    private void CompleteNodeCardDrag(Border card, NodeId nodeId)
    {
        card.BorderBrush = _selectedNodeId == nodeId ? _nodeCardSelectedBorder : _nodeCardBorder;
        _activeDragNodeId = null;

        if (_nodeLookup.TryGetValue(nodeId, out var node))
        {
            SetStatus($"Moved node '{node.Type}'.");
            return;
        }

        SetStatus("Moved node.");
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

        RenderWireVisuals(_edgeSnapshot);
    }

    private void OnNodeCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isConnectionDragging && e.Pointer.Captured == NodeCanvas)
        {
            var pointerScreen = e.GetPosition(NodeCanvas);
            _activeConnectionPointerWorld = ScreenToWorld(pointerScreen);
            _hoverConnectionTarget = _activeConnectionSource is PortKey source &&
                                     TryFindNearestCompatiblePort(source, pointerScreen, out var target, out _)
                ? target
                : null;
            RenderWireVisuals(_edgeSnapshot);
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

    private void ApplyNodeSelectionVisuals()
    {
        foreach (var (nodeId, card) in _nodeCards)
        {
            if (_activeDragNodeId == nodeId)
            {
                card.BorderBrush = _nodeCardDragBorder;
                continue;
            }

            card.BorderBrush = _selectedNodeId == nodeId
                ? _nodeCardSelectedBorder
                : _nodeCardBorder;
        }
    }

    private void RenderWireVisuals(IReadOnlyList<Edge> edges)
    {
        foreach (var visual in _wireVisuals)
        {
            _graphLayer.Children.Remove(visual);
        }

        _wireVisuals.Clear();
        UpdatePortHandleVisualStates(edges);

        var insertIndex = 0;
        foreach (var edge in edges)
        {
            var sourceKey = new PortKey(edge.FromNodeId, edge.FromPort, PortDirection.Output);
            var targetKey = new PortKey(edge.ToNodeId, edge.ToPort, PortDirection.Input);
            if (!TryGetPortAnchor(sourceKey, out var sourceAnchor) ||
                !TryGetPortAnchor(targetKey, out var targetAnchor))
            {
                continue;
            }

            var (wire, arrow) = CreateWire(sourceAnchor, targetAnchor);
            _graphLayer.Children.Insert(insertIndex++, wire);
            _graphLayer.Children.Insert(insertIndex++, arrow);
            _wireVisuals.Add(wire);
            _wireVisuals.Add(arrow);
        }

        if (_isConnectionDragging &&
            TryResolveDragWireEndpoints(out var dragSourceAnchor, out var dragTargetAnchor))
        {
            var (dragWire, dragArrow) = CreateWire(dragSourceAnchor, dragTargetAnchor);
            dragWire.StrokeDashArray = new AvaloniaList<double> { 5, 4 };
            _graphLayer.Children.Insert(insertIndex++, dragWire);
            _graphLayer.Children.Insert(insertIndex++, dragArrow);
            _wireVisuals.Add(dragWire);
            _wireVisuals.Add(dragArrow);
        }
    }

    private (Polyline Wire, Polygon Arrow) CreateWire(Point sourceAnchor, Point targetAnchor)
    {
        var wire = new Polyline
        {
            IsHitTestVisible = false,
            Stroke = _wireStroke,
            StrokeThickness = WireThickness,
            Points = new[] { sourceAnchor, targetAnchor }
        };
        var arrow = (Polygon)CreateArrowHead(sourceAnchor, targetAnchor);
        return (wire, arrow);
    }

    private bool TryCommitDraggedConnection(Point pointerScreen)
    {
        if (_activeConnectionSource is not PortKey source)
        {
            return false;
        }

        if (!TryFindNearestCompatiblePort(source, pointerScreen, out var target, out _))
        {
            SetStatus("Connection canceled.");
            return false;
        }

        var (fromPort, toPort) = ResolveConnectionOrder(source, target);
        try
        {
            _editorSession.Connect(fromPort.NodeId, fromPort.PortName, toPort.NodeId, toPort.PortName);
            RefreshGraphBindings();
            SetStatus($"Connected {fromPort.PortName} -> {toPort.PortName}");
            return true;
        }
        catch (Exception exception)
        {
            SetStatus($"Connect failed: {exception.Message}");
            return false;
        }
    }

    private void ResetConnectionDragState()
    {
        _isConnectionDragging = false;
        _activeConnectionSource = null;
        _hoverConnectionTarget = null;
        RenderWireVisuals(_edgeSnapshot);
    }

    private bool TryFindNearestCompatiblePort(
        PortKey sourcePort,
        Point pointerScreen,
        out PortKey targetPort,
        out Point targetAnchor)
    {
        targetPort = default;
        targetAnchor = default;
        var found = false;
        var nearestDistance = double.MaxValue;
        var targetDirection = sourcePort.Direction == PortDirection.Output
            ? PortDirection.Input
            : PortDirection.Output;
        foreach (var node in _nodeLookup.Values)
        {
            var nodeType = _editorSession.GetNodeTypeDefinition(node.Type);
            var candidatePortNames = targetDirection == PortDirection.Input
                ? nodeType.Inputs.Select(port => port.Name)
                : nodeType.Outputs.Select(port => port.Name);

            foreach (var candidatePortName in candidatePortNames)
            {
                var candidate = new PortKey(node.Id, candidatePortName, targetDirection);
                if (!TryGetPortAnchor(candidate, out var anchor))
                {
                    continue;
                }

                var anchorScreen = WorldToScreen(anchor);
                var dx = anchorScreen.X - pointerScreen.X;
                var dy = anchorScreen.Y - pointerScreen.Y;
                var distance = Math.Sqrt((dx * dx) + (dy * dy));
                if (distance > PortSnapDistancePixels || distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = distance;
                targetPort = candidate;
                targetAnchor = anchor;
                found = true;
            }
        }

        return found;
    }

    private static (PortKey FromPort, PortKey ToPort) ResolveConnectionOrder(PortKey first, PortKey second)
    {
        return first.Direction == PortDirection.Output
            ? (first, second)
            : (second, first);
    }

    private bool TryResolveDragWireEndpoints(
        out Point dragSourceAnchor,
        out Point dragTargetAnchor)
    {
        dragSourceAnchor = default;
        dragTargetAnchor = default;

        if (_activeConnectionSource is not PortKey source ||
            !TryGetPortAnchor(source, out var sourceAnchor))
        {
            return false;
        }

        if (source.Direction == PortDirection.Output)
        {
            dragSourceAnchor = sourceAnchor;
            if (_hoverConnectionTarget is PortKey hoverTarget &&
                TryGetPortAnchor(hoverTarget, out var hoverAnchor))
            {
                dragTargetAnchor = hoverAnchor;
                return true;
            }

            dragTargetAnchor = _activeConnectionPointerWorld;
            return true;
        }

        dragTargetAnchor = sourceAnchor;
        if (_hoverConnectionTarget is PortKey hoverOutput &&
            TryGetPortAnchor(hoverOutput, out var hoverOutputAnchor))
        {
            dragSourceAnchor = hoverOutputAnchor;
        }
        else
        {
            dragSourceAnchor = _activeConnectionPointerWorld;
        }

        return true;
    }

    private bool TryGetPortAnchor(PortKey key, out Point anchor)
    {
        anchor = default;

        if (_portHandles.TryGetValue(key, out var visual))
        {
            if (visual.Glyph.IsVisible && TryResolveVisualAnchor(visual, out anchor))
            {
                return true;
            }
        }

        if (!_nodeLookup.TryGetValue(key.NodeId, out var node) ||
            !_nodePositions.TryGetValue(key.NodeId, out var nodePosition))
        {
            return false;
        }

        var nodeType = _editorSession.GetNodeTypeDefinition(node.Type);
        if (!GraphPortLayoutController.TryResolveAnchorPlan(
                nodeType,
                key.PortName,
                key.Direction,
                out var anchorPlan))
        {
            return false;
        }

        var cardHeight = GetCardHeight(key.NodeId);
        var cardWidth = GetCardWidth(key.NodeId);
        anchor = GraphPortLayoutController.ResolveAnchor(
            nodePosition,
            cardWidth,
            cardHeight,
            anchorPlan,
            PortAnchorEdgePadding);
        return true;
    }

    private bool TryResolveVisualAnchor(PortHandleVisual visual, out Point anchor)
    {
        var width = visual.HitArea.Bounds.Width;
        var height = visual.HitArea.Bounds.Height;
        if (width <= 0 || height <= 0)
        {
            anchor = default;
            return false;
        }

        var translated = visual.HitArea.TranslatePoint(visual.AnchorLocalPoint, _graphLayer);
        if (translated.HasValue)
        {
            anchor = translated.Value;
            return true;
        }

        anchor = default;
        return false;
    }

    private void UpdatePortHandleVisualStates(IReadOnlyList<Edge> edges)
    {
        var connectedInputs = edges
            .Select(edge => new PortKey(edge.ToNodeId, edge.ToPort, PortDirection.Input))
            .ToHashSet();
        var connectedOutputs = edges
            .Select(edge => new PortKey(edge.FromNodeId, edge.FromPort, PortDirection.Output))
            .ToHashSet();

        foreach (var (key, handle) in _portHandles)
        {
            var isConnected = key.Direction == PortDirection.Output
                ? connectedOutputs.Contains(key)
                : connectedInputs.Contains(key);
            var role = handle.Role;
            var showGlyph = !isConnected;

            handle.Glyph.IsVisible = showGlyph;

            handle.Glyph.Fill = ResolvePortHandleFill(key.Direction, role, isConnected);
            handle.Glyph.Stroke = isConnected ? _portBorderConnected : _portBorderUnconnected;
            handle.Glyph.StrokeThickness = 1;

            if (showGlyph &&
                ((_hoverConnectionTarget is PortKey hover && hover.Equals(key)) ||
                 (_activeConnectionSource is PortKey active && active.Equals(key))))
            {
                handle.Glyph.Stroke = _portBorderHover;
                handle.Glyph.StrokeThickness = 1.5;
            }
        }
    }

    private IBrush ResolvePortHandleFill(PortDirection direction, NodePortRole role, bool connected)
    {
        if (direction == PortDirection.Output)
        {
            return connected ? _portOutputConnected : _portOutputUnconnected;
        }

        if (role == NodePortRole.Mask)
        {
            return connected ? _portMaskConnected : _portMaskUnconnected;
        }

        return connected ? _portInputConnected : _portInputUnconnected;
    }

    private GraphPortSide ResolvePortSideForHandle(NodeId nodeId, string portName, PortDirection direction)
    {
        if (_nodeLookup.TryGetValue(nodeId, out var node))
        {
            var nodeType = _editorSession.GetNodeTypeDefinition(node.Type);
            if (GraphPortLayoutController.TryResolveAnchorPlan(
                    nodeType,
                    portName,
                    direction,
                    out var plan))
            {
                return plan.Side;
            }
        }

        return direction == PortDirection.Output
            ? GraphPortSide.Bottom
            : GraphPortSide.Top;
    }

    private static PortGlyphGeometry ResolvePortGlyphGeometry(GraphPortSide side, PortDirection direction)
    {
        return side switch
        {
            GraphPortSide.Right => direction == PortDirection.Input
                ? new PortGlyphGeometry(
                    Width: 18,
                    Height: 10,
                    AnchorPoint: new Point(1, 5),
                    Points: new[]
                    {
                        new Point(1, 5),
                        new Point(7, 1),
                        new Point(7, 3.5),
                        new Point(17, 3.5),
                        new Point(17, 6.5),
                        new Point(7, 6.5),
                        new Point(7, 9)
                    })
                : new PortGlyphGeometry(
                    Width: 18,
                    Height: 10,
                    AnchorPoint: new Point(1, 5),
                    Points: new[]
                    {
                        new Point(17, 5),
                        new Point(11, 1),
                        new Point(11, 3.5),
                        new Point(1, 3.5),
                        new Point(1, 6.5),
                        new Point(11, 6.5),
                        new Point(11, 9)
                    }),
            GraphPortSide.Bottom => direction == PortDirection.Output
                ? new PortGlyphGeometry(
                    Width: 10,
                    Height: 18,
                    AnchorPoint: new Point(5, 1),
                    Points: new[]
                    {
                        new Point(5, 17),
                        new Point(1, 11),
                        new Point(3.5, 11),
                        new Point(3.5, 1),
                        new Point(6.5, 1),
                        new Point(6.5, 11),
                        new Point(9, 11)
                    })
                : new PortGlyphGeometry(
                    Width: 10,
                    Height: 18,
                    AnchorPoint: new Point(5, 1),
                    Points: new[]
                    {
                        new Point(5, 1),
                        new Point(1, 7),
                        new Point(3.5, 7),
                        new Point(3.5, 17),
                        new Point(6.5, 17),
                        new Point(6.5, 7),
                        new Point(9, 7)
                    }),
            _ => direction == PortDirection.Input
                ? new PortGlyphGeometry(
                    Width: 10,
                    Height: 18,
                    AnchorPoint: new Point(5, 17),
                    Points: new[]
                    {
                        new Point(5, 17),
                        new Point(1, 11),
                        new Point(3.5, 11),
                        new Point(3.5, 1),
                        new Point(6.5, 1),
                        new Point(6.5, 11),
                        new Point(9, 11)
                    })
                : new PortGlyphGeometry(
                    Width: 10,
                    Height: 18,
                    AnchorPoint: new Point(5, 17),
                    Points: new[]
                    {
                        new Point(5, 1),
                        new Point(1, 7),
                        new Point(3.5, 7),
                        new Point(3.5, 17),
                        new Point(6.5, 17),
                        new Point(6.5, 7),
                        new Point(9, 7)
                    })
        };
    }

    private Control CreateArrowHead(Point from, Point to)
    {
        var deltaX = to.X - from.X;
        var deltaY = to.Y - from.Y;
        var length = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
        if (length < 0.001)
        {
            return new Polygon
            {
                IsHitTestVisible = false,
                Fill = _wireArrowFill,
                Points = new[] { to, to, to }
            };
        }

        var direction = new Point(deltaX / length, deltaY / length);
        var perpendicular = new Point(-direction.Y, direction.X);
        var basePoint = new Point(
            to.X - (direction.X * WireArrowLength),
            to.Y - (direction.Y * WireArrowLength));
        var left = new Point(
            basePoint.X + (perpendicular.X * WireArrowWidth),
            basePoint.Y + (perpendicular.Y * WireArrowWidth));
        var right = new Point(
            basePoint.X - (perpendicular.X * WireArrowWidth),
            basePoint.Y - (perpendicular.Y * WireArrowWidth));

        return new Polygon
        {
            IsHitTestVisible = false,
            Fill = _wireArrowFill,
            Points = new[] { to, left, right }
        };
    }

    private double GetCardHeight(NodeId nodeId)
    {
        if (!_nodeCards.TryGetValue(nodeId, out var card))
        {
            return NodeCardHeight;
        }

        return card.Bounds.Height > 0 ? card.Bounds.Height : NodeCardHeight;
    }

    private double GetCardWidth(NodeId nodeId)
    {
        if (!_nodeCards.TryGetValue(nodeId, out var card))
        {
            return NodeCardWidth;
        }

        return card.Bounds.Width > 0 ? card.Bounds.Width : NodeCardWidth;
    }

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

    private readonly record struct PortKey(NodeId NodeId, string PortName, PortDirection Direction);

    private sealed record PortGlyphGeometry(
        double Width,
        double Height,
        Point AnchorPoint,
        IList<Point> Points);

    private sealed record PortHandleVisual(
        Border HitArea,
        Polygon Glyph,
        NodePortRole Role,
        Point AnchorLocalPoint);
}
