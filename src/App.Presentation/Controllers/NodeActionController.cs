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
    private readonly Dictionary<string, Func<NodeId, Task>> _handlers;
    private readonly Dictionary<NodeActionDisplayKey, string> _displayValues = new();

    public NodeActionController(
        IEditorSession editorSession,
        IImageLoader imageLoader,
        Func<string, Task<string?>> pickImagePathAsync,
        Action requestPreviewForActiveSlot,
        Action refreshPropertiesEditor,
        Action<string> setStatus)
    {
        _editorSession = editorSession;
        _imageLoader = imageLoader;
        _pickImagePathAsync = pickImagePathAsync;
        _requestPreviewForActiveSlot = requestPreviewForActiveSlot;
        _refreshPropertiesEditor = refreshPropertiesEditor;
        _setStatus = setStatus;
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

        if (!_imageLoader.TryLoad(path, out var image, out var error) || image is null)
        {
            _setStatus($"Load failed: {error}");
            return;
        }

        try
        {
            _editorSession.SetInputImage(nodeId, image);
            _displayValues[new NodeActionDisplayKey(nodeId, NodeActionIds.PickImageSource)] = path;
            _requestPreviewForActiveSlot();
            _setStatus($"Loaded {Path.GetFileName(path)} into {nodeId}.");
            _refreshPropertiesEditor();
        }
        catch (Exception exception)
        {
            _setStatus($"Load failed: {exception.Message}");
        }
    }

    private readonly record struct NodeActionDisplayKey(NodeId NodeId, string ActionId);
}
