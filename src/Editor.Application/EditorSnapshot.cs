using Editor.Domain.Graph;

namespace Editor.Application;

public sealed record EditorSnapshot(
    IReadOnlyList<Node> Nodes,
    IReadOnlyList<Edge> Edges,
    IReadOnlyList<string> AvailableNodeTypes,
    string Status,
    bool CanUndo,
    bool CanRedo,
    NodeId InputNodeId,
    NodeId OutputNodeId);
