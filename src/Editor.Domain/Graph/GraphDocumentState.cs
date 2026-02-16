namespace Editor.Domain.Graph;

public sealed record GraphDocumentState(
    NodeId InputNodeId,
    NodeId OutputNodeId,
    IReadOnlyList<GraphNodeState> Nodes,
    IReadOnlyList<Edge> Edges);
