namespace Editor.Domain.Graph;

public sealed record GraphNodeState(
    NodeId NodeId,
    string NodeType,
    IReadOnlyDictionary<string, ParameterValue> Parameters);
