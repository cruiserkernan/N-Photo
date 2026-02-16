using Editor.Domain.Graph;
using Editor.Tests.Common;
using Editor.Engine;
using Editor.Engine.Rendering;

namespace Editor.Engine.Tests;

public class EngineFoundationTests
{
    [Fact]
    public void GraphCompiler_IsDeterministic_ForSameGraph()
    {
        var engine = new BootstrapEditorEngine();
        var transform = engine.AddNode(NodeTypes.Transform);
        var blur = engine.AddNode(NodeTypes.Blur);

        engine.Connect(engine.InputNodeId, "Image", transform, "Image");
        engine.Connect(transform, "Image", blur, "Image");
        engine.Connect(blur, "Image", engine.OutputNodeId, "Image");

        var compiler = new GraphCompiler();
        var graph = new NodeGraph();
        foreach (var node in engine.Nodes)
        {
            graph.AddNode(new Node(node.Id, node.Type, node.Parameters));
        }

        foreach (var edge in engine.Edges)
        {
            graph.AddEdge(edge, new DagValidator());
        }

        var first = compiler.Compile(graph, engine.OutputNodeId);
        var second = compiler.Compile(graph, engine.OutputNodeId);

        Assert.Equal(first.OrderedNodeIds, second.OrderedNodeIds);
        Assert.Equal(first.Fingerprints, second.Fingerprints);
    }

    [Fact]
    public async Task LatestRenderScheduler_CancelsOlderWork_WhenNewerWorkArrives()
    {
        using var scheduler = new LatestRenderScheduler();
        var completion = 0;
        var cancellations = 0;

        scheduler.ScheduleLatest(async token =>
        {
            try
            {
                await Task.Delay(300, token);
                Interlocked.Exchange(ref completion, 1);
            }
            catch (OperationCanceledException)
            {
                Interlocked.Exchange(ref cancellations, 1);
                throw;
            }
        });

        scheduler.ScheduleLatest(async token =>
        {
            await Task.Delay(40, token);
            Interlocked.Exchange(ref completion, 2);
        });

        await Task.Delay(420);

        Assert.Equal(2, completion);
        Assert.Equal(1, cancellations);
    }

    [Fact]
    public void InMemoryTileCache_ReturnsClonedImageCopy()
    {
        var cache = new InMemoryTileCache();
        var image = new Editor.Domain.Imaging.RgbaImage(2, 2);
        var key = new Editor.Engine.Abstractions.Rendering.TileKey(
            NodeId.New(),
            new Editor.Engine.Abstractions.Rendering.NodeFingerprint("fingerprint"),
            0,
            0,
            0,
            "preview");

        cache.Set(key, image);

        Assert.True(cache.TryGet(key, out var fetched));
        Assert.NotSame(image, fetched);
    }

    [Fact]
    public void LoadGraphDocument_ReplacesGraphAndResetsHistory()
    {
        var engine = new BootstrapEditorEngine();
        engine.AddNode(NodeTypes.Blur);
        Assert.True(engine.CanUndo);

        var inputNodeId = NodeId.New();
        var blurNodeId = NodeId.New();
        var outputNodeId = NodeId.New();
        var replacement = new GraphDocumentState(
            inputNodeId,
            outputNodeId,
            new[]
            {
                new GraphNodeState(
                    inputNodeId,
                    NodeTypes.ImageInput,
                    new Dictionary<string, ParameterValue>(StringComparer.Ordinal)),
                new GraphNodeState(
                    blurNodeId,
                    NodeTypes.Blur,
                    new Dictionary<string, ParameterValue>(StringComparer.Ordinal)
                    {
                        ["Radius"] = ParameterValue.Integer(3)
                    }),
                new GraphNodeState(
                    outputNodeId,
                    NodeTypes.Output,
                    new Dictionary<string, ParameterValue>(StringComparer.Ordinal))
            },
            new[]
            {
                new Edge(inputNodeId, NodePortNames.Image, blurNodeId, NodePortNames.Image),
                new Edge(blurNodeId, NodePortNames.Image, outputNodeId, NodePortNames.Image)
            });

        engine.LoadGraphDocument(replacement);

        Assert.Equal(inputNodeId, engine.InputNodeId);
        Assert.Equal(outputNodeId, engine.OutputNodeId);
        Assert.False(engine.CanUndo);
        Assert.False(engine.CanRedo);
        Assert.Equal(3, engine.Nodes.Count);
        Assert.Equal(2, engine.Edges.Count);
        Assert.Contains(engine.Nodes, node => node.Id == blurNodeId && node.Type == NodeTypes.Blur);
    }

    [Fact]
    public void LoadGraphDocument_InvalidDocument_DoesNotMutateExistingGraph()
    {
        var engine = new BootstrapEditorEngine();
        var before = engine.CaptureGraphDocument();
        var invalidDocument = new GraphDocumentState(
            before.InputNodeId,
            before.OutputNodeId,
            before.Nodes,
            new[]
            {
                new Edge(before.InputNodeId, NodePortNames.Image, NodeId.New(), NodePortNames.Image)
            });

        Assert.Throws<InvalidOperationException>(() => engine.LoadGraphDocument(invalidDocument));

        var after = engine.CaptureGraphDocument();
        Assert.Equal(before.InputNodeId, after.InputNodeId);
        Assert.Equal(before.OutputNodeId, after.OutputNodeId);
        Assert.Equal(
            before.Nodes.Select(CreateGraphNodeSignature),
            after.Nodes.Select(CreateGraphNodeSignature));
        Assert.Equal(before.Edges, after.Edges);
    }

    [Fact]
    public void LoadGraphDocument_ClearsInputImageStateAndCachedOutput()
    {
        var engine = new BootstrapEditorEngine();
        engine.SetInputImage(TestImageFactory.CreateGradient(12, 8));
        Assert.True(engine.TryRenderOutput(out var firstRender, out var firstError), firstError);
        Assert.NotNull(firstRender);

        var current = engine.CaptureGraphDocument();
        engine.LoadGraphDocument(current);

        Assert.False(engine.TryRenderOutput(out var secondRender, out _));
        Assert.Null(secondRender);
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
