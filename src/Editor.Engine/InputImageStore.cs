using Editor.Domain.Graph;
using Editor.Domain.Imaging;

namespace Editor.Engine;

internal sealed class InputImageStore
{
    private readonly Dictionary<NodeId, RgbaImage> _images = new();

    public void Set(NodeId nodeId, RgbaImage image)
    {
        _images[nodeId] = image.Clone();
    }

    public bool TryGet(NodeId nodeId, out RgbaImage image)
    {
        if (_images.TryGetValue(nodeId, out var stored))
        {
            image = stored.Clone();
            return true;
        }

        image = null!;
        return false;
    }

    public IReadOnlyDictionary<NodeId, RgbaImage> Snapshot()
    {
        return _images.ToDictionary(pair => pair.Key, pair => pair.Value.Clone());
    }

    public void PruneTo(NodeGraph graph)
    {
        var liveNodeIds = graph.Nodes.Select(node => node.Id).ToHashSet();
        var staleKeys = _images.Keys.Where(key => !liveNodeIds.Contains(key)).ToArray();
        foreach (var key in staleKeys)
        {
            _images.Remove(key);
        }
    }
}
