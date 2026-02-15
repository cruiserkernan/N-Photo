namespace Editor.Domain.Graph;

public sealed record NodePortDefinition(string Name, PortDirection Direction, NodePortRole Role = NodePortRole.Standard);
