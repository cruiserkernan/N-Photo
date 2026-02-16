using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using App.Presentation.Controllers;
using Editor.Domain.Graph;
using Editor.IO;

namespace App;

public partial class MainWindow
{
    private const string AppTitle = "N-Photo";
    private const string UntitledProjectName = "Untitled";

    private enum UnsavedChangesDecision
    {
        Save,
        Discard,
        Cancel
    }

    private void InitializeProjectDocumentState()
    {
        _currentProjectPath = null;
        SetCleanProjectState(_currentProjectPath);
    }

    private void OnPersistentStateMutated()
    {
        UpdateDocumentDirtyState();
    }

    private void UpdateDocumentDirtyState()
    {
        if (_suppressDirtyTracking)
        {
            return;
        }

        try
        {
            var currentSignature = _projectDocumentStore.CreateCanonicalSignature(
                CaptureProjectDocument(_currentProjectPath));
            _isDocumentDirty = !string.Equals(currentSignature, _savedProjectSignature, StringComparison.Ordinal);
        }
        catch
        {
            _isDocumentDirty = true;
        }

        UpdateWindowTitle();
    }

    private void SetCleanProjectState(string? projectPath)
    {
        _currentProjectPath = projectPath;
        _savedProjectSignature = _projectDocumentStore.CreateCanonicalSignature(
            CaptureProjectDocument(projectPath));
        _isDocumentDirty = false;
        UpdateWindowTitle();
    }

    private void UpdateWindowTitle()
    {
        var projectName = string.IsNullOrWhiteSpace(_currentProjectPath)
            ? UntitledProjectName
            : Path.GetFileName(_currentProjectPath);
        var dirtySuffix = _isDocumentDirty ? "*" : string.Empty;
        Title = $"{AppTitle} - {projectName}{dirtySuffix}";
    }

