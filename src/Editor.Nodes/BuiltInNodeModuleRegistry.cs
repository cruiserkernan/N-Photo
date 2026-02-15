using Editor.Domain.Graph;
using Editor.Engine.Abstractions;
using Editor.Nodes.Modules;

namespace Editor.Nodes;

public sealed class BuiltInNodeModuleRegistry : INodeModuleRegistry
{
    private readonly IReadOnlyDictionary<NodeTypeId, INodeModule> _byId;
    private readonly IReadOnlyDictionary<string, INodeModule> _byName;

    public BuiltInNodeModuleRegistry()
    {
        Modules = new INodeModule[]
        {
            new ImageInputNodeModule(),
            new ElbowNodeModule(),
            new TransformNodeModule(),
            new CropNodeModule(),
            new ExposureContrastNodeModule(),
            new CurvesNodeModule(),
            new HslNodeModule(),
            new BlurNodeModule(),
            new SharpenNodeModule(),
            new BlendNodeModule(),
            new OutputNodeModule()
        };

        _byId = Modules.ToDictionary(module => module.TypeId);
        _byName = Modules.ToDictionary(module => module.TypeId.Value, StringComparer.Ordinal);
        NodeTypes = Modules
            .Select(module => new NodeTypeDescriptor(module.TypeId, module.TypeId.Value, module.Definition))
            .OrderBy(descriptor => descriptor.DisplayName, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<INodeModule> Modules { get; }

    public IReadOnlyList<NodeTypeDescriptor> NodeTypes { get; }

    public bool TryGet(NodeTypeId typeId, out INodeModule module)
    {
        return _byId.TryGetValue(typeId, out module!);
    }

    public bool TryGet(string typeName, out INodeModule module)
    {
        return _byName.TryGetValue(typeName, out module!);
    }

    public INodeModule Get(NodeTypeId typeId)
    {
        return TryGet(typeId, out var module)
            ? module
            : throw new InvalidOperationException($"Node type '{typeId}' is not registered.");
    }
}
