using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using App.Presentation.Controllers;
using Editor.Domain.Graph;
using Editor.Engine.Abstractions;

namespace App;

public partial class MainWindow
{
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
        if (IsAutomationModeEnabled)
        {
            SetStatus("Export is disabled in automation mode.");
            return;
        }

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
        if (IsAutomationModeEnabled)
        {
            SetStatus("Image picker is disabled in automation mode.");
            return null;
        }

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
}