    private bool TryHandleProjectShortcut(KeyEventArgs e)
    {
        var hasPrimaryModifier = (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0;
        var hasAlt = (e.KeyModifiers & KeyModifiers.Alt) != 0;
        if (!hasPrimaryModifier || hasAlt)
        {
            return false;
        }

        if (e.Key == Key.S && (e.KeyModifiers & KeyModifiers.Shift) != 0)
        {
            OnSaveProjectAsClicked(this, new RoutedEventArgs(Button.ClickEvent));
            e.Handled = true;
            return true;
        }

        switch (e.Key)
        {
            case Key.N:
                OnNewProjectClicked(this, new RoutedEventArgs(Button.ClickEvent));
                e.Handled = true;
                return true;
            case Key.O:
                OnOpenProjectClicked(this, new RoutedEventArgs(Button.ClickEvent));
                e.Handled = true;
                return true;
            case Key.S:
                OnSaveProjectClicked(this, new RoutedEventArgs(Button.ClickEvent));
                e.Handled = true;
                return true;
            default:
                return false;
        }
    }

    private async void OnNewProjectClicked(object? sender, RoutedEventArgs e)
    {
        if (!await ConfirmCanDiscardUnsavedChangesAsync("start a new project"))
        {
            return;
        }

        try
        {
            _suppressDirtyTracking = true;
            _editorSession.LoadGraphDocument(CreateDefaultProjectGraphDocument());
            _nodePositions.Clear();
            _selectedNodeId = null;
            _previewRouting.RestoreState(new PreviewRoutingState(new Dictionary<int, NodeId>(), null));
            _nodeActionController.RestoreImageSourceBindings(new Dictionary<NodeId, string>());
            RefreshGraphBindings();
            SetCleanProjectState(projectPath: null);
            SetStatus("New project created.");
        }
        catch (Exception exception)
        {
            SetStatus($"New project failed: {exception.Message}");
        }
        finally
        {
            _suppressDirtyTracking = false;
            UpdateDocumentDirtyState();
        }
    }

    private async void OnOpenProjectClicked(object? sender, RoutedEventArgs e)
    {
        if (!await ConfirmCanDiscardUnsavedChangesAsync("open another project"))
        {
            return;
        }

        var path = await PickProjectOpenPathAsync();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await OpenProjectFromPathAsync(path);
    }

    private async void OnSaveProjectClicked(object? sender, RoutedEventArgs e)
    {
        await SaveProjectAsync(forceSaveAs: false);
    }

    private async void OnSaveProjectAsClicked(object? sender, RoutedEventArgs e)
    {
        await SaveProjectAsync(forceSaveAs: true);
    }

    private async Task<bool> OpenProjectFromPathAsync(string path)
    {
        if (!_projectDocumentStore.TryLoad(path, out var document, out var errorMessage) || document is null)
        {
            SetStatus($"Open failed: {errorMessage}");
            return false;
        }

        try
        {
            _suppressDirtyTracking = true;
            var graphDocument = ToGraphDocumentState(document.Graph);
            _editorSession.LoadGraphDocument(graphDocument);

            _nodePositions.Clear();
            foreach (var position in document.Ui.NodePositions)
            {
                _nodePositions[new NodeId(position.NodeId)] = new Point(position.X, position.Y);
            }

            _selectedNodeId = document.Ui.SelectedNodeId.HasValue
                ? new NodeId(document.Ui.SelectedNodeId.Value)
                : null;

            var previewSlots = document.Ui.PreviewSlots
                .ToDictionary(slot => slot.Slot, slot => new NodeId(slot.NodeId));
            _previewRouting.RestoreState(new PreviewRoutingState(previewSlots, document.Ui.ActivePreviewSlot));

            var persistedBindings = document.Assets.ImageInputs.ToDictionary(
                binding => new NodeId(binding.NodeId),
                binding => binding.Path);
            _nodeActionController.RestoreImageSourceBindings(persistedBindings);

            var warnings = new List<string>();
            foreach (var binding in document.Assets.ImageInputs)
            {
                var nodeId = new NodeId(binding.NodeId);
                var resolvedPath = ResolveAssetPath(binding.Path, path);
                if (_nodeActionController.TryLoadImageSourceBinding(nodeId, resolvedPath, out var loadError))
                {
                    continue;
                }

                warnings.Add($"Node {nodeId}: {loadError}");
            }

            RefreshGraphBindings();
            SetCleanProjectState(path);
            if (warnings.Count == 0)
            {
                SetStatus($"Opened project: {Path.GetFileName(path)}");
            }
            else
            {
                SetStatus($"Opened project with {warnings.Count} warning(s).");
            }

            return true;
        }
        catch (Exception exception)
        {
            SetStatus($"Open failed: {exception.Message}");
            return false;
        }
        finally
        {
            _suppressDirtyTracking = false;
            UpdateDocumentDirtyState();
        }
    }

    private async Task<bool> SaveProjectAsync(bool forceSaveAs)
    {
        var savePath = _currentProjectPath;
        if (forceSaveAs || string.IsNullOrWhiteSpace(savePath))
        {
            savePath = await PickProjectSavePathAsync();
        }

        if (string.IsNullOrWhiteSpace(savePath))
        {
            return false;
        }

        try
        {
            var document = CaptureProjectDocument(savePath);
            if (!_projectDocumentStore.TrySave(document, savePath, out var errorMessage))
            {
                SetStatus($"Save failed: {errorMessage}");
                return false;
            }

            var persistedBindings = document.Assets.ImageInputs.ToDictionary(
                binding => new NodeId(binding.NodeId),
                binding => binding.Path);
            _nodeActionController.RestoreImageSourceBindings(persistedBindings);
            SetCleanProjectState(savePath);
            SetStatus($"Saved project: {Path.GetFileName(savePath)}");
            return true;
        }
        catch (Exception exception)
        {
            SetStatus($"Save failed: {exception.Message}");
            return false;
        }
    }

    private ProjectDocument CaptureProjectDocument(string? targetProjectPath)
    {
        var graphDocument = _editorSession.CaptureGraphDocument();
        var liveNodeIds = graphDocument.Nodes.Select(node => node.NodeId).ToHashSet();

        var projectNodes = graphDocument.Nodes
            .OrderBy(node => node.NodeId.Value)
            .Select(node => new ProjectNode
            {
                Id = node.NodeId.Value,
                Type = node.NodeType,
                Parameters = node.Parameters
                    .OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
                    .Select(parameter => new ProjectParameter
                    {
                        Name = parameter.Key,
                        Value = ProjectParameterValueCodec.FromDomain(parameter.Value)
                    })
                    .ToArray()
            })
            .ToArray();

        var projectEdges = graphDocument.Edges
            .OrderBy(edge => edge.FromNodeId.Value)
            .ThenBy(edge => edge.FromPort, StringComparer.Ordinal)
            .ThenBy(edge => edge.ToNodeId.Value)
            .ThenBy(edge => edge.ToPort, StringComparer.Ordinal)
            .Select(edge => new ProjectEdge
            {
                FromNodeId = edge.FromNodeId.Value,
                FromPort = edge.FromPort,
                ToNodeId = edge.ToNodeId.Value,
                ToPort = edge.ToPort
            })
            .ToArray();

        var nodePositions = _nodePositions
            .Where(position => liveNodeIds.Contains(position.Key))
            .OrderBy(position => position.Key.Value)
            .Select(position => new ProjectNodePosition
            {
                NodeId = position.Key.Value,
                X = position.Value.X,
                Y = position.Value.Y
            })
            .ToArray();

        Guid? selectedNodeId = _selectedNodeId is NodeId selectedNode && liveNodeIds.Contains(selectedNode)
            ? selectedNode.Value
            : null;

        var previewState = _previewRouting.CaptureState();
        var previewSlots = previewState.Slots
            .Where(slot => slot.Key > 0 && liveNodeIds.Contains(slot.Value))
            .OrderBy(slot => slot.Key)
            .Select(slot => new ProjectPreviewSlotBinding
            {
                Slot = slot.Key,
                NodeId = slot.Value.Value
            })
            .ToArray();
        int? activePreviewSlot = previewState.ActiveSlot is int activeSlot &&
                                 previewSlots.Any(slot => slot.Slot == activeSlot)
            ? activeSlot
            : null;

        var imageBindings = _nodeActionController.CaptureImageSourceBindings()
            .Where(binding => liveNodeIds.Contains(binding.Key) && !string.IsNullOrWhiteSpace(binding.Value))
            .OrderBy(binding => binding.Key.Value)
            .Select(binding => new ProjectImageBinding
            {
                NodeId = binding.Key.Value,
                Path = NormalizeAssetPathForSave(binding.Value, _currentProjectPath, targetProjectPath)
            })
            .ToArray();

        return new ProjectDocument
        {
            FormatVersion = ProjectDocument.CurrentFormatVersion,
            Graph = new ProjectGraph
            {
                InputNodeId = graphDocument.InputNodeId.Value,
                OutputNodeId = graphDocument.OutputNodeId.Value,
                Nodes = projectNodes,
                Edges = projectEdges
            },
            Ui = new ProjectUiState
            {
                NodePositions = nodePositions,
                SelectedNodeId = selectedNodeId,
                PreviewSlots = previewSlots,
                ActivePreviewSlot = activePreviewSlot
            },
            Assets = new ProjectAssets
            {
                ImageInputs = imageBindings
            }
        };
    }

    private static GraphDocumentState ToGraphDocumentState(ProjectGraph graph)
    {
        var nodes = graph.Nodes
            .OrderBy(node => node.Id)
            .Select(node =>
            {
                var parameters = new Dictionary<string, ParameterValue>(StringComparer.Ordinal);
                foreach (var parameter in node.Parameters)
                {
                    if (!ProjectParameterValueCodec.TryToDomain(parameter.Value, out var domainValue, out var error))
                    {
                        throw new InvalidOperationException(
                            $"Node '{node.Id}' parameter '{parameter.Name}' is invalid: {error}");
                    }

                    parameters[parameter.Name] = domainValue;
                }

                return new GraphNodeState(new NodeId(node.Id), node.Type, parameters);
            })
            .ToArray();

        var edges = graph.Edges
            .OrderBy(edge => edge.FromNodeId)
            .ThenBy(edge => edge.FromPort, StringComparer.Ordinal)
            .ThenBy(edge => edge.ToNodeId)
            .ThenBy(edge => edge.ToPort, StringComparer.Ordinal)
            .Select(edge => new Edge(
                new NodeId(edge.FromNodeId),
                edge.FromPort,
                new NodeId(edge.ToNodeId),
                edge.ToPort))
            .ToArray();

        return new GraphDocumentState(
            new NodeId(graph.InputNodeId),
            new NodeId(graph.OutputNodeId),
            nodes,
            edges);
    }

    private static GraphDocumentState CreateDefaultProjectGraphDocument()
    {
        var inputNodeId = NodeId.New();
        var outputNodeId = NodeId.New();
        var nodes = new[]
        {
            new GraphNodeState(
                inputNodeId,
                NodeTypes.ImageInput,
                new Dictionary<string, ParameterValue>(StringComparer.Ordinal)),
            new GraphNodeState(
                outputNodeId,
                NodeTypes.Output,
                new Dictionary<string, ParameterValue>(StringComparer.Ordinal))
        };

        var edges = new[]
        {
            new Edge(inputNodeId, NodePortNames.Image, outputNodeId, NodePortNames.Image)
        };

        return new GraphDocumentState(inputNodeId, outputNodeId, nodes, edges);
    }

    private static string NormalizeAssetPathForSave(string rawPath, string? sourceProjectPath, string? targetProjectPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return rawPath;
        }

        var absolutePath = TryResolveAbsolutePath(rawPath, sourceProjectPath);
        if (absolutePath is null)
        {
            return rawPath;
        }

        return ToStoredAssetPath(absolutePath, targetProjectPath);
    }

