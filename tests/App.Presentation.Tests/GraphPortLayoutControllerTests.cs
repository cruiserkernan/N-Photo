using App.Presentation.Controllers;
using Avalonia;
using Editor.Domain.Graph;

namespace App.Presentation.Tests;

public class GraphPortLayoutControllerTests
{
    [Fact]
    public void TryResolveAnchorPlan_StandardInput_UsesTopEdge()
    {
        var nodeType = NodeTypeCatalog.GetByName(NodeTypes.Blend);

        var resolved = GraphPortLayoutController.TryResolveAnchorPlan(
            nodeType,
            "Base",
            PortDirection.Input,
            out var plan);

        Assert.True(resolved);
        Assert.Equal(GraphPortSide.Top, plan.Side);
        Assert.Equal(0, plan.Index);
        Assert.Equal(2, plan.Count);
    }

    [Fact]
    public void TryResolveAnchorPlan_MaskInput_UsesRightEdge()
    {
        var nodeType = NodeTypeCatalog.GetByName(NodeTypes.Blend);

        var resolved = GraphPortLayoutController.TryResolveAnchorPlan(
            nodeType,
            NodePortNames.Mask,
            PortDirection.Input,
            out var plan);

        Assert.True(resolved);
        Assert.Equal(GraphPortSide.Right, plan.Side);
        Assert.Equal(0, plan.Index);
        Assert.Equal(1, plan.Count);
    }

    [Fact]
    public void TryResolveAnchorPlan_Output_UsesBottomEdge()
    {
        var nodeType = NodeTypeCatalog.GetByName(NodeTypes.Transform);

        var resolved = GraphPortLayoutController.TryResolveAnchorPlan(
            nodeType,
            NodePortNames.Image,
            PortDirection.Output,
            out var plan);

        Assert.True(resolved);
        Assert.Equal(GraphPortSide.Bottom, plan.Side);
    }

    [Fact]
    public void ResolveAnchor_SinglePort_CentersOnEdge()
    {
        var position = new Point(10, 20);
        var topPlan = new GraphPortAnchorPlan(GraphPortSide.Top, 0, 1);
        var rightPlan = new GraphPortAnchorPlan(GraphPortSide.Right, 0, 1);

        var topAnchor = GraphPortLayoutController.ResolveAnchor(position, 100, 80, topPlan);
        var rightAnchor = GraphPortLayoutController.ResolveAnchor(position, 100, 80, rightPlan);

        Assert.Equal(new Point(60, 20), topAnchor);
        Assert.Equal(new Point(110, 60), rightAnchor);
    }

    [Fact]
    public void ResolveNodeCenter_ReturnsCardMidpoint()
    {
        var center = GraphPortLayoutController.ResolveNodeCenter(
            new Point(10, 20),
            cardWidth: 100,
            cardHeight: 80);

        Assert.Equal(new Point(60, 60), center);
    }

    [Fact]
    public void TryResolveBorderIntersection_UsesDirectionToTowardPoint()
    {
        var resolved = GraphPortLayoutController.TryResolveBorderIntersection(
            nodeCenter: new Point(60, 60),
            cardWidth: 100,
            cardHeight: 80,
            towardPoint: new Point(260, 60),
            out var borderPoint);

        Assert.True(resolved);
        Assert.Equal(new Point(110, 60), borderPoint);
    }
}
