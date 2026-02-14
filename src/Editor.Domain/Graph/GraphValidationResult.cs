namespace Editor.Domain.Graph;

public sealed record GraphValidationResult(
    bool IsValid,
    string Message,
    IReadOnlyList<NodeId> CyclePath)
{
    public static GraphValidationResult Success { get; } =
        new(true, string.Empty, Array.Empty<NodeId>());

    public static GraphValidationResult Cyclic(IReadOnlyList<NodeId> cyclePath) =>
        new(false, "The graph contains a cycle.", cyclePath);
}
