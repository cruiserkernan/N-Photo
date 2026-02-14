using System.Globalization;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine;
using Editor.IO;

namespace App;

public partial class MainWindow : Window
{
    private readonly IEditorEngine _editorEngine;
    private readonly IImageLoader _imageLoader;
    private readonly IImageExporter _imageExporter;
    private readonly Dictionary<NodeId, Node> _nodeLookup = new();
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
        _previewBitmap?.Dispose();
        base.OnClosed(e);
    }

    private void WireEvents()
    {
        _editorEngine.PreviewUpdated += OnPreviewUpdated;
    }

    private void InitializeUiState()
    {
        NodeTypeComboBox.ItemsSource = _editorEngine.AvailableNodeTypes;
        NodeTypeComboBox.SelectedIndex = 0;
        RefreshGraphBindings();
        SetStatus("Ready");
        _editorEngine.RequestPreviewRender();
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
            _editorEngine.AddNode(nodeType);
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

        _nodeLookup.Clear();
        foreach (var node in nodes)
        {
            _nodeLookup[node.Id] = node;
        }

        var nodeOptions = nodes
            .Select(node => new NodeOption(node.Id, node.Type))
            .ToArray();

        NodeListBox.ItemsSource = nodeOptions.Select(option => option.ToString()).ToArray();
        EdgeListBox.ItemsSource = edges.Select(DescribeEdge).ToArray();

        SyncComboSelection(FromNodeComboBox, nodeOptions);
        SyncComboSelection(ToNodeComboBox, nodeOptions);
        SyncComboSelection(ParameterNodeComboBox, nodeOptions);

        RefreshFromPorts();
        RefreshToPorts();
        RefreshParameterNames();

        UndoButton.IsEnabled = _editorEngine.CanUndo;
        RedoButton.IsEnabled = _editorEngine.CanRedo;
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
