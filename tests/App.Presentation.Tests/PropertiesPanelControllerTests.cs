using App.Presentation.Controllers;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Editor.Application;
using Editor.Domain.Graph;
using Editor.Engine;
using Editor.Nodes;

namespace App.Presentation.Tests;

public class PropertiesPanelControllerTests
{
    [Fact]
    public void Refresh_NoSelection_ShowsEmptyState()
    {
        var registry = new BuiltInNodeModuleRegistry();
        using var session = new EditorSession(new BootstrapEditorEngine(registry), registry);
        var controller = new PropertiesPanelController(
            session,
            (_, _) => Task.CompletedTask,
            (_, _) => null,
            () => { },
            _ => { });

        var host = new StackPanel();
        var selectedNodeText = new TextBlock();

        controller.Refresh(
            host,
            selectedNodeText,
            selectedNodeId: null,
            nodeLookup: new Dictionary<NodeId, Node>());

        Assert.Equal("None", selectedNodeText.Text);
        Assert.Single(host.Children);
        var hint = Assert.IsType<TextBlock>(host.Children[0]);
        Assert.Contains("Select a node", hint.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Refresh_RendersNodeActionsFromMetadata_AndExecutesAction()
    {
        var registry = new BuiltInNodeModuleRegistry();
        using var session = new EditorSession(new BootstrapEditorEngine(registry), registry);

        NodeId? capturedNodeId = null;
        string? capturedActionId = null;

        var controller = new PropertiesPanelController(
            session,
            (nodeId, actionId) =>
            {
                capturedNodeId = nodeId;
                capturedActionId = actionId;
                return Task.CompletedTask;
            },
            (_, _) => null,
            () => { },
            _ => { });

        var host = new StackPanel();
        var selectedNodeText = new TextBlock();
        var snapshot = session.GetSnapshot();
        var nodeLookup = snapshot.Nodes.ToDictionary(node => node.Id);

        controller.Refresh(
            host,
            selectedNodeText,
            snapshot.InputNodeId,
            nodeLookup);

        var actionButton = host.Children
            .OfType<Border>()
            .Select(border => border.Child)
            .OfType<StackPanel>()
            .SelectMany(panel => panel.Children)
            .OfType<Button>()
            .FirstOrDefault();

        Assert.NotNull(actionButton);
        actionButton!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.Equal(snapshot.InputNodeId, capturedNodeId);
        Assert.Equal(NodeActionIds.PickImageSource, capturedActionId);
    }
}
