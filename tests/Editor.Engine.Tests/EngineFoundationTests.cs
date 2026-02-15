using Editor.Domain.Graph;
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
}
