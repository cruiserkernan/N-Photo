using Editor.Application;
using Editor.Domain.Graph;
using Editor.Engine;
using Editor.Engine.Abstractions;
using Editor.Nodes;

namespace Editor.Application.Tests;

public class EditorSessionTests
{
    [Fact]
    public void Snapshot_ExposesEngineStateAndNodeTypes()
    {
        var registry = new BuiltInNodeModuleRegistry();
        var engine = new BootstrapEditorEngine(registry);
        var session = new EditorSession(engine, registry);

        var snapshot = session.GetSnapshot();

        Assert.NotEmpty(snapshot.Nodes);
        Assert.Contains(NodeTypes.Transform, snapshot.AvailableNodeTypes);
        Assert.Equal(engine.InputNodeId, snapshot.InputNodeId);
        Assert.Equal(engine.OutputNodeId, snapshot.OutputNodeId);
    }

    [Fact]
    public void AddNode_UsesTypedNodeTypeId()
    {
        var registry = new BuiltInNodeModuleRegistry();
        var engine = new BootstrapEditorEngine(registry);
        var session = new EditorSession(engine, registry);

        var nodeId = session.AddNode(new NodeTypeId(NodeTypes.Blur));

        var node = session.GetSnapshot().Nodes.Single(candidate => candidate.Id == nodeId);
        Assert.Equal(NodeTypes.Blur, node.Type);
    }
}
