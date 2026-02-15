using Editor.Domain.Graph;
using Editor.Nodes;

namespace Editor.Nodes.Tests;

public class BuiltInNodeModuleRegistryTests
{
    [Fact]
    public void Registry_ContainsExpectedCoreNodeTypes()
    {
        var registry = new BuiltInNodeModuleRegistry();

        Assert.True(registry.TryGet(new Editor.Engine.Abstractions.NodeTypeId(NodeTypes.ImageInput), out _));
        Assert.True(registry.TryGet(new Editor.Engine.Abstractions.NodeTypeId(NodeTypes.Elbow), out _));
        Assert.True(registry.TryGet(new Editor.Engine.Abstractions.NodeTypeId(NodeTypes.Transform), out _));
        Assert.True(registry.TryGet(new Editor.Engine.Abstractions.NodeTypeId(NodeTypes.Output), out _));
        Assert.Contains(registry.NodeTypes, type => type.TypeId.Value == NodeTypes.Blend);
    }
}
