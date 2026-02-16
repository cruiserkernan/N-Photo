using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using App.Presentation.Controllers;
using Editor.Domain.Graph;
using Ellipse = Avalonia.Controls.Shapes.Ellipse;
using Line = Avalonia.Controls.Shapes.Line;
using Polygon = Avalonia.Controls.Shapes.Polygon;

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
    private const double WireThickness = 3;
    private const double ArrowHeadHalfWidth = 3.0;
    private const double ArrowHeadLength = 5.0;
    private const double ConnectionSnapDotDiameter = 8;
    private const double PortLineGrabDistance = 12;
    private const double PortLineGrabLength = 30;
    private const double ConnectedLineGrabLength = 44;
    private const double ElbowNodeDiameter = 24;

    private IBrush _nodeCardBackground = Brushes.Transparent;
    private IBrush _nodeCardBorder = Brushes.Transparent;
    private IBrush _nodeCardSelectedBorder = Brushes.Transparent;
    private IBrush _nodeCardDragBorder = Brushes.Transparent;
    private IBrush _nodeCardTitleForeground = Brushes.White;
    private IBrush _nodeCardDetailForeground = Brushes.White;
    private IBrush _wireStroke = Brushes.White;
    private IBrush _portInputConnected = Brushes.White;
    private IBrush _portInputUnconnected = Brushes.White;
    private IBrush _portOutputConnected = Brushes.White;
    private IBrush _portOutputUnconnected = Brushes.White;
    private IBrush _portMaskConnected = Brushes.White;
    private IBrush _portMaskUnconnected = Brushes.White;
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
    private Edge? _activeConnectionDetachedEdge;
    private PortKey? _hoverConnectionTarget;
    private Point? _hoverConnectionTargetAnchor;
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
        _portInputConnected = ResolveBrush("Brush.PortHandle.Input.Connected", "#E8C47D");
        _portInputUnconnected = ResolveBrush("Brush.PortHandle.Input.Unconnected", "#6D5B34");
        _portOutputConnected = ResolveBrush("Brush.PortHandle.Output.Connected", "#F0C47A");
        _portOutputUnconnected = ResolveBrush("Brush.PortHandle.Output.Unconnected", "#726245");
        _portMaskConnected = ResolveBrush("Brush.PortHandle.Mask.Connected", "#7CB786");
        _portMaskUnconnected = ResolveBrush("Brush.PortHandle.Mask.Unconnected", "#3A6442");
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

        RenderPortVisuals(edges);
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
            ClipToBounds = false,
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
            ClipToBounds = false,
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
        var tipLocalPoint = new Point(
            glyphOffsetX + glyphGeometry.TipPoint.X,
            glyphOffsetY + glyphGeometry.TipPoint.Y);
        var edgeLocalPoint = new Point(
            glyphOffsetX + glyphGeometry.EdgePoint.X,
            glyphOffsetY + glyphGeometry.EdgePoint.Y);

        var handle = new Border
        {
            Tag = key,
            Width = hitWidth,
            Height = hitHeight,
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            ZIndex = 20
        };
        ToolTip.SetTip(handle, role == NodePortRole.Mask ? $"Mask: {portName}" : $"{direction}: {portName}");
        handle.PointerPressed += OnPortHandlePressed;

        _portHandles[key] = new PortHandleVisual(handle, role, tipLocalPoint, edgeLocalPoint);
        return handle;
    }

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

    private void RenderPortVisuals(IReadOnlyList<Edge> edges)
    {
        foreach (var visual in _wireVisuals)
        {
            _graphLayer.Children.Remove(visual);
        }

        _wireVisuals.Clear();

        var inputToOutput = new Dictionary<PortKey, PortKey>();
        var connectedOutputs = new HashSet<PortKey>();
        foreach (var edge in edges)
        {
            var inputKey = new PortKey(edge.ToNodeId, edge.ToPort, PortDirection.Input);
            var outputKey = new PortKey(edge.FromNodeId, edge.FromPort, PortDirection.Output);
            inputToOutput[inputKey] = outputKey;
            connectedOutputs.Add(outputKey);
        }

        var insertIndex = _graphLayer.Children.Count;
        var renderDragLine = false;
        Point dragLineFrom = default, dragLineTo = default;

        foreach (var (key, handle) in _portHandles)
        {
            if (!TryGetPortAnchors(key, out var baseAnchor, out var tipAnchor))
            {
                continue;
            }

            var isDragSource = _isConnectionDragging &&
                               _activeConnectionSource is PortKey src && src.Equals(key);
            var isConnectedOutput = key.Direction == PortDirection.Output && connectedOutputs.Contains(key);

            if (!isDragSource && isConnectedOutput)
            {
                continue;
            }

            Point farEnd;
            bool isConnected;
            IBrush stroke;
            double opacity;

            if (isDragSource)
            {
                if (key.Direction == PortDirection.Output)
                {
                    if (TryResolveNodeBorderAnchor(key.NodeId, _activeConnectionPointerWorld, out var dynamicSourceAnchor))
                    {
                        baseAnchor = dynamicSourceAnchor;
                        tipAnchor = dynamicSourceAnchor;
                    }

                    farEnd = tipAnchor;
                    isConnected = connectedOutputs.Contains(key);
                    stroke = _portBorderHover;
                    opacity = 1.0;

                    renderDragLine = true;
                    dragLineFrom = tipAnchor;
                    dragLineTo = _activeConnectionPointerWorld;
                }
                else
                {
                    if (TryResolveNodeBorderAnchor(key.NodeId, _activeConnectionPointerWorld, out var dynamicInputAnchor))
                    {
                        baseAnchor = dynamicInputAnchor;
                    }

                    farEnd = _activeConnectionPointerWorld;
                    isConnected = false;
                    stroke = _portBorderHover;
                    opacity = 1.0;
                }
            }
            else if (key.Direction != PortDirection.Output &&
                     inputToOutput.TryGetValue(key, out var sourceOutputKey))
            {
                isConnected = true;
                stroke = _wireStroke;
                opacity = 1.0;

                if (TryGetNodeCenter(sourceOutputKey.NodeId, out var sourceCenter) &&
                    TryGetNodeCenter(key.NodeId, out var targetCenter) &&
                    TryResolveNodeBorderAnchor(sourceOutputKey.NodeId, targetCenter, out var sourceBorderAnchor) &&
                    TryResolveNodeBorderAnchor(key.NodeId, sourceCenter, out var targetBorderAnchor))
                {
                    farEnd = sourceBorderAnchor;
                    baseAnchor = targetBorderAnchor;
                }
                else
                {
                    farEnd = TryGetPortAnchor(sourceOutputKey, out var outputTip)
                        ? outputTip
                        : tipAnchor;
                }
            }
            else
            {
                farEnd = tipAnchor;
                isConnected = isConnectedOutput;
                stroke = ResolvePortHandleStroke(key.Direction, handle.Role, isConnected);
                opacity = isConnected ? 1.0 : 0.75;
            }

            if (!isDragSource &&
                _hoverConnectionTarget is PortKey hover && hover.Equals(key))
            {
                stroke = _portBorderHover;
                opacity = 1.0;
            }

            var line = new Line
            {
                IsHitTestVisible = false,
                StartPoint = farEnd,
                EndPoint = baseAnchor,
                Stroke = stroke,
                StrokeThickness = WireThickness,
                StrokeLineCap = PenLineCap.Round,
                Opacity = opacity
            };
            _graphLayer.Children.Insert(insertIndex++, line);
            _wireVisuals.Add(line);

            var drawArrow = false;
            Point arrowTip = default;
            Point arrowFrom = default;

            if (key.Direction == PortDirection.Output)
            {
                if (!isConnected)
                {
                    drawArrow = true;
                    arrowTip = farEnd;
                    arrowFrom = baseAnchor;
                }
            }
            else
            {
                drawArrow = true;
                arrowTip = baseAnchor;
                arrowFrom = farEnd;
            }

            if (drawArrow)
            {
                var arrowPoints = ComputeArrowhead(arrowTip, arrowFrom);
                if (arrowPoints.Count > 0)
                {
                    var arrow = new Polygon
                    {
                        IsHitTestVisible = false,
                        Points = arrowPoints,
                        Fill = stroke,
                        Opacity = opacity
                    };
                    _graphLayer.Children.Insert(insertIndex++, arrow);
                    _wireVisuals.Add(arrow);
                }
            }
        }

        if (renderDragLine)
        {
            var dragLine = new Line
            {
                IsHitTestVisible = false,
                StartPoint = dragLineFrom,
                EndPoint = dragLineTo,
                Stroke = _wireStroke,
                StrokeThickness = WireThickness,
                StrokeLineCap = PenLineCap.Round,
                StrokeDashArray = new AvaloniaList<double> { 5, 4 }
            };
            _graphLayer.Children.Insert(insertIndex, dragLine);
            _wireVisuals.Add(dragLine);
            insertIndex++;
        }

        if (_isConnectionDragging && _hoverConnectionTargetAnchor is Point snapAnchor)
        {
            var radius = ConnectionSnapDotDiameter / 2;
            var snapDot = new Ellipse
            {
                IsHitTestVisible = false,
                Width = ConnectionSnapDotDiameter,
                Height = ConnectionSnapDotDiameter,
                Fill = _portBorderHover,
                Stroke = _wireStroke,
                StrokeThickness = 1.25,
                ZIndex = 30
            };
            Canvas.SetLeft(snapDot, snapAnchor.X - radius);
            Canvas.SetTop(snapDot, snapAnchor.Y - radius);
            _graphLayer.Children.Insert(insertIndex, snapDot);
            _wireVisuals.Add(snapDot);
        }
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
        var newEdge = new Edge(fromPort.NodeId, fromPort.PortName, toPort.NodeId, toPort.PortName);

        if (_activeConnectionDetachedEdge is Edge detachedEdge &&
            detachedEdge.Equals(newEdge))
        {
            SetStatus("Connection unchanged.");
            return true;
        }

        try
        {
            if (_activeConnectionDetachedEdge is Edge edgeToDetach)
            {
                _editorSession.Disconnect(
                    edgeToDetach.FromNodeId,
                    edgeToDetach.FromPort,
                    edgeToDetach.ToNodeId,
                    edgeToDetach.ToPort);
            }

            _editorSession.Connect(newEdge.FromNodeId, newEdge.FromPort, newEdge.ToNodeId, newEdge.ToPort);
            RefreshGraphBindings();
            SetStatus($"Connected {fromPort.PortName} -> {toPort.PortName}");
            return true;
        }
        catch (Exception exception)
        {
            if (_activeConnectionDetachedEdge is Edge edgeToRestore)
            {
                try
                {
                    _editorSession.Connect(
                        edgeToRestore.FromNodeId,
                        edgeToRestore.FromPort,
                        edgeToRestore.ToNodeId,
                        edgeToRestore.ToPort);
                    RefreshGraphBindings();
                }
                catch
                {
                    // If rollback fails, keep original connect error but refresh view from latest snapshot.
                    RefreshGraphBindings();
                }
            }

            SetStatus($"Connect failed: {exception.Message}");
            return false;
        }
    }

    private void ResetConnectionDragState()
    {
        _isConnectionDragging = false;
        _activeConnectionSource = null;
        _activeConnectionDetachedEdge = null;
        _hoverConnectionTarget = null;
        _hoverConnectionTargetAnchor = null;
        RenderPortVisuals(_edgeSnapshot);
    }

    private void BeginConnectionDrag(PortKey sourcePort, Point pointerScreen, Edge? detachedEdge = null)
    {
        _activeConnectionSource = sourcePort;
        _activeConnectionDetachedEdge = detachedEdge;
        if (_activeConnectionDetachedEdge is null &&
            sourcePort.Direction == PortDirection.Output &&
            TryResolveOutputDetachEdgeForDrag(sourcePort, pointerScreen, out var existingEdge))
        {
            _activeConnectionDetachedEdge = existingEdge;
        }

        _hoverConnectionTarget = null;
        _hoverConnectionTargetAnchor = null;
        _isConnectionDragging = true;
        _activeConnectionPointerWorld = ScreenToWorld(pointerScreen);
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
        var pointerWorld = ScreenToWorld(pointerScreen);
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
                var hasDynamicAnchor = TryResolveNodeBorderAnchor(candidate.NodeId, pointerWorld, out var anchor);
                if (!hasDynamicAnchor && !TryGetPortAnchor(candidate, out anchor))
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

    private bool TryFindPortLineGrabTarget(Point pointerScreen, out PortLineGrabTarget target)
    {
        target = default;
        var nearestDistance = double.MaxValue;
        var found = false;

        var connectedInputs = new HashSet<PortKey>();
        var connectedOutputs = new HashSet<PortKey>();
        foreach (var edge in _edgeSnapshot)
        {
            var inputKey = new PortKey(edge.ToNodeId, edge.ToPort, PortDirection.Input);
            var outputKey = new PortKey(edge.FromNodeId, edge.FromPort, PortDirection.Output);
            connectedInputs.Add(inputKey);
            connectedOutputs.Add(outputKey);

            if (!TryResolveConnectedLineAnchors(outputKey, inputKey, out var outputAnchor, out var inputAnchor))
            {
                continue;
            }

            var outputAnchorScreen = WorldToScreen(outputAnchor);
            var inputAnchorScreen = WorldToScreen(inputAnchor);
            if (TryResolveLineGrabSegment(outputAnchorScreen, inputAnchorScreen, ConnectedLineGrabLength, out var outputGrabStart, out var outputGrabEnd))
            {
                var outputDistance = ComputeDistanceToSegment(pointerScreen, outputGrabStart, outputGrabEnd);
                if (outputDistance <= PortLineGrabDistance && outputDistance < nearestDistance)
                {
                    nearestDistance = outputDistance;
                    target = new PortLineGrabTarget(inputKey, edge);
                    found = true;
                }
            }

            if (TryResolveLineGrabSegment(inputAnchorScreen, outputAnchorScreen, ConnectedLineGrabLength, out var inputGrabStart, out var inputGrabEnd))
            {
                var inputDistance = ComputeDistanceToSegment(pointerScreen, inputGrabStart, inputGrabEnd);
                if (inputDistance <= PortLineGrabDistance && inputDistance < nearestDistance)
                {
                    nearestDistance = inputDistance;
                    target = new PortLineGrabTarget(outputKey, edge);
                    found = true;
                }
            }
        }

        foreach (var (candidatePort, _) in _portHandles)
        {
            var isConnected = candidatePort.Direction == PortDirection.Output
                ? connectedOutputs.Contains(candidatePort)
                : connectedInputs.Contains(candidatePort);
            if (isConnected)
            {
                continue;
            }

            if (!TryGetPortAnchors(candidatePort, out var baseAnchor, out var tipAnchor))
            {
                continue;
            }

            var baseAnchorScreen = WorldToScreen(baseAnchor);
            var tipAnchorScreen = WorldToScreen(tipAnchor);
            if (!TryResolveLineGrabSegment(baseAnchorScreen, tipAnchorScreen, PortLineGrabLength, out var grabStart, out var grabEnd))
            {
                continue;
            }

            var distance = ComputeDistanceToSegment(pointerScreen, grabStart, grabEnd);
            if (distance <= PortLineGrabDistance && distance < nearestDistance)
            {
                nearestDistance = distance;
                target = new PortLineGrabTarget(candidatePort, null);
                found = true;
            }
        }

        return found;
    }

    private bool TryResolveConnectedLineAnchors(
        PortKey outputPort,
        PortKey inputPort,
        out Point outputAnchor,
        out Point inputAnchor)
    {
        outputAnchor = default;
        inputAnchor = default;
        if (TryGetNodeCenter(outputPort.NodeId, out var sourceCenter) &&
            TryGetNodeCenter(inputPort.NodeId, out var targetCenter) &&
            TryResolveNodeBorderAnchor(outputPort.NodeId, targetCenter, out var sourceBorderAnchor) &&
            TryResolveNodeBorderAnchor(inputPort.NodeId, sourceCenter, out var targetBorderAnchor))
        {
            outputAnchor = sourceBorderAnchor;
            inputAnchor = targetBorderAnchor;
            return true;
        }

        return TryGetPortAnchor(outputPort, out outputAnchor) &&
               TryGetPortAnchor(inputPort, out inputAnchor);
    }

    private static PortLineGrabTarget ResolveLineGrabTargetWithModifiers(
        PortLineGrabTarget target,
        KeyModifiers modifiers)
    {
        if (target.DetachedEdge is not Edge edge ||
            (modifiers & KeyModifiers.Control) == 0)
        {
            return target;
        }

        var invertedSource = target.SourcePort.Direction == PortDirection.Output
            ? new PortKey(edge.ToNodeId, edge.ToPort, PortDirection.Input)
            : new PortKey(edge.FromNodeId, edge.FromPort, PortDirection.Output);
        return new PortLineGrabTarget(invertedSource, edge);
    }

    private bool TryResolveOutputDetachEdgeForDrag(PortKey outputPort, Point pointerScreen, out Edge detachedEdge)
    {
        detachedEdge = default!;
        if (outputPort.Direction != PortDirection.Output)
        {
            return false;
        }

        var candidates = _edgeSnapshot
            .Where(edge => edge.FromNodeId == outputPort.NodeId &&
                           string.Equals(edge.FromPort, outputPort.PortName, StringComparison.Ordinal))
            .ToArray();
        if (candidates.Length == 0)
        {
            return false;
        }

        if (candidates.Length == 1)
        {
            detachedEdge = candidates[0];
            return true;
        }

        var nearestDistance = double.MaxValue;
        Edge? bestEdge = null;
        foreach (var candidate in candidates)
        {
            var inputPort = new PortKey(candidate.ToNodeId, candidate.ToPort, PortDirection.Input);
            if (!TryResolveConnectedLineAnchors(outputPort, inputPort, out var outputAnchor, out var inputAnchor))
            {
                continue;
            }

            var outputAnchorScreen = WorldToScreen(outputAnchor);
            var inputAnchorScreen = WorldToScreen(inputAnchor);
            var distance = ComputeDistanceToSegment(pointerScreen, outputAnchorScreen, inputAnchorScreen);
            if (distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = distance;
            bestEdge = candidate;
        }

        detachedEdge = bestEdge ?? candidates[0];
        return true;
    }

    private static (PortKey FromPort, PortKey ToPort) ResolveConnectionOrder(PortKey first, PortKey second)
    {
        return first.Direction == PortDirection.Output
            ? (first, second)
            : (second, first);
    }

    private bool TryGetPortAnchor(PortKey key, out Point anchor)
    {
        anchor = default;

        if (_portHandles.TryGetValue(key, out var visual))
        {
            if (TryResolveVisualAnchorPoint(visual, visual.TipLocalPoint, out anchor))
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
        var edgeAnchor = GraphPortLayoutController.ResolveAnchor(
            nodePosition,
            cardWidth,
            cardHeight,
            anchorPlan,
            PortAnchorEdgePadding);
        anchor = edgeAnchor + ResolvePortTipOffset(anchorPlan.Side, key.Direction);
        return true;
    }

    private bool TryGetPortAnchors(PortKey key, out Point baseAnchor, out Point tipAnchor)
    {
        baseAnchor = default;
        tipAnchor = default;

        if (_portHandles.TryGetValue(key, out var visual))
        {
            if (TryResolveVisualAnchorPoint(visual, visual.EdgeLocalPoint, out baseAnchor) &&
                TryResolveVisualAnchorPoint(visual, visual.TipLocalPoint, out tipAnchor))
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
        baseAnchor = GraphPortLayoutController.ResolveAnchor(
            nodePosition,
            cardWidth,
            cardHeight,
            anchorPlan,
            PortAnchorEdgePadding);
        tipAnchor = baseAnchor + ResolvePortTipOffset(anchorPlan.Side, key.Direction);
        return true;
    }

    private bool TryGetNodeCenter(NodeId nodeId, out Point center)
    {
        center = default;
        if (!_nodePositions.TryGetValue(nodeId, out var nodePosition))
        {
            return false;
        }

        center = GraphPortLayoutController.ResolveNodeCenter(
            nodePosition,
            GetCardWidth(nodeId),
            GetCardHeight(nodeId));
        return true;
    }

    private bool TryResolveNodeBorderAnchor(NodeId nodeId, Point towardPoint, out Point borderAnchor)
    {
        borderAnchor = default;
        if (!TryGetNodeCenter(nodeId, out var nodeCenter))
        {
            return false;
        }

        return GraphPortLayoutController.TryResolveBorderIntersection(
            nodeCenter,
            GetCardWidth(nodeId),
            GetCardHeight(nodeId),
            towardPoint,
            out borderAnchor);
    }

    private bool TryResolveVisualAnchorPoint(PortHandleVisual visual, Point localPoint, out Point anchor)
    {
        var width = visual.HitArea.Bounds.Width;
        var height = visual.HitArea.Bounds.Height;
        if (width <= 0 || height <= 0)
        {
            anchor = default;
            return false;
        }

        var translated = visual.HitArea.TranslatePoint(localPoint, _graphLayer);
        if (translated.HasValue)
        {
            anchor = translated.Value;
            return true;
        }

        anchor = default;
        return false;
    }

    private IBrush ResolvePortHandleStroke(PortDirection direction, NodePortRole role, bool connected)
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
                    EdgePoint: new Point(1, 5),
                    TipPoint: new Point(17, 5))
                : new PortGlyphGeometry(
                    Width: 12,
                    Height: 10,
                    EdgePoint: new Point(1, 5),
                    TipPoint: new Point(11, 5)),
            GraphPortSide.Bottom => direction == PortDirection.Output
                ? new PortGlyphGeometry(
                    Width: 10,
                    Height: 12,
                    EdgePoint: new Point(5, 1),
                    TipPoint: new Point(5, 11))
                : new PortGlyphGeometry(
                    Width: 10,
                    Height: 18,
                    EdgePoint: new Point(5, 1),
                    TipPoint: new Point(5, 17)),
            _ => direction == PortDirection.Input
                ? new PortGlyphGeometry(
                    Width: 10,
                    Height: 18,
                    EdgePoint: new Point(5, 17),
                    TipPoint: new Point(5, 1))
                : new PortGlyphGeometry(
                    Width: 10,
                    Height: 18,
                    EdgePoint: new Point(5, 17),
                    TipPoint: new Point(5, 7))
        };
    }

    private static IList<Point> ComputeArrowhead(Point tip, Point from)
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

        var baseCenterX = tip.X - nx * ArrowHeadLength;
        var baseCenterY = tip.Y - ny * ArrowHeadLength;

        return new[]
        {
            tip,
            new Point(baseCenterX + px * ArrowHeadHalfWidth, baseCenterY + py * ArrowHeadHalfWidth),
            new Point(baseCenterX - px * ArrowHeadHalfWidth, baseCenterY - py * ArrowHeadHalfWidth)
        };
    }

    private static bool TryResolveLineGrabSegment(
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
            nodeBorderAnchor.X + dx * scale,
            nodeBorderAnchor.Y + dy * scale);
        return true;
    }

    private static double ComputeDistanceToSegment(Point point, Point segmentStart, Point segmentEnd)
    {
        var dx = segmentEnd.X - segmentStart.X;
        var dy = segmentEnd.Y - segmentStart.Y;
        var lengthSquared = dx * dx + dy * dy;
        if (lengthSquared < 0.0001)
        {
            return Math.Sqrt(
                ((point.X - segmentStart.X) * (point.X - segmentStart.X)) +
                ((point.Y - segmentStart.Y) * (point.Y - segmentStart.Y)));
        }

        var projection = ((point.X - segmentStart.X) * dx + (point.Y - segmentStart.Y) * dy) / lengthSquared;
        projection = Math.Clamp(projection, 0.0, 1.0);

        var nearestX = segmentStart.X + dx * projection;
        var nearestY = segmentStart.Y + dy * projection;
        var deltaX = point.X - nearestX;
        var deltaY = point.Y - nearestY;
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    private static Vector ResolvePortTipOffset(GraphPortSide side, PortDirection direction)
    {
        var geometry = ResolvePortGlyphGeometry(side, direction);
        return new Vector(
            geometry.TipPoint.X - geometry.EdgePoint.X,
            geometry.TipPoint.Y - geometry.EdgePoint.Y);
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
    private readonly record struct PortLineGrabTarget(PortKey SourcePort, Edge? DetachedEdge);

    private sealed record PortGlyphGeometry(
        double Width,
        double Height,
        Point EdgePoint,
        Point TipPoint);

    private sealed record PortHandleVisual(
        Border HitArea,
        NodePortRole Role,
        Point TipLocalPoint,
        Point EdgeLocalPoint);
}
