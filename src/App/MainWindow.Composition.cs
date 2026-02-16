using Avalonia.Controls;
using App.Presentation.Controllers;
using App.Views;
using Editor.Application;
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
}
