using App.Presentation.Controllers;
using Editor.Application;
using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine;
using Editor.IO;
using Editor.Nodes;

namespace App.Presentation.Tests;

public sealed class NodeActionControllerTests
{
    [Fact]
    public async Task ExecuteAsync_PickImageSource_LoadsImageAndStoresDisplayText()
    {
        var registry = new BuiltInNodeModuleRegistry();
        using var session = new EditorSession(new BootstrapEditorEngine(registry), registry);
        var snapshot = session.GetSnapshot();
        var nodeId = snapshot.InputNodeId;
        var selectedPath = @"C:\images\source.png";
        var image = new RgbaImage(1, 1);
        var loader = new StubImageLoader(image);
        var previewRequested = false;
        var propertiesRefreshed = false;
        var statusMessages = new List<string>();
        var controller = new NodeActionController(
            session,
            loader,
            _ => Task.FromResult<string?>(selectedPath),
            () => previewRequested = true,
            () => propertiesRefreshed = true,
            statusMessages.Add);

        await controller.ExecuteAsync(nodeId, NodeActionIds.PickImageSource);

        Assert.True(previewRequested);
        Assert.True(propertiesRefreshed);
        Assert.Equal(selectedPath, controller.ResolveDisplayText(nodeId, NodeActionIds.PickImageSource));
        Assert.Contains(statusMessages, message => message.Contains("Loaded source.png", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PruneUnavailableNodes_RemovesDisplayText()
    {
        var registry = new BuiltInNodeModuleRegistry();
        using var session = new EditorSession(new BootstrapEditorEngine(registry), registry);
        var nodeId = session.GetSnapshot().InputNodeId;
        var controller = new NodeActionController(
            session,
            new StubImageLoader(new RgbaImage(1, 1)),
            _ => Task.FromResult<string?>(@"C:\images\source.png"),
            () => { },
            () => { },
            _ => { });

        await controller.ExecuteAsync(nodeId, NodeActionIds.PickImageSource);
        controller.PruneUnavailableNodes(new HashSet<NodeId>());

        Assert.Null(controller.ResolveDisplayText(nodeId, NodeActionIds.PickImageSource));
    }

    [Fact]
    public async Task ExecuteAsync_UnknownAction_Throws()
    {
        var registry = new BuiltInNodeModuleRegistry();
        using var session = new EditorSession(new BootstrapEditorEngine(registry), registry);
        var nodeId = session.GetSnapshot().InputNodeId;
        var controller = new NodeActionController(
            session,
            new StubImageLoader(new RgbaImage(1, 1)),
            _ => Task.FromResult<string?>(null),
            () => { },
            () => { },
            _ => { });

        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.ExecuteAsync(nodeId, "UnknownAction"));
    }

    private sealed class StubImageLoader : IImageLoader
    {
        private readonly RgbaImage _image;

        public StubImageLoader(RgbaImage image)
        {
            _image = image;
        }

        public bool TryLoad(string path, out RgbaImage? image, out string errorMessage)
        {
            image = _image.Clone();
            errorMessage = string.Empty;
            return true;
        }
    }
}
