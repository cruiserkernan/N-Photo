namespace Editor.Domain.Graph;

public sealed record Edge(
    NodeId FromNodeId,
    string FromPort,
    NodeId ToNodeId,
    string ToPort);
