using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using App.Presentation.Controllers;
using App.Views;
using App.Workspace;
using Editor.Application;
using Editor.Domain.Graph;
using Editor.Engine;
using Editor.Engine.Abstractions;
using Editor.IO;

namespace App;

public partial class MainWindow : Window
{
    private readonly IEditorSession _editorSession;
    private readonly IImageLoader _imageLoader;
    private readonly IImageExporter _imageExporter;
    private readonly NodeActionController _nodeActionController;
    private readonly PropertiesPanelController _propertiesPanelController;
    private readonly List<Button> _nodeStripButtons = new();
    private readonly PreviewRoutingController _previewRouting = new();

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
        InitializeGraphVisualResources();
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

        ViewerCanvas.PointerPressed -= OnViewerCanvasPointerPressed;
        ViewerCanvas.SizeChanged -= OnViewerCanvasSizeChanged;
        ViewerCanvas.PointerMoved -= OnViewerCanvasPointerMoved;
        ViewerCanvas.PointerReleased -= OnViewerCanvasPointerReleased;
        ViewerCanvas.PointerCaptureLost -= OnViewerCanvasPointerCaptureLost;
        ViewerCanvas.PointerWheelChanged -= OnViewerCanvasPointerWheelChanged;

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

        ViewerCanvas.PointerPressed += OnViewerCanvasPointerPressed;
        ViewerCanvas.SizeChanged += OnViewerCanvasSizeChanged;
        ViewerCanvas.PointerMoved += OnViewerCanvasPointerMoved;
        ViewerCanvas.PointerReleased += OnViewerCanvasPointerReleased;
        ViewerCanvas.PointerCaptureLost += OnViewerCanvasPointerCaptureLost;
        ViewerCanvas.PointerWheelChanged += OnViewerCanvasPointerWheelChanged;

        KeyDown += OnWindowKeyDown;
    }

    private void InitializeUiState()
    {
        InitializeWorkspaceUi();
        EnsureGraphLayer();
        InitializeViewerViewport();
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
                FileTypeChoices =
                [
                    new FilePickerFileType("PNG Image") { Patterns = ["*.png"] },
                    new FilePickerFileType("JPEG Image") { Patterns = ["*.jpg", "*.jpeg"] }
                ]
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
                FileTypeFilter =
                [
                    new FilePickerFileType("Images")
                    {
                        Patterns = ["*.png", "*.jpg", "*.jpeg"]
                    }
                ]
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

    private InputElement? GetFocusedElement()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        return topLevel?.FocusManager?.GetFocusedElement() as InputElement;
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
}
