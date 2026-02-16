using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
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
                var arrowPoints = GraphWireGeometryController.ComputeArrowhead(
                    arrowTip,
                    arrowFrom,
                    ArrowHeadLength,
                    ArrowHeadHalfWidth);
                if (arrowPoints.Count > 0)
                {
                    var arrow = new Polygon
                    {
                        IsHitTestVisible = false,
                        Points = [.. arrowPoints],
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

    private readonly record struct PortKey(NodeId NodeId, string PortName, PortDirection Direction);
    private readonly record struct PortLineGrabTarget(PortKey SourcePort, Edge? DetachedEdge);

    private sealed record PortHandleVisual(
        Border HitArea,
        NodePortRole Role,
        Point TipLocalPoint,
        Point EdgeLocalPoint);
}
