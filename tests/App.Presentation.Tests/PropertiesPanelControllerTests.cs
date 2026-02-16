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

    [Fact]
    public void Refresh_ParameterUpdateSuccess_InvokesMutationCallback()
    {
        var registry = new BuiltInNodeModuleRegistry();
        using var session = new EditorSession(new BootstrapEditorEngine(registry), registry);
        var transformNodeId = session.AddNode(new Editor.Engine.Abstractions.NodeTypeId(NodeTypes.Transform));
        var mutationCallbackCount = 0;
        var controller = new PropertiesPanelController(
            session,
            (_, _) => Task.CompletedTask,
            (_, _) => null,
            () => { },
            _ => { },
            () => mutationCallbackCount++,
            new ParameterEditorPrimitiveRegistry(
            [
                new TriggerValueParameterEditorPrimitive(ParameterValue.Float(1.2f))
            ]));

        var host = new StackPanel();
        var selectedNodeText = new TextBlock();
        var snapshot = session.GetSnapshot();
        var nodeLookup = snapshot.Nodes.ToDictionary(node => node.Id);

        controller.Refresh(host, selectedNodeText, transformNodeId, nodeLookup);

        var applyButton = host.Children.OfType<Button>().First();
        applyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.Equal(1, mutationCallbackCount);
    }

    [Fact]
    public void Refresh_ParameterUpdateFailure_DoesNotInvokeMutationCallback()
    {
        var registry = new BuiltInNodeModuleRegistry();
        using var session = new EditorSession(new BootstrapEditorEngine(registry), registry);
        var transformNodeId = session.AddNode(new Editor.Engine.Abstractions.NodeTypeId(NodeTypes.Transform));
        var mutationCallbackCount = 0;
        var statusMessages = new List<string>();
        var controller = new PropertiesPanelController(
            session,
            (_, _) => Task.CompletedTask,
            (_, _) => null,
            () => { },
            statusMessages.Add,
            () => mutationCallbackCount++,
            new ParameterEditorPrimitiveRegistry(
            [
                new TriggerValueParameterEditorPrimitive(ParameterValue.Boolean(true))
            ]));

        var host = new StackPanel();
        var selectedNodeText = new TextBlock();
        var snapshot = session.GetSnapshot();
        var nodeLookup = snapshot.Nodes.ToDictionary(node => node.Id);

        controller.Refresh(host, selectedNodeText, transformNodeId, nodeLookup);

        var applyButton = host.Children.OfType<Button>().First();
        applyButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.Equal(0, mutationCallbackCount);
        Assert.Contains(statusMessages, message => message.Contains("Set parameter failed", StringComparison.Ordinal));
    }

    private sealed class TriggerValueParameterEditorPrimitive : IParameterEditorPrimitive
    {
        private readonly ParameterValue _valueToApply;

        public TriggerValueParameterEditorPrimitive(ParameterValue valueToApply)
        {
            _valueToApply = valueToApply;
        }

        public string Id => "test-trigger";

        public bool CanCreate(NodeParameterDefinition definition, ParameterValue currentValue)
        {
            return definition.Kind == ParameterValueKind.Float;
        }

        public Control Create(ParameterEditorPrimitiveContext context)
        {
            var button = new Button
            {
                Content = "Apply"
            };
            button.Click += (_, _) => context.ApplyValue(_valueToApply);
            return button;
        }
    }
}
