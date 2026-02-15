using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine;
using Editor.IO;
using Polygon = Avalonia.Controls.Shapes.Polygon;
using Polyline = Avalonia.Controls.Shapes.Polyline;

namespace App;

public partial class MainWindow : Window
{
    private const double NodeCardWidth = 170;
    private const double NodeCardHeight = 64;
    private const double NodeCanvasPadding = 8;
    private const int NodeCanvasColumns = 3;
    private const double MinZoomScale = 0.35;
    private const double MaxZoomScale = 2.5;
    private const double ZoomStepFactor = 1.1;
    private static readonly IBrush NodeCardBackground = Brush.Parse("#2B2F36");
    private static readonly IBrush NodeCardBorder = Brush.Parse("#8CA8C9");
    private static readonly IBrush NodeCardSelectedBorder = Brush.Parse("#5EC0FF");
    private static readonly IBrush NodeCardDragBorder = Brush.Parse("#F5C26B");
    private static readonly IBrush NodeCardTitleForeground = Brush.Parse("#F2F2F2");
    private static readonly IBrush NodeCardDetailForeground = Brush.Parse("#B8C2CE");
    private static readonly IBrush WireStroke = Brush.Parse("#87A7CB");
    private static readonly IBrush WireArrowFill = Brush.Parse("#87A7CB");
    private const double WireThickness = 2;
    private const double WireArrowLength = 9;
    private const double WireArrowWidth = 5;
    private readonly IEditorEngine _editorEngine;
    private readonly IImageLoader _imageLoader;
    private readonly IImageExporter _imageExporter;
    private readonly Dictionary<NodeId, Node> _nodeLookup = new();
    private readonly Dictionary<NodeId, Point> _nodePositions = new();
    private readonly Dictionary<NodeId, Border> _nodeCards = new();
    private readonly List<Control> _wireVisuals = new();
    private readonly Dictionary<int, NodeId> _previewSlots = new();
    private readonly Canvas _graphLayer = new();
    private IReadOnlyList<Edge> _edgeSnapshot = Array.Empty<Edge>();
    private NodeId? _activeDragNodeId;
    private NodeId? _selectedNodeId;
    private int? _activePreviewSlot;
    private bool _isPanningCanvas;
    private bool _isViewTransformInitialized;
    private Point _lastPanPointerScreen;
    private Point _activeDragOffset;
    private Vector _panOffset;
    private double _zoomScale = 1.0;
    private WriteableBitmap? _previewBitmap;

    public MainWindow()
        : this(new BootstrapEditorEngine(), new SkiaImageLoader(), new SkiaImageExporter())
    {
    }

    public MainWindow(IEditorEngine editorEngine, IImageLoader imageLoader, IImageExporter imageExporter)
    {
        _editorEngine = editorEngine;
        _imageLoader = imageLoader;
        _imageExporter = imageExporter;

        InitializeComponent();
        WireEvents();
        InitializeUiState();
    }

    protected override void OnClosed(EventArgs e)
    {
        _editorEngine.PreviewUpdated -= OnPreviewUpdated;
        NodeCanvas.SizeChanged -= OnNodeCanvasSizeChanged;
        NodeCanvas.PointerMoved -= OnNodeCanvasPointerMoved;
        NodeCanvas.PointerReleased -= OnNodeCanvasPointerReleased;
        NodeCanvas.PointerCaptureLost -= OnNodeCanvasPointerCaptureLost;
        NodeCanvas.PointerWheelChanged -= OnNodeCanvasPointerWheelChanged;
        KeyDown -= OnWindowKeyDown;
        _previewBitmap?.Dispose();
        base.OnClosed(e);
    }

