using Editor.Application;
using Editor.Domain.Graph;
using Editor.IO;

namespace App.Presentation.Controllers;

public sealed class NodeActionController
{
    private readonly IEditorSession _editorSession;
    private readonly IImageLoader _imageLoader;
    private readonly Func<string, Task<string?>> _pickImagePathAsync;
    private readonly Action _requestPreviewForActiveSlot;
    private readonly Action _refreshPropertiesEditor;
    private readonly Action<string> _setStatus;
    private readonly Action _notifyMutation;
    private readonly Dictionary<string, Func<NodeId, Task>> _handlers;
    private readonly Dictionary<NodeActionDisplayKey, string> _displayValues = new();

    public NodeActionController(
        IEditorSession editorSession,
        IImageLoader imageLoader,
        Func<string, Task<string?>> pickImagePathAsync,
        Action requestPreviewForActiveSlot,
        Action refreshPropertiesEditor,
        Action<string> setStatus,
        Action? notifyMutation = null)
    {
        _editorSession = editorSession;
        _imageLoader = imageLoader;
        _pickImagePathAsync = pickImagePathAsync;
        _requestPreviewForActiveSlot = requestPreviewForActiveSlot;
        _refreshPropertiesEditor = refreshPropertiesEditor;
        _setStatus = setStatus;
        _notifyMutation = notifyMutation ?? (() => { });
        _handlers = new Dictionary<string, Func<NodeId, Task>>(StringComparer.Ordinal)
        {
            [NodeActionIds.PickImageSource] = LoadImageIntoNodeAsync
        };
    }

    public Task ExecuteAsync(NodeId nodeId, string actionId)
    {
        if (_handlers.TryGetValue(actionId, out var handler))
        {
            return handler(nodeId);
        }

        throw new InvalidOperationException($"Unknown node action '{actionId}'.");
    }

    public string? ResolveDisplayText(NodeId nodeId, string actionId)
    {
        return _displayValues.TryGetValue(new NodeActionDisplayKey(nodeId, actionId), out var displayValue)
            ? displayValue
            : null;
    }

    public IReadOnlyDictionary<NodeId, string> CaptureImageSourceBindings()
    {
        return _displayValues
            .Where(entry => string.Equals(entry.Key.ActionId, NodeActionIds.PickImageSource, StringComparison.Ordinal))
            .ToDictionary(entry => entry.Key.NodeId, entry => entry.Value);
    }

    public void RestoreImageSourceBindings(IReadOnlyDictionary<NodeId, string> bindings)
    {
        var staleKeys = _displayValues.Keys
            .Where(key => string.Equals(key.ActionId, NodeActionIds.PickImageSource, StringComparison.Ordinal))
            .ToArray();
        foreach (var staleKey in staleKeys)
        {
            _displayValues.Remove(staleKey);
        }

        foreach (var (nodeId, path) in bindings)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            _displayValues[new NodeActionDisplayKey(nodeId, NodeActionIds.PickImageSource)] = path;
        }
    }

    public bool TryLoadImageSourceBinding(NodeId nodeId, string path, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            errorMessage = "Image path is required.";
            return false;
        }

        if (!_imageLoader.TryLoad(path, out var image, out var loadError) || image is null)
        {
            errorMessage = loadError;
            return false;
        }

        try
        {
            _editorSession.SetInputImage(nodeId, image);
            _displayValues[new NodeActionDisplayKey(nodeId, NodeActionIds.PickImageSource)] = path;
            _notifyMutation();
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }

    public void PruneUnavailableNodes(IReadOnlySet<NodeId> liveNodeIds)
    {
        var staleKeys = _displayValues.Keys
            .Where(key => !liveNodeIds.Contains(key.NodeId))
            .ToArray();
        foreach (var staleKey in staleKeys)
        {
            _displayValues.Remove(staleKey);
        }
    }

    private async Task LoadImageIntoNodeAsync(NodeId nodeId)
    {
        var path = await _pickImagePathAsync("Choose Source Image");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!TryLoadImageSourceBinding(nodeId, path, out var errorMessage))
        {
            _setStatus($"Load failed: {errorMessage}");
            return;
        }

        _requestPreviewForActiveSlot();
        _setStatus($"Loaded {Path.GetFileName(path)} into {nodeId}.");
        _refreshPropertiesEditor();
    }

    private readonly record struct NodeActionDisplayKey(NodeId NodeId, string ActionId);
}
