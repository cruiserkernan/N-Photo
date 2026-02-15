using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using App.Views;
using App.Workspace;
using App.Presentation.Controllers;
using Editor.Application;
using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine;
using Editor.Engine.Abstractions;
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
    private const double PortHandleSize = 10;
    private const double PortHandleVerticalSpacing = 16;
    private const double PortSnapDistancePixels = 18;
    private const double WireThickness = 2;
    private const double WireArrowLength = 9;
    private const double WireArrowWidth = 5;
    private readonly IEditorSession _editorSession;
    private readonly IImageLoader _imageLoader;
    private readonly IImageExporter _imageExporter;
    private readonly IBrush _nodeCardBackground;
    private readonly IBrush _nodeCardBorder;
    private readonly IBrush _nodeCardSelectedBorder;
    private readonly IBrush _nodeCardDragBorder;
    private readonly IBrush _nodeCardTitleForeground;
    private readonly IBrush _nodeCardDetailForeground;
    private readonly IBrush _wireStroke;
    private readonly IBrush _wireArrowFill;
    private readonly NodeActionController _nodeActionController;
    private readonly PropertiesPanelController _propertiesPanelController;
    private readonly Dictionary<NodeId, Node> _nodeLookup = new();
    private readonly Dictionary<NodeId, Point> _nodePositions = new();
    private readonly Dictionary<NodeId, Border> _nodeCards = new();
    private readonly Dictionary<PortKey, Border> _portHandles = new();
    private readonly List<Button> _nodeStripButtons = new();
    private readonly List<Control> _wireVisuals = new();
    private readonly PreviewRoutingController _previewRouting = new();
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
    private WriteableBitmap? _previewBitmap;
    private TopToolbarView ToolbarView => this.FindControl<TopToolbarView>("TopToolbar")
                                          ?? throw new InvalidOperationException("TopToolbar not found.");
    private GraphPanelView GraphPanelView => this.FindControl<GraphPanelView>("GraphPanelRoot")
                                             ?? throw new InvalidOperationException("GraphPanelRoot not found.");
    private ViewerPanelView ViewerPanelView => this.FindControl<ViewerPanelView>("ViewerPanelRoot")
                                               ?? throw new InvalidOperationException("ViewerPanelRoot not found.");
    private PropertiesPanelView PropertiesPanelView => this.FindControl<PropertiesPanelView>("PropertiesPanelRoot")
                                                       ?? throw new InvalidOperationException("PropertiesPanelRoot not found.");
    private Grid PaneSeed => this.FindControl<Grid>("PaneSeedGrid")
                             ?? throw new InvalidOperationException("PaneSeedGrid not found.");
    private Dock.Avalonia.Controls.DockControl WorkspaceDock => this.FindControl<Dock.Avalonia.Controls.DockControl>("WorkspaceDockControl")
                                                               ?? throw new InvalidOperationException("WorkspaceDockControl not found.");
    private Button ExportButton => ToolbarView.ExportButtonControl;
    private Button UndoButton => ToolbarView.UndoButtonControl;
    private Button RedoButton => ToolbarView.RedoButtonControl;
    private TextBlock StatusTextBlock => ToolbarView.StatusTextBlockControl;
    private StackPanel NodeStripHost => ToolbarView.NodeStripHostControl;
    private TextBox NodeSearchBox => ToolbarView.NodeSearchBoxControl;
    private Button NodeSearchAddButton => ToolbarView.NodeSearchAddButtonControl;
    private Canvas NodeCanvas => GraphPanelView.NodeCanvasControl;
    private Image PreviewImage => ViewerPanelView.PreviewImageControl;
    private TextBlock SelectedNodeText => PropertiesPanelView.SelectedNodeTextControl;
    private StackPanel PropertyEditorHost => PropertiesPanelView.PropertyEditorHostControl;

    public MainWindow()
        : this(CreateDefaultSession(), new SkiaImageLoader(), new SkiaImageExporter())
    {
    }

    public MainWindow(IEditorSession editorSession, IImageLoader imageLoader, IImageExporter imageExporter)
    {
        _editorSession = editorSession;
        _imageLoader = imageLoader;
        _imageExporter = imageExporter;
        _nodeActionController = new NodeActionController(
            _editorSession,
            _imageLoader,
            PickImagePathAsync,
            RequestPreviewForActiveSlot,
            RefreshPropertiesEditor,
            SetStatus);
        _propertiesPanelController = new PropertiesPanelController(
            _editorSession,
            _nodeActionController.ExecuteAsync,
            _nodeActionController.ResolveDisplayText,
            RefreshGraphBindings,
            SetStatus);

        InitializeComponent();
        _nodeCardBackground = ResolveBrush("Brush.NodeCard.Background", "#2B3038");
        _nodeCardBorder = ResolveBrush("Brush.NodeCard.Border", "#4F5D73");
        _nodeCardSelectedBorder = ResolveBrush("Brush.NodeCard.BorderSelected", "#59B5FF");
        _nodeCardDragBorder = ResolveBrush("Brush.NodeCard.BorderDragging", "#E8B874");
        _nodeCardTitleForeground = ResolveBrush("Brush.Text.Primary", "#E5EAF2");
        _nodeCardDetailForeground = ResolveBrush("Brush.Text.Muted", "#95A1B3");
        _wireStroke = ResolveBrush("Brush.Graph.Wire", "#72A6D5");
        _wireArrowFill = ResolveBrush("Brush.Graph.Wire", "#72A6D5");
        WireEvents();
        InitializeUiState();
    }

    private static IEditorSession CreateDefaultSession()
    {
        var registry = new Editor.Nodes.BuiltInNodeModuleRegistry();
        return new EditorSession(new BootstrapEditorEngine(registry), registry);
    }

    protected override void OnClosed(EventArgs e)
    {
        _editorSession.PreviewUpdated -= OnPreviewUpdated;
        ExportButton.Click -= OnExportClicked;
        UndoButton.Click -= OnUndoClicked;
        RedoButton.Click -= OnRedoClicked;
        NodeSearchAddButton.Click -= OnNodeSearchAddClicked;
        NodeSearchBox.KeyDown -= OnNodeSearchBoxKeyDown;
        UnwireNodeToolbarButtons();
        NodeCanvas.PointerPressed -= OnNodeCanvasPointerPressed;
        NodeCanvas.SizeChanged -= OnNodeCanvasSizeChanged;
        NodeCanvas.PointerMoved -= OnNodeCanvasPointerMoved;
        NodeCanvas.PointerReleased -= OnNodeCanvasPointerReleased;
        NodeCanvas.PointerCaptureLost -= OnNodeCanvasPointerCaptureLost;
        NodeCanvas.PointerWheelChanged -= OnNodeCanvasPointerWheelChanged;
        KeyDown -= OnWindowKeyDown;
        if (_editorSession is IDisposable disposableSession)
        {
            disposableSession.Dispose();
        }
        _previewBitmap?.Dispose();
        base.OnClosed(e);
    }

    private void WireEvents()
    {
        _editorSession.PreviewUpdated += OnPreviewUpdated;
        ExportButton.Click += OnExportClicked;
        UndoButton.Click += OnUndoClicked;
        RedoButton.Click += OnRedoClicked;
        NodeSearchAddButton.Click += OnNodeSearchAddClicked;
        NodeSearchBox.KeyDown += OnNodeSearchBoxKeyDown;
        NodeCanvas.PointerPressed += OnNodeCanvasPointerPressed;
        NodeCanvas.SizeChanged += OnNodeCanvasSizeChanged;
        NodeCanvas.PointerMoved += OnNodeCanvasPointerMoved;
        NodeCanvas.PointerReleased += OnNodeCanvasPointerReleased;
        NodeCanvas.PointerCaptureLost += OnNodeCanvasPointerCaptureLost;
        NodeCanvas.PointerWheelChanged += OnNodeCanvasPointerWheelChanged;
        KeyDown += OnWindowKeyDown;
    }

    private void InitializeUiState()
    {
        InitializeWorkspaceUi();
        EnsureGraphLayer();
        BuildNodeToolbarStrip();
        RefreshGraphBindings();
        SetStatus("Ready");
    }

    private void InitializeWorkspaceUi()
    {
        DetachSeedPanels();

        var workspaceLayoutManager = new WorkspaceLayoutManager(
            GraphPanelView,
            ViewerPanelView,
            PropertiesPanelView);

        WorkspaceDock.Factory = workspaceLayoutManager.Factory;
        WorkspaceDock.Layout = workspaceLayoutManager.Layout;
        WorkspaceDock.IsDockingEnabled = true;
    }

    private void DetachSeedPanels()
    {
        PaneSeed.Children.Remove(GraphPanelView);
        PaneSeed.Children.Remove(ViewerPanelView);
        PaneSeed.Children.Remove(PropertiesPanelView);
    }

    private void BuildNodeToolbarStrip()
    {
        UnwireNodeToolbarButtons();
        NodeStripHost.Children.Clear();

        foreach (var nodeType in _editorSession.GetSnapshot().AvailableNodeTypes)
        {
            var button = new Button
            {
                Tag = nodeType,
                Content = GetNodeToolbarLabel(nodeType),
                Classes = { "node-strip-button" },
                Padding = new Thickness(8, 4),
                MinWidth = 34
            };
            ToolTip.SetTip(button, $"Add {nodeType}");
            button.Click += OnNodeToolbarAddClicked;
            NodeStripHost.Children.Add(button);
            _nodeStripButtons.Add(button);
        }
    }

    private void UnwireNodeToolbarButtons()
    {
        foreach (var button in _nodeStripButtons)
        {
            button.Click -= OnNodeToolbarAddClicked;
        }

        _nodeStripButtons.Clear();
    }

    private static string GetNodeToolbarLabel(string nodeType)
    {
        return NodeDisplayLabelController.GetNodeToolbarLabel(nodeType);
    }

    private async void OnExportClicked(object? sender, RoutedEventArgs e)
    {
        if (!_editorSession.TryRenderOutput(out var image, out var renderError) || image is null)
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

    private void OnUndoClicked(object? sender, RoutedEventArgs e)
    {
        _editorSession.Undo();
        RefreshGraphBindings();
        SetStatus("Undo");
    }

    private void OnRedoClicked(object? sender, RoutedEventArgs e)
    {
        _editorSession.Redo();
        RefreshGraphBindings();
        SetStatus("Redo");
    }

    private void OnNodeToolbarAddClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button ||
            button.Tag is not string nodeType)
        {
            SetStatus("Node type action unavailable.");
            return;
        }

        AddNodeOfType(nodeType);
    }

    private void OnNodeSearchAddClicked(object? sender, RoutedEventArgs e)
    {
        var query = (NodeSearchBox.Text ?? string.Empty).Trim();
        var nodeTypes = _editorSession.GetSnapshot().AvailableNodeTypes;
        var match = nodeTypes
            .FirstOrDefault(type => string.Equals(type, query, StringComparison.OrdinalIgnoreCase))
            ?? nodeTypes.FirstOrDefault(type => type.Contains(query, StringComparison.OrdinalIgnoreCase));

        if (match is null)
        {
            SetStatus("No node type matches search.");
            return;
        }

        AddNodeOfType(match);
    }

    private void OnNodeSearchBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        OnNodeSearchAddClicked(NodeSearchAddButton, new RoutedEventArgs(Button.ClickEvent));
        e.Handled = true;
    }

    private void AddNodeOfType(string nodeType)
    {
        try
        {
            var nodeId = _editorSession.AddNode(new NodeTypeId(nodeType));
            _nodePositions[nodeId] = GetViewportCenterWorld();
            RefreshGraphBindings();
            SetStatus($"Node '{nodeType}' added.");
        }
        catch (Exception exception)
        {
            SetStatus($"Add node failed: {exception.Message}");
        }
    }

    private async Task<string?> PickImagePathAsync(string title)
    {
        if (StorageProvider is null)
        {
            SetStatus("Storage provider unavailable.");
            return null;
        }

        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = title,
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
            return null;
        }

        var path = file.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            SetStatus("Selected file has no local path.");
            return null;
        }

        return path;
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
                SetStatus(_editorSession.GetSnapshot().Status);
            }
            catch (Exception exception)
            {
                SetStatus($"Preview update failed: {exception.Message}");
            }
        });
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
        var title = new TextBlock
        {
            Text = node.Type,
            FontWeight = FontWeight.SemiBold,
            Foreground = _nodeCardTitleForeground
        };
        var detail = new TextBlock
        {
            Text = node.Id.ToString()[..8],
            Foreground = _nodeCardDetailForeground
        };

        var details = new StackPanel
        {
            Spacing = 3
        };
        details.Children.Add(title);
        details.Children.Add(detail);

        var inputPorts = new StackPanel
        {
            Spacing = 4,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        foreach (var input in nodeType.Inputs)
        {
            inputPorts.Children.Add(CreatePortHandle(node.Id, input.Name, PortDirection.Input));
        }

        var outputPorts = new StackPanel
        {
            Spacing = 4,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        foreach (var output in nodeType.Outputs)
        {
            outputPorts.Children.Add(CreatePortHandle(node.Id, output.Name, PortDirection.Output));
        }

        var content = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            ColumnSpacing = 8
        };
        content.Children.Add(inputPorts);
        content.Children.Add(details);
        Grid.SetColumn(details, 1);
        content.Children.Add(outputPorts);
        Grid.SetColumn(outputPorts, 2);

        var card = new Border
        {
            Tag = node.Id,
            Width = NodeCardWidth,
            MinHeight = NodeCardHeight,
            Padding = new Thickness(8),
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

    private Border CreatePortHandle(NodeId nodeId, string portName, PortDirection direction)
    {
        var key = new PortKey(nodeId, portName, direction);
        var handle = new Border
        {
            Tag = key,
            Width = PortHandleSize,
            Height = PortHandleSize,
            CornerRadius = new CornerRadius(PortHandleSize / 2),
            BorderThickness = new Thickness(1),
            BorderBrush = _nodeCardBorder,
            Background = direction == PortDirection.Input
                ? ResolveBrush("Brush.PortHandle.Input", "#54657B")
                : ResolveBrush("Brush.PortHandle.Output", "#6F9AC4"),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        ToolTip.SetTip(handle, $"{direction}: {portName}");

        if (direction == PortDirection.Output)
        {
            handle.PointerPressed += OnOutputPortHandlePressed;
        }
        else
        {
            handle.PointerPressed += OnInputPortHandlePressed;
        }

        _portHandles[key] = handle;
        return handle;
    }

    private void OnOutputPortHandlePressed(object? sender, PointerPressedEventArgs e)
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

    private static void OnInputPortHandlePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border handle ||
            !e.GetCurrentPoint(handle).Properties.IsLeftButtonPressed)
        {
            return;
        }

        // Consume left-click on input handles so node card drag does not start from the handle.
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
            _hoverConnectionTarget = TryFindNearestInputPort(pointerScreen, out var target, out _)
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

        if (PreviewRoutingController.IsPreviewResetKey(e.Key))
        {
            _previewRouting.Reset();
            _editorSession.RequestPreviewRender();
            SetStatus("Preview reset to output.");
            e.Handled = true;
            return;
        }

        if (!PreviewRoutingController.TryMapPreviewSlot(e.Key, out var slot))
        {
            return;
        }

        if (_selectedNodeId is NodeId selectedNodeId && _nodeLookup.ContainsKey(selectedNodeId))
        {
            _previewRouting.AssignSlot(slot, selectedNodeId);
            RequestPreviewForActiveSlot();
            SetStatus($"Assigned preview slot {slot} to {_nodeLookup[selectedNodeId].Type}.");
            e.Handled = true;
            return;
        }

        if (!_previewRouting.HasSlot(slot))
        {
            SetStatus($"Preview slot {slot} is empty.");
            e.Handled = true;
            return;
        }

        _previewRouting.Activate(slot);
        RequestPreviewForActiveSlot();
        SetStatus($"Preview slot {slot} activated.");
        e.Handled = true;
    }

    private void RequestPreviewForActiveSlot()
    {
        if (_previewRouting.TryGetActiveTarget(_nodeLookup.Keys.ToArray(), out var previewNodeId))
        {
            _editorSession.RequestPreviewRender(previewNodeId);
            return;
        }

        _editorSession.RequestPreviewRender();
    }

    private void PruneSelectionAndPreviewSlots(IReadOnlyList<Node> nodes)
    {
        var liveNodeIds = nodes.Select(node => node.Id).ToHashSet();
        if (_selectedNodeId is NodeId selectedNodeId && !liveNodeIds.Contains(selectedNodeId))
        {
            _selectedNodeId = null;
        }

        _nodeActionController.PruneUnavailableNodes(liveNodeIds);

        if (_previewRouting.Prune(liveNodeIds))
        {
            _editorSession.RequestPreviewRender();
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
        RefreshPropertiesEditor();
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
            _activeConnectionSource is PortKey source &&
            TryGetPortAnchor(source, out var dragSourceAnchor))
        {
            var dragTargetAnchor = _hoverConnectionTarget is PortKey hoverKey &&
                                   TryGetPortAnchor(hoverKey, out var hoverAnchor)
                ? hoverAnchor
                : _activeConnectionPointerWorld;

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
        var delta = new Vector(targetAnchor.X - sourceAnchor.X, targetAnchor.Y - sourceAnchor.Y);
        var isHorizontalDominant = Math.Abs(delta.X) >= Math.Abs(delta.Y);
        var (bendA, bendB) = GraphInteractionController.ResolveOrthogonalBends(sourceAnchor, targetAnchor, isHorizontalDominant);

        var wire = new Polyline
        {
            IsHitTestVisible = false,
            Stroke = _wireStroke,
            StrokeThickness = WireThickness,
            Points = new[] { sourceAnchor, bendA, bendB, targetAnchor }
        };
        var arrow = (Polygon)CreateArrowHead(bendB, targetAnchor);
        return (wire, arrow);
    }

    private bool TryCommitDraggedConnection(Point pointerScreen)
    {
        if (_activeConnectionSource is not PortKey source)
        {
            return false;
        }

        if (!TryFindNearestInputPort(pointerScreen, out var target, out _))
        {
            SetStatus("Connection canceled.");
            return false;
        }

        try
        {
            _editorSession.Connect(source.NodeId, source.PortName, target.NodeId, target.PortName);
            RefreshGraphBindings();
            SetStatus($"Connected {source.PortName} -> {target.PortName}");
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

    private bool TryFindNearestInputPort(Point pointerScreen, out PortKey targetPort, out Point targetAnchor)
    {
        targetPort = default;
        targetAnchor = default;
        var found = false;
        var nearestDistance = double.MaxValue;
        foreach (var node in _nodeLookup.Values)
        {
            var nodeType = _editorSession.GetNodeTypeDefinition(node.Type);
            foreach (var input in nodeType.Inputs)
            {
                var candidate = new PortKey(node.Id, input.Name, PortDirection.Input);
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

    private bool TryGetPortAnchor(PortKey key, out Point anchor)
    {
        anchor = default;

        if (!_nodeLookup.TryGetValue(key.NodeId, out var node) ||
            !_nodePositions.TryGetValue(key.NodeId, out var nodePosition))
        {
            return false;
        }

        var nodeType = _editorSession.GetNodeTypeDefinition(node.Type);
        var ports = key.Direction == PortDirection.Input
            ? nodeType.Inputs
            : nodeType.Outputs;
        var portIndex = ports
            .Select((port, index) => new { port.Name, Index = index })
            .Where(item => string.Equals(item.Name, key.PortName, StringComparison.Ordinal))
            .Select(item => item.Index)
            .FirstOrDefault(-1);
        if (portIndex < 0)
        {
            return false;
        }

        var cardHeight = GetCardHeight(key.NodeId);
        var cardWidth = GetCardWidth(key.NodeId);
        var totalSpan = (ports.Count - 1) * PortHandleVerticalSpacing;
        var startY = nodePosition.Y + ((cardHeight - totalSpan) / 2);
        var y = startY + (portIndex * PortHandleVerticalSpacing);
        var x = key.Direction == PortDirection.Input
            ? nodePosition.X
            : nodePosition.X + cardWidth;

        anchor = new Point(x, y);
        return true;
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

    private void RefreshPropertiesEditor()
    {
        _propertiesPanelController.Refresh(
            PropertyEditorHost,
            SelectedNodeText,
            _selectedNodeId,
            _nodeLookup);
    }

    private IBrush ResolveBrush(string resourceKey, string fallbackHex)
    {
        if (TryGetResource(resourceKey, ActualThemeVariant, out var resource) &&
            resource is IBrush brush)
        {
            return brush;
        }

        return Brush.Parse(fallbackHex);
    }

    private void SetStatus(string message)
    {
        StatusTextBlock.Text = message;
    }

    private readonly record struct PortKey(NodeId NodeId, string PortName, PortDirection Direction);
}
