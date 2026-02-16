using App.Workspace;

namespace App;

public partial class MainWindow
{
    protected override void OnClosed(EventArgs e)
    {
        _editorSession.PreviewUpdated -= OnPreviewUpdated;
        Closing -= OnWindowClosing;
        NewProjectButton.Click -= OnNewProjectClicked;
        OpenProjectButton.Click -= OnOpenProjectClicked;
        SaveProjectButton.Click -= OnSaveProjectClicked;
        SaveProjectAsButton.Click -= OnSaveProjectAsClicked;
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
        Closing += OnWindowClosing;

        NewProjectButton.Click += OnNewProjectClicked;
        OpenProjectButton.Click += OnOpenProjectClicked;
        SaveProjectButton.Click += OnSaveProjectClicked;
        SaveProjectAsButton.Click += OnSaveProjectAsClicked;
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
        InitializeProjectDocumentState();
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
}