    private static string ResolveAssetPath(string storedPath, string projectPath)
    {
        if (Path.IsPathRooted(storedPath))
        {
            return storedPath;
        }

        var projectDirectory = Path.GetDirectoryName(projectPath);
        if (string.IsNullOrWhiteSpace(projectDirectory))
        {
            return storedPath;
        }

        return Path.GetFullPath(Path.Combine(projectDirectory, storedPath));
    }

    private static string? TryResolveAbsolutePath(string path, string? projectPath)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return null;
        }

        var projectDirectory = Path.GetDirectoryName(projectPath);
        if (string.IsNullOrWhiteSpace(projectDirectory))
        {
            return null;
        }

        return Path.GetFullPath(Path.Combine(projectDirectory, path));
    }

    private static string ToStoredAssetPath(string absolutePath, string? projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return absolutePath;
        }

        var projectDirectory = Path.GetDirectoryName(projectPath);
        if (string.IsNullOrWhiteSpace(projectDirectory))
        {
            return absolutePath;
        }

        var relativePath = Path.GetRelativePath(projectDirectory, absolutePath);
        if (Path.IsPathRooted(relativePath))
        {
            return absolutePath;
        }

        if (relativePath.Equals("..", StringComparison.Ordinal) ||
            relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
            relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
        {
            return absolutePath;
        }

        return relativePath;
    }

    private async Task<string?> PickProjectOpenPathAsync()
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
                Title = "Open Project",
                FileTypeFilter =
                [
                    new FilePickerFileType("N-Photo Project")
                    {
                        Patterns = ["*.nphoto"]
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
            SetStatus("Selected project has no local path.");
            return null;
        }

        return path;
    }

    private async Task<string?> PickProjectSavePathAsync()
    {
        if (StorageProvider is null)
        {
            SetStatus("Storage provider unavailable.");
            return null;
        }

        var suggestedName = string.IsNullOrWhiteSpace(_currentProjectPath)
            ? "project.nphoto"
            : Path.GetFileName(_currentProjectPath);

        var file = await StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title = "Save Project",
                SuggestedFileName = suggestedName,
                DefaultExtension = "nphoto",
                FileTypeChoices =
                [
                    new FilePickerFileType("N-Photo Project")
                    {
                        Patterns = ["*.nphoto"]
                    }
                ]
            });

        if (file is null)
        {
            return null;
        }

        var path = file.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path))
        {
            SetStatus("Selected project location has no local path.");
            return null;
        }

        return path;
    }

    private async Task<bool> ConfirmCanDiscardUnsavedChangesAsync(string action)
    {
        if (!_isDocumentDirty)
        {
            return true;
        }

        var decision = await ShowUnsavedChangesDialogAsync(action);
        return decision switch
        {
            UnsavedChangesDecision.Save => await SaveProjectAsync(forceSaveAs: false),
            UnsavedChangesDecision.Discard => true,
            _ => false
        };
    }

    private async Task<UnsavedChangesDecision> ShowUnsavedChangesDialogAsync(string action)
    {
        var dialog = new Window
        {
            Title = "Unsaved Changes",
            Width = 420,
            Height = 180,
            CanResize = false,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var decision = UnsavedChangesDecision.Cancel;
        void Resolve(UnsavedChangesDecision nextDecision)
        {
            decision = nextDecision;
            dialog.Close();
        }

        var saveButton = new Button
        {
            Content = "Save",
            MinWidth = 90,
            Classes = { "toolbar-button", "toolbar-button-primary" }
        };
        saveButton.Click += (_, _) => Resolve(UnsavedChangesDecision.Save);

        var discardButton = new Button
        {
            Content = "Discard",
            MinWidth = 90,
            Classes = { "toolbar-button" }
        };
        discardButton.Click += (_, _) => Resolve(UnsavedChangesDecision.Discard);

        var cancelButton = new Button
        {
            Content = "Cancel",
            MinWidth = 90,
            Classes = { "toolbar-button" }
        };
        cancelButton.Click += (_, _) => Resolve(UnsavedChangesDecision.Cancel);

        dialog.Content = new Border
        {
            Padding = new Thickness(16),
            Child = new StackPanel
            {
                Spacing = 14,
                Children =
                {
                    new TextBlock
                    {
                        Text = $"You have unsaved changes. Save before you {action}?",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 8,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Children =
                        {
                            saveButton,
                            discardButton,
                            cancelButton
                        }
                    }
                }
            }
        };

        await dialog.ShowDialog(this);
        return decision;
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_suppressClosePromptOnce)
        {
            _suppressClosePromptOnce = false;
            return;
        }

        if (_isPromptingUnsavedChanges || !_isDocumentDirty)
        {
            return;
        }

        e.Cancel = true;
        _isPromptingUnsavedChanges = true;
        try
        {
            if (!await ConfirmCanDiscardUnsavedChangesAsync("close the window"))
            {
                return;
            }

            _suppressClosePromptOnce = true;
            Close();
        }
        finally
        {
            _isPromptingUnsavedChanges = false;
        }
    }
}
