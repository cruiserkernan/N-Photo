using Avalonia.Interactivity;
using Avalonia.Controls;
using Editor.Domain.Graph;
using Editor.Engine;
using Editor.IO;

namespace App;

public partial class MainWindow : Window
{
    private readonly IEditorEngine _editorEngine;
    private readonly IImageLoader _imageLoader;
    private readonly IImageExporter _imageExporter;

    public MainWindow()
        : this(new BootstrapEditorEngine(), new StubImageLoader(), new StubImageExporter())
    {
    }

    public MainWindow(IEditorEngine editorEngine, IImageLoader imageLoader, IImageExporter imageExporter)
    {
        _editorEngine = editorEngine;
        _imageLoader = imageLoader;
        _imageExporter = imageExporter;

        SeedBootstrapGraph();
        InitializeComponent();
    }

    private void OnLoadClicked(object? sender, RoutedEventArgs e)
    {
        var loaded = _imageLoader.TryLoad("bootstrap-input-placeholder.png");
        _editorEngine.RequestPreviewRender();
        StatusTextBlock.Text = loaded
            ? "Load stub invoked"
            : "Load stub failed";
    }

    private void OnExportClicked(object? sender, RoutedEventArgs e)
    {
        var exported = _imageExporter.TryExport("bootstrap-output-placeholder.png");
        StatusTextBlock.Text = exported
            ? "Export stub invoked"
            : "Export stub failed";
    }

    private static void SeedBootstrapGraph()
    {
        var validator = new DagValidator();
        var graph = new NodeGraph();
        var imageInput = new Node(NodeId.New(), "ImageInput");
        var output = new Node(NodeId.New(), "Output");

        graph.AddNode(imageInput);
        graph.AddNode(output);
        graph.AddEdge(new Edge(imageInput.Id, "Image", output.Id, "Image"), validator);
    }
}
