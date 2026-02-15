using Avalonia.Input;
using Editor.Domain.Graph;

namespace App.Presentation.Controllers;

public sealed class PreviewRoutingController
{
    private readonly Dictionary<int, NodeId> _previewSlots = new();

    public int? ActiveSlot { get; private set; }

    public IReadOnlyDictionary<int, NodeId> PreviewSlots => _previewSlots;

    public void AssignSlot(int slot, NodeId nodeId)
    {
        _previewSlots[slot] = nodeId;
        ActiveSlot = slot;
    }

    public bool TryGetActiveTarget(IReadOnlyCollection<NodeId> liveNodeIds, out NodeId targetNodeId)
    {
        targetNodeId = default;

        if (ActiveSlot is not int activeSlot ||
            !_previewSlots.TryGetValue(activeSlot, out var mappedNodeId) ||
            !liveNodeIds.Contains(mappedNodeId))
        {
            return false;
        }

        targetNodeId = mappedNodeId;
        return true;
    }

    public void Activate(int slot)
    {
        ActiveSlot = slot;
    }

    public bool HasSlot(int slot)
    {
        return _previewSlots.ContainsKey(slot);
    }

    public void Reset()
    {
        ActiveSlot = null;
    }

    public bool Prune(IReadOnlyCollection<NodeId> liveNodeIds)
    {
        var removedSlots = _previewSlots
            .Where(slot => !liveNodeIds.Contains(slot.Value))
            .Select(slot => slot.Key)
            .ToArray();

        foreach (var slot in removedSlots)
        {
            _previewSlots.Remove(slot);
        }

        if (ActiveSlot is int active && !_previewSlots.ContainsKey(active))
        {
            ActiveSlot = null;
            return true;
        }

        return false;
    }

    public static bool TryMapPreviewSlot(Key key, out int slot)
    {
        slot = key switch
        {
            Key.D1 or Key.NumPad1 => 1,
            Key.D2 or Key.NumPad2 => 2,
            Key.D3 or Key.NumPad3 => 3,
            Key.D4 or Key.NumPad4 => 4,
            Key.D5 or Key.NumPad5 => 5,
            _ => 0
        };

        return slot != 0;
    }

    public static bool IsPreviewResetKey(Key key)
    {
        return key is Key.D0 or Key.NumPad0;
    }
}