    private void WireEvents()
    {
        _editorEngine.PreviewUpdated += OnPreviewUpdated;
        NodeCanvas.SizeChanged += OnNodeCanvasSizeChanged;
        NodeCanvas.PointerMoved += OnNodeCanvasPointerMoved;
        NodeCanvas.PointerReleased += OnNodeCanvasPointerReleased;
        NodeCanvas.PointerCaptureLost += OnNodeCanvasPointerCaptureLost;
        NodeCanvas.PointerWheelChanged += OnNodeCanvasPointerWheelChanged;
        KeyDown += OnWindowKeyDown;
    }

    private void InitializeUiState()
    {
        EnsureGraphLayer();
        NodeTypeComboBox.ItemsSource = _editorEngine.AvailableNodeTypes;
        NodeTypeComboBox.SelectedIndex = 0;
        RefreshGraphBindings();
        SetStatus("Ready");
    }

    private async void OnLoadClicked(object? sender, RoutedEventArgs e)
    {
        if (StorageProvider is null)
        {
            SetStatus("Storage provider unavailable.");
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Load Image",
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Images")
                    {
                        Patterns = new[] { "*.png", "*.jpg", "*.jpeg" }
                    }
                }
            });

        var file = files.FirstOrDefault();
        if (file is null)
        {
            return;
        }

        var path = file.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            SetStatus("Selected file has no local path.");
            return;
        }

        if (!_imageLoader.TryLoad(path, out var image, out var error) || image is null)
        {
            SetStatus($"Load failed: {error}");
            return;
        }

        _editorEngine.SetInputImage(image);
        RequestPreviewForActiveSlot();
        SetStatus($"Loaded: {Path.GetFileName(path)}");
    }

    private async void OnExportClicked(object? sender, RoutedEventArgs e)
    {
        if (!_editorEngine.TryRenderOutput(out var image, out var renderError) || image is null)
        {
            SetStatus($"Render failed: {renderError}");
            return;
        }

        if (StorageProvider is null)
        {
            SetStatus("Storage provider unavailable.");
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Export Image",
                SuggestedFileName = "output.png",
                DefaultExtension = "png",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("PNG Image") { Patterns = new[] { "*.png" } },
                    new FilePickerFileType("JPEG Image") { Patterns = new[] { "*.jpg", "*.jpeg" } }
                }
            });

        if (file is null)
        {
            return;
        }

        var path = file.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            SetStatus("Selected save location has no local path.");
            return;
        }

        if (!_imageExporter.TryExport(image, path, out var exportError))
        {
            SetStatus($"Export failed: {exportError}");
            return;
        }

        SetStatus($"Exported: {Path.GetFileName(path)}");
    }

    private void OnBuildChainClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var transform = _editorEngine.AddNode(NodeTypes.Transform);
            var exposure = _editorEngine.AddNode(NodeTypes.ExposureContrast);
            var blur = _editorEngine.AddNode(NodeTypes.Blur);
            var sharpen = _editorEngine.AddNode(NodeTypes.Sharpen);

            _editorEngine.Connect(_editorEngine.InputNodeId, "Image", transform, "Image");
            _editorEngine.Connect(transform, "Image", exposure, "Image");
            _editorEngine.Connect(exposure, "Image", blur, "Image");
            _editorEngine.Connect(blur, "Image", sharpen, "Image");
            _editorEngine.Connect(sharpen, "Image", _editorEngine.OutputNodeId, "Image");

            RefreshGraphBindings();
            SetStatus("Built 4-node chain.");
        }
        catch (Exception exception)
        {
            SetStatus($"Build chain failed: {exception.Message}");
        }
    }

    private void OnUndoClicked(object? sender, RoutedEventArgs e)
    {
        _editorEngine.Undo();
        RefreshGraphBindings();
        SetStatus("Undo");
    }

    private void OnRedoClicked(object? sender, RoutedEventArgs e)
    {
        _editorEngine.Redo();
        RefreshGraphBindings();
        SetStatus("Redo");
    }

    private void OnAddNodeClicked(object? sender, RoutedEventArgs e)
    {
        if (NodeTypeComboBox.SelectedItem is not string nodeType)
        {
            SetStatus("Select a node type first.");
            return;
        }

        try
        {
            var nodeId = _editorEngine.AddNode(nodeType);
            _nodePositions[nodeId] = GetViewportCenterWorld();
            RefreshGraphBindings();
            SetStatus($"Node '{nodeType}' added.");
        }
        catch (Exception exception)
        {
            SetStatus($"Add node failed: {exception.Message}");
        }
    }

    private void OnConnectClicked(object? sender, RoutedEventArgs e)
    {
        if (FromNodeComboBox.SelectedItem is not NodeOption fromNode ||
            ToNodeComboBox.SelectedItem is not NodeOption toNode ||
            FromPortComboBox.SelectedItem is not string fromPort ||
            ToPortComboBox.SelectedItem is not string toPort)
        {
            SetStatus("Select source node/port and target node/port.");
            return;
        }

        try
        {
            _editorEngine.Connect(fromNode.Id, fromPort, toNode.Id, toPort);
            RefreshGraphBindings();
            SetStatus($"Connected {fromNode.Type}.{fromPort} -> {toNode.Type}.{toPort}");
        }
        catch (Exception exception)
        {
            SetStatus($"Connect failed: {exception.Message}");
        }
    }

    private void OnFromNodeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RefreshFromPorts();
    }

    private void OnToNodeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RefreshToPorts();
    }

    private void OnParameterNodeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RefreshParameterNames();
    }

    private void OnParameterNameSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RefreshParameterValueInput();
    }

    private void OnApplyParameterClicked(object? sender, RoutedEventArgs e)
    {
        if (ParameterNodeComboBox.SelectedItem is not NodeOption nodeOption)
        {
            SetStatus("Select a node to edit.");
            return;
        }

        if (ParameterNameComboBox.SelectedItem is not string parameterName)
        {
            SetStatus("Select a parameter.");
            return;
        }

        if (!_nodeLookup.TryGetValue(nodeOption.Id, out var node))
        {
            SetStatus("Selected node no longer exists.");
            return;
        }

        var nodeType = NodeTypeCatalog.GetByName(node.Type);
        if (!nodeType.Parameters.TryGetValue(parameterName, out var definition))
        {
            SetStatus($"Parameter '{parameterName}' not found.");
            return;
        }

        try
        {
            var rawText = ParameterValueTextBox.Text ?? string.Empty;
            var value = ParseParameterValue(definition, rawText);
            _editorEngine.SetParameter(node.Id, parameterName, value);
            RefreshGraphBindings();
            SetStatus($"Updated {node.Type}.{parameterName}");
        }
        catch (Exception exception)
        {
            SetStatus($"Set parameter failed: {exception.Message}");
        }
    }

    private void OnPreviewUpdated(object? sender, PreviewFrame frame)
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                _previewBitmap?.Dispose();
                var bitmap = new WriteableBitmap(
                    new PixelSize(frame.Width, frame.Height),
                    new Avalonia.Vector(96, 96),
                    PixelFormat.Rgba8888,
                    AlphaFormat.Unpremul);

                using (var locked = bitmap.Lock())
                {
                    Marshal.Copy(frame.RgbaBytes, 0, locked.Address, frame.RgbaBytes.Length);
                }

                _previewBitmap = bitmap;
                PreviewImage.Source = _previewBitmap;
                SetStatus(_editorEngine.Status);
            }
            catch (Exception exception)
            {
                SetStatus($"Preview update failed: {exception.Message}");
            }
        });
    }

    private void RefreshGraphBindings()
    {
        var nodes = _editorEngine.Nodes;
        var edges = _editorEngine.Edges;
        _edgeSnapshot = edges;

        _nodeLookup.Clear();
        foreach (var node in nodes)
        {
            _nodeLookup[node.Id] = node;
        }

        var nodeOptions = nodes
            .Select(node => new NodeOption(node.Id, node.Type))
            .ToArray();

        RefreshNodeCanvas(nodes, edges);
        NodeListBox.ItemsSource = nodeOptions.Select(option => option.ToString()).ToArray();
        EdgeListBox.ItemsSource = edges.Select(DescribeEdge).ToArray();

        SyncComboSelection(FromNodeComboBox, nodeOptions);
        SyncComboSelection(ToNodeComboBox, nodeOptions);
        SyncComboSelection(ParameterNodeComboBox, nodeOptions);

        RefreshFromPorts();
        RefreshToPorts();
        RefreshParameterNames();
        PruneSelectionAndPreviewSlots(nodes);

        UndoButton.IsEnabled = _editorEngine.CanUndo;
        RedoButton.IsEnabled = _editorEngine.CanRedo;
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
        foreach (var node in nodes)
        {
            var card = CreateNodeCard(node);
            SetNodeCardPosition(card, _nodePositions[node.Id]);
            _graphLayer.Children.Add(card);
            _nodeCards[node.Id] = card;
        }

        RenderWireVisuals(edges);
        ApplyNodeSelectionVisuals();
    }

    private Border CreateNodeCard(Node node)
    {
        var title = new TextBlock
        {
            Text = node.Type,
            FontWeight = FontWeight.SemiBold,
            Foreground = NodeCardTitleForeground
        };
        var detail = new TextBlock
        {
            Text = node.Id.ToString()[..8],
            Foreground = NodeCardDetailForeground
        };

        var content = new StackPanel
        {
            Spacing = 3
        };
        content.Children.Add(title);
        content.Children.Add(detail);

        var card = new Border
        {
            Tag = node.Id,
            Width = NodeCardWidth,
            MinHeight = NodeCardHeight,
            Padding = new Thickness(8),
            Background = NodeCardBackground,
            BorderBrush = NodeCardBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Cursor = new Cursor(StandardCursorType.SizeAll),
            Child = content
        };

        card.PointerPressed += OnNodeCardPointerPressed;
        card.PointerMoved += OnNodeCardPointerMoved;
        card.PointerReleased += OnNodeCardPointerReleased;
        card.PointerCaptureLost += OnNodeCardPointerCaptureLost;
        return card;
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
            NodeCanvas.Focus();
            e.Handled = true;
        }
    }

    private void OnNodeCardPointerPressed(object? sender, PointerPressedEventArgs e)
    {
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

        card.BorderBrush = NodeCardDragBorder;
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
        card.BorderBrush = _selectedNodeId == nodeId ? NodeCardSelectedBorder : NodeCardBorder;
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
            _panOffset = new Vector(e.NewSize.Width / 2, e.NewSize.Height / 2);
            _isViewTransformInitialized = true;
            ApplyGraphTransform();
        }

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
        if (!_isPanningCanvas)
        {
            return;
        }

        StopCanvasPan();
    }

    private void OnNodeCanvasPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (Math.Abs(e.Delta.Y) < double.Epsilon)
        {
            return;
        }

        var pointerScreen = e.GetPosition(NodeCanvas);
        var worldAtPointer = ScreenToWorld(pointerScreen);
        var zoomFactor = e.Delta.Y > 0
            ? ZoomStepFactor
            : 1 / ZoomStepFactor;
        var nextZoom = Math.Clamp(_zoomScale * zoomFactor, MinZoomScale, MaxZoomScale);
        if (Math.Abs(nextZoom - _zoomScale) < 0.0001)
        {
            return;
        }

        _zoomScale = nextZoom;
        _panOffset = new Vector(
            pointerScreen.X - (worldAtPointer.X * _zoomScale),
            pointerScreen.Y - (worldAtPointer.Y * _zoomScale));
        ApplyGraphTransform();
        e.Handled = true;
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if ((e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) != 0)
        {
            return;
        }

        if (GetFocusedElement() is TextBox)
        {
            return;
        }

        if (IsPreviewResetKey(e.Key))
        {
            _activePreviewSlot = null;
            _editorEngine.RequestPreviewRender();
            SetStatus("Preview reset to output.");
            e.Handled = true;
            return;
        }

        if (!TryMapPreviewSlot(e.Key, out var slot))
        {
            return;
        }

        if (_selectedNodeId is NodeId selectedNodeId && _nodeLookup.ContainsKey(selectedNodeId))
        {
            _previewSlots[slot] = selectedNodeId;
            _activePreviewSlot = slot;
            RequestPreviewForActiveSlot();
            SetStatus($"Assigned preview slot {slot} to {_nodeLookup[selectedNodeId].Type}.");
            e.Handled = true;
            return;
        }

        if (!_previewSlots.TryGetValue(slot, out _))
        {
            SetStatus($"Preview slot {slot} is empty.");
            e.Handled = true;
            return;
        }

        _activePreviewSlot = slot;
        RequestPreviewForActiveSlot();
        SetStatus($"Preview slot {slot} activated.");
        e.Handled = true;
    }

    private void RequestPreviewForActiveSlot()
    {
        if (_activePreviewSlot is int activeSlot &&
            _previewSlots.TryGetValue(activeSlot, out var previewNodeId) &&
            _nodeLookup.ContainsKey(previewNodeId))
        {
            _editorEngine.RequestPreviewRender(previewNodeId);
            return;
        }

        _editorEngine.RequestPreviewRender();
    }

    private void PruneSelectionAndPreviewSlots(IReadOnlyList<Node> nodes)
    {
        var liveNodeIds = nodes.Select(node => node.Id).ToHashSet();
        if (_selectedNodeId is NodeId selectedNodeId && !liveNodeIds.Contains(selectedNodeId))
        {
            _selectedNodeId = null;
        }

        var removedSlots = _previewSlots
            .Where(slot => !liveNodeIds.Contains(slot.Value))
            .Select(slot => slot.Key)
            .ToArray();
        foreach (var slot in removedSlots)
        {
            _previewSlots.Remove(slot);
        }

        if (_activePreviewSlot is int activeSlot && !_previewSlots.ContainsKey(activeSlot))
        {
            _activePreviewSlot = null;
            _editorEngine.RequestPreviewRender();
            SetStatus("Active preview slot was removed. Showing output.");
        }

        ApplyNodeSelectionVisuals();
    }

    private void SetSelectedNode(NodeId nodeId)
    {
        if (_selectedNodeId == nodeId)
        {
            return;
        }

        _selectedNodeId = nodeId;
        ApplyNodeSelectionVisuals();
    }

    private void ApplyNodeSelectionVisuals()
    {
        foreach (var (nodeId, card) in _nodeCards)
        {
            if (_activeDragNodeId == nodeId)
            {
                card.BorderBrush = NodeCardDragBorder;
                continue;
            }

            card.BorderBrush = _selectedNodeId == nodeId
                ? NodeCardSelectedBorder
                : NodeCardBorder;
        }
    }

    private void RenderWireVisuals(IReadOnlyList<Edge> edges)
    {
        foreach (var visual in _wireVisuals)
        {
            _graphLayer.Children.Remove(visual);
        }

        _wireVisuals.Clear();

        var insertIndex = 0;
        foreach (var edge in edges)
        {
            if (!_nodePositions.TryGetValue(edge.FromNodeId, out var fromPosition) ||
                !_nodePositions.TryGetValue(edge.ToNodeId, out var toPosition))
            {
                continue;
            }

            var fromCardWidth = GetCardWidth(edge.FromNodeId);
            var fromCardHeight = GetCardHeight(edge.FromNodeId);
            var toCardWidth = GetCardWidth(edge.ToNodeId);
            var toCardHeight = GetCardHeight(edge.ToNodeId);
            var fromCenterX = fromPosition.X + (fromCardWidth / 2);
            var toCenterX = toPosition.X + (toCardWidth / 2);
            var isLeftToRight = fromCenterX <= toCenterX;
            var sourceAnchor = new Point(
                isLeftToRight ? fromPosition.X + fromCardWidth : fromPosition.X,
                fromPosition.Y + (fromCardHeight / 2));
            var targetAnchor = new Point(
                isLeftToRight ? toPosition.X : toPosition.X + toCardWidth,
                toPosition.Y + (toCardHeight / 2));

            var middleX = (sourceAnchor.X + targetAnchor.X) / 2;
            var bendA = new Point(middleX, sourceAnchor.Y);
            var bendB = new Point(middleX, targetAnchor.Y);

            var wire = new Polyline
            {
                IsHitTestVisible = false,
                Stroke = WireStroke,
                StrokeThickness = WireThickness,
                Points = new[] { sourceAnchor, bendA, bendB, targetAnchor }
            };

            var arrow = CreateArrowHead(bendB, targetAnchor);
            _graphLayer.Children.Insert(insertIndex++, wire);
            _graphLayer.Children.Insert(insertIndex++, arrow);
            _wireVisuals.Add(wire);
            _wireVisuals.Add(arrow);
        }
    }

    private static bool TryMapPreviewSlot(Key key, out int slot)
    {
        slot = key switch
        {
            Key.D1 or Key.NumPad1 => 1,
            Key.D2 or Key.NumPad2 => 2,
            Key.D3 or Key.NumPad3 => 3,
            Key.D4 or Key.NumPad4 => 4,
            Key.D5 or Key.NumPad5 => 5,
            _ => 0
        };

        return slot != 0;
    }

    private static bool IsPreviewResetKey(Key key)
    {
        return key is Key.D0 or Key.NumPad0;
    }

    private InputElement? GetFocusedElement()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        return topLevel?.FocusManager?.GetFocusedElement() as InputElement;
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
                Fill = WireArrowFill,
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
            Fill = WireArrowFill,
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
        if (NodeCanvas.Children.Count == 1 && ReferenceEquals(NodeCanvas.Children[0], _graphLayer))
        {
            return;
        }

        NodeCanvas.Children.Clear();
        NodeCanvas.Children.Add(_graphLayer);
        ApplyGraphTransform();
    }

    private bool IsCanvasBackgroundSource(object? source)
    {
        return ReferenceEquals(source, NodeCanvas) || ReferenceEquals(source, _graphLayer);
    }

    private void StopCanvasPan()
    {
        _isPanningCanvas = false;
    }

    private void ApplyGraphTransform()
    {
        var transforms = new Transforms
        {
            new ScaleTransform(_zoomScale, _zoomScale),
            new TranslateTransform(_panOffset.X, _panOffset.Y)
        };
        _graphLayer.RenderTransform = new TransformGroup { Children = transforms };
    }

    private Point ScreenToWorld(Point screenPoint)
    {
        return new Point(
            (screenPoint.X - _panOffset.X) / _zoomScale,
            (screenPoint.Y - _panOffset.Y) / _zoomScale);
    }

    private Point GetViewportCenterWorld()
    {
        if (NodeCanvas.Bounds.Width <= 0 || NodeCanvas.Bounds.Height <= 0)
        {
            return GetDefaultNodePosition(_nodePositions.Count);
        }

        var screenCenter = new Point(NodeCanvas.Bounds.Width / 2, NodeCanvas.Bounds.Height / 2);
        return ScreenToWorld(screenCenter);
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

    private static void SyncComboSelection(ComboBox comboBox, IReadOnlyList<NodeOption> options)
    {
        var current = comboBox.SelectedItem as NodeOption;
        comboBox.ItemsSource = options;
        if (current is null)
        {
            comboBox.SelectedIndex = options.Count > 0 ? 0 : -1;
            return;
        }

        var match = options.FirstOrDefault(option => option.Id == current.Id);
        comboBox.SelectedItem = match ?? options.FirstOrDefault();
    }

    private void RefreshFromPorts()
    {
        if (FromNodeComboBox.SelectedItem is not NodeOption selected ||
            !_nodeLookup.TryGetValue(selected.Id, out var node))
        {
            FromPortComboBox.ItemsSource = Array.Empty<string>();
            return;
        }

        var ports = NodeTypeCatalog.GetByName(node.Type)
            .Outputs
            .Select(port => port.Name)
            .ToArray();
        FromPortComboBox.ItemsSource = ports;
        FromPortComboBox.SelectedIndex = ports.Length > 0 ? 0 : -1;
    }

    private void RefreshToPorts()
    {
        if (ToNodeComboBox.SelectedItem is not NodeOption selected ||
            !_nodeLookup.TryGetValue(selected.Id, out var node))
        {
            ToPortComboBox.ItemsSource = Array.Empty<string>();
            return;
        }

        var ports = NodeTypeCatalog.GetByName(node.Type)
            .Inputs
            .Select(port => port.Name)
            .ToArray();
        ToPortComboBox.ItemsSource = ports;
        ToPortComboBox.SelectedIndex = ports.Length > 0 ? 0 : -1;
    }

    private void RefreshParameterNames()
    {
        if (ParameterNodeComboBox.SelectedItem is not NodeOption selected ||
            !_nodeLookup.TryGetValue(selected.Id, out var node))
        {
            ParameterNameComboBox.ItemsSource = Array.Empty<string>();
            ParameterValueTextBox.Text = string.Empty;
            return;
        }

        var parameterNames = NodeTypeCatalog.GetByName(node.Type)
            .Parameters
            .Keys
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        ParameterNameComboBox.ItemsSource = parameterNames;
        ParameterNameComboBox.SelectedIndex = parameterNames.Length > 0 ? 0 : -1;
        RefreshParameterValueInput();
    }

    private void RefreshParameterValueInput()
    {
        if (ParameterNodeComboBox.SelectedItem is not NodeOption selected ||
            !_nodeLookup.TryGetValue(selected.Id, out var node) ||
            ParameterNameComboBox.SelectedItem is not string parameterName)
        {
            ParameterValueTextBox.Text = string.Empty;
            return;
        }

        ParameterValueTextBox.Text = FormatParameterValue(node.GetParameter(parameterName));
    }

    private static ParameterValue ParseParameterValue(NodeParameterDefinition definition, string rawText)
    {
        return definition.Kind switch
        {
            ParameterValueKind.Float => ParameterValue.Float(float.Parse(rawText, CultureInfo.InvariantCulture)),
            ParameterValueKind.Integer => ParameterValue.Integer(int.Parse(rawText, CultureInfo.InvariantCulture)),
            ParameterValueKind.Boolean => ParameterValue.Boolean(bool.Parse(rawText)),
            ParameterValueKind.Enum => ParameterValue.Enum(rawText.Trim()),
            _ => throw new InvalidOperationException($"Unsupported parameter kind '{definition.Kind}'.")
        };
    }

    private static string FormatParameterValue(ParameterValue value)
    {
        return value.Kind switch
        {
            ParameterValueKind.Float => value.AsFloat().ToString("0.###", CultureInfo.InvariantCulture),
            ParameterValueKind.Integer => value.AsInteger().ToString(CultureInfo.InvariantCulture),
            ParameterValueKind.Boolean => value.AsBoolean().ToString(),
            ParameterValueKind.Enum => value.AsEnum(),
            _ => string.Empty
        };
    }

    private string DescribeEdge(Edge edge)
    {
        var fromLabel = _nodeLookup.TryGetValue(edge.FromNodeId, out var fromNode)
            ? fromNode.Type
            : edge.FromNodeId.ToString();
        var toLabel = _nodeLookup.TryGetValue(edge.ToNodeId, out var toNode)
            ? toNode.Type
            : edge.ToNodeId.ToString();
        return $"{fromLabel}.{edge.FromPort} -> {toLabel}.{edge.ToPort}";
    }

    private void SetStatus(string message)
    {
        StatusTextBlock.Text = message;
    }

    private sealed record NodeOption(NodeId Id, string Type)
    {
        public override string ToString() => $"{Type} ({Id})";
    }
}
