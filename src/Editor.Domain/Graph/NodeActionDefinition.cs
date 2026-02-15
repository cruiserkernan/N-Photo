namespace Editor.Domain.Graph;

public sealed record NodeActionDefinition(
    string Id,
    string Label,
    string ButtonText,
    string EmptyDisplayText = "");
