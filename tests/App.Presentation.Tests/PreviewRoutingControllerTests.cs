using App.Presentation.Controllers;
using Avalonia.Input;
using Editor.Domain.Graph;

namespace App.Presentation.Tests;

public class PreviewRoutingControllerTests
{
    [Fact]
    public void AssignAndResolveActiveTarget_ReturnsAssignedNode()
    {
        var controller = new PreviewRoutingController();
        var nodeId = NodeId.New();

        controller.AssignSlot(1, nodeId);

        Assert.True(controller.TryGetActiveTarget(new[] { nodeId }, out var resolved));
        Assert.Equal(nodeId, resolved);
    }

    [Fact]
    public void Prune_RemovesDeadSlotAndResetsActive()
    {
        var controller = new PreviewRoutingController();
        var nodeId = NodeId.New();

        controller.AssignSlot(2, nodeId);
        var wasReset = controller.Prune(Array.Empty<NodeId>());

        Assert.True(wasReset);
        Assert.Null(controller.ActiveSlot);
    }

    [Fact]
    public void TryMapPreviewSlot_MapsNumericKeys()
    {
        Assert.True(PreviewRoutingController.TryMapPreviewSlot(Key.D1, out var slot));
        Assert.Equal(1, slot);
        Assert.True(PreviewRoutingController.IsPreviewResetKey(Key.D0));
    }
}
