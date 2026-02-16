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

    [Fact]
    public void CaptureAndLoadGraphDocument_RoundTripsThroughSessionBoundary()
    {
        var registry = new BuiltInNodeModuleRegistry();
        var engine = new BootstrapEditorEngine(registry);
        var session = new EditorSession(engine, registry);
        var baseline = session.CaptureGraphDocument();

        session.AddNode(new NodeTypeId(NodeTypes.Transform));
        var mutated = session.CaptureGraphDocument();
        Assert.NotEqual(baseline.Nodes.Count, mutated.Nodes.Count);

        session.LoadGraphDocument(baseline);
        var restored = session.CaptureGraphDocument();

        Assert.Equal(baseline.InputNodeId, restored.InputNodeId);
        Assert.Equal(baseline.OutputNodeId, restored.OutputNodeId);
        Assert.Equal(
            baseline.Nodes.Select(CreateGraphNodeSignature),
            restored.Nodes.Select(CreateGraphNodeSignature));
        Assert.Equal(baseline.Edges, restored.Edges);
    }

    private static string CreateGraphNodeSignature(GraphNodeState node)
    {
        var parameterSignature = string.Join(
            "|",
            node.Parameters
                .OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
                .Select(parameter => $"{parameter.Key}:{parameter.Value.Kind}:{parameter.Value.Value}"));
        return $"{node.NodeId.Value:N}:{node.NodeType}:{parameterSignature}";
    }
}
