using Editor.Domain.Graph;

namespace Editor.Engine.Abstractions;

public sealed record NodeTypeDescriptor(
    NodeTypeId TypeId,
    string DisplayName,
    NodeTypeDefinition Definition);
