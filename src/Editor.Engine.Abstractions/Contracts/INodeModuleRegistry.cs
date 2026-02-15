namespace Editor.Engine.Abstractions;

public interface INodeModuleRegistry
{
    IReadOnlyList<INodeModule> Modules { get; }

    IReadOnlyList<NodeTypeDescriptor> NodeTypes { get; }

    bool TryGet(NodeTypeId typeId, out INodeModule module);

    bool TryGet(string typeName, out INodeModule module);

    INodeModule Get(NodeTypeId typeId);
}
