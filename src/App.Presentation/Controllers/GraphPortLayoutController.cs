using Avalonia;
using Editor.Domain.Graph;

namespace App.Presentation.Controllers;

public enum GraphPortSide
{
    Top = 0,
    Bottom = 1,
    Right = 2
}

public readonly record struct GraphPortAnchorPlan(GraphPortSide Side, int Index, int Count);

public static class GraphPortLayoutController
{
    public static bool IsMaskPort(NodePortDefinition port)
    {
        return port.Direction == PortDirection.Input && port.Role == NodePortRole.Mask;
    }

    public static bool IsMaskInput(NodeTypeDefinition nodeType, string portName)
    {
        return nodeType.Inputs.Any(port =>
            string.Equals(port.Name, portName, StringComparison.Ordinal) &&
            IsMaskPort(port));
    }

    public static bool TryResolveAnchorPlan(
        NodeTypeDefinition nodeType,
        string portName,
        PortDirection direction,
        out GraphPortAnchorPlan plan)
    {
        if (direction == PortDirection.Output)
        {
            return TryResolvePlanForPorts(
                nodeType.Outputs,
                portName,
                GraphPortSide.Bottom,
                out plan);
        }

        var maskInputs = nodeType.Inputs.Where(IsMaskPort).ToArray();
        if (TryResolvePlanForPorts(maskInputs, portName, GraphPortSide.Right, out plan))
        {
            return true;
        }

        var standardInputs = nodeType.Inputs.Where(port => !IsMaskPort(port)).ToArray();
        return TryResolvePlanForPorts(standardInputs, portName, GraphPortSide.Top, out plan);
    }

    public static Point ResolveAnchor(
        Point nodePosition,
        double cardWidth,
        double cardHeight,
        GraphPortAnchorPlan plan,
        double edgePadding = 18)
    {
        return plan.Side switch
        {
            GraphPortSide.Top => new Point(
                nodePosition.X + ResolveEdgeOffset(plan.Index, plan.Count, cardWidth, edgePadding),
                nodePosition.Y),
            GraphPortSide.Bottom => new Point(
                nodePosition.X + ResolveEdgeOffset(plan.Index, plan.Count, cardWidth, edgePadding),
                nodePosition.Y + cardHeight),
            _ => new Point(
                nodePosition.X + cardWidth,
                nodePosition.Y + ResolveEdgeOffset(plan.Index, plan.Count, cardHeight, edgePadding))
        };
    }

    public static Point ResolveNodeCenter(Point nodePosition, double cardWidth, double cardHeight)
    {
        return new Point(
            nodePosition.X + (cardWidth / 2),
            nodePosition.Y + (cardHeight / 2));
    }

    public static bool TryResolveBorderIntersection(
        Point nodeCenter,
        double cardWidth,
        double cardHeight,
        Point towardPoint,
        out Point borderPoint)
    {
        borderPoint = default;

        var dx = towardPoint.X - nodeCenter.X;
        var dy = towardPoint.Y - nodeCenter.Y;
        if (Math.Abs(dx) < 0.001 && Math.Abs(dy) < 0.001)
        {
            return false;
        }

        var halfWidth = Math.Max(0.001, cardWidth / 2);
        var halfHeight = Math.Max(0.001, cardHeight / 2);
        var scale = Math.Max(Math.Abs(dx) / halfWidth, Math.Abs(dy) / halfHeight);
        if (scale < 0.001)
        {
            return false;
        }

        borderPoint = new Point(
            nodeCenter.X + (dx / scale),
            nodeCenter.Y + (dy / scale));
        return true;
    }

    private static bool TryResolvePlanForPorts(
        IReadOnlyList<NodePortDefinition> ports,
        string portName,
        GraphPortSide side,
        out GraphPortAnchorPlan plan)
    {
        for (var i = 0; i < ports.Count; i++)
        {
            if (!string.Equals(ports[i].Name, portName, StringComparison.Ordinal))
            {
                continue;
            }

            plan = new GraphPortAnchorPlan(side, i, ports.Count);
            return true;
        }

        plan = default;
        return false;
    }

    private static double ResolveEdgeOffset(int index, int count, double length, double edgePadding)
    {
        if (count <= 1)
        {
            return length / 2;
        }

        var safePadding = Math.Min(edgePadding, length / 2);
        var span = Math.Max(0, length - (safePadding * 2));
        var step = span / (count - 1);
        return safePadding + (index * step);
    }
}
