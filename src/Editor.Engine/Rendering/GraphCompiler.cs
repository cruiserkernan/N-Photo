using System.Globalization;
using System.Text;
using Editor.Domain.Graph;
using Editor.Engine.Abstractions.Rendering;

namespace Editor.Engine.Rendering;

public sealed class GraphCompiler : IGraphCompiler
{
    public GraphExecutionPlan Compile(NodeGraph graph, NodeId targetNodeId)
    {
        if (!graph.ContainsNode(targetNodeId))
        {
            throw new InvalidOperationException($"Target node '{targetNodeId}' does not exist.");
        }

        var reachable = CollectReachableNodes(graph, targetNodeId);
        var orderedNodeIds = TopologicalSort(graph, reachable);
        var fingerprints = BuildFingerprints(graph, orderedNodeIds, reachable);
        return new GraphExecutionPlan(targetNodeId, orderedNodeIds, fingerprints);
    }

    private static HashSet<NodeId> CollectReachableNodes(NodeGraph graph, NodeId targetNodeId)
    {
        var reachable = new HashSet<NodeId>();
        var pending = new Stack<NodeId>();
        pending.Push(targetNodeId);

        while (pending.Count > 0)
        {
            var nodeId = pending.Pop();
            if (!reachable.Add(nodeId))
            {
                continue;
            }

            foreach (var incoming in graph.GetIncomingEdges(nodeId))
            {
                pending.Push(incoming.FromNodeId);
            }
        }

        return reachable;
    }

    private static IReadOnlyList<NodeId> TopologicalSort(NodeGraph graph, HashSet<NodeId> reachable)
    {
        var indegree = reachable.ToDictionary(nodeId => nodeId, _ => 0);
        var outgoing = reachable.ToDictionary(nodeId => nodeId, _ => new List<NodeId>());

        foreach (var edge in graph.Edges)
        {
            if (!reachable.Contains(edge.FromNodeId) || !reachable.Contains(edge.ToNodeId))
            {
                continue;
            }

            indegree[edge.ToNodeId]++;
            outgoing[edge.FromNodeId].Add(edge.ToNodeId);
        }

        var ready = indegree
            .Where(pair => pair.Value == 0)
            .Select(pair => pair.Key)
            .OrderBy(nodeId => nodeId.Value)
            .ToList();

        var ordered = new List<NodeId>(reachable.Count);
        while (ready.Count > 0)
        {
            var next = ready[0];
            ready.RemoveAt(0);
            ordered.Add(next);

            foreach (var downstream in outgoing[next])
            {
                indegree[downstream]--;
                if (indegree[downstream] != 0)
                {
                    continue;
                }

                ready.Add(downstream);
            }

            ready.Sort((left, right) => left.Value.CompareTo(right.Value));
        }

        if (ordered.Count != reachable.Count)
        {
            throw new InvalidOperationException("Graph compilation failed: reachable subgraph contains a cycle.");
        }

        return ordered;
    }

    private static IReadOnlyDictionary<NodeId, NodeFingerprint> BuildFingerprints(
        NodeGraph graph,
        IReadOnlyList<NodeId> orderedNodeIds,
        HashSet<NodeId> reachable)
    {
        var fingerprints = new Dictionary<NodeId, NodeFingerprint>();

        foreach (var nodeId in orderedNodeIds)
        {
            var node = graph.GetNode(nodeId);
            var builder = new StringBuilder();
            builder.Append(node.Type);

            foreach (var parameter in node.Parameters.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                builder.Append("|p:");
                builder.Append(parameter.Key);
                builder.Append(':');
                builder.Append(FormatParameterValue(parameter.Value));
            }

            var incomingEdges = graph.GetIncomingEdges(nodeId)
                .Where(edge => reachable.Contains(edge.FromNodeId))
                .OrderBy(edge => edge.ToPort, StringComparer.Ordinal)
                .ThenBy(edge => edge.FromNodeId.Value)
                .ThenBy(edge => edge.FromPort, StringComparer.Ordinal)
                .ToArray();

            foreach (var edge in incomingEdges)
            {
                builder.Append("|e:");
                builder.Append(edge.FromNodeId);
                builder.Append(':');
                builder.Append(edge.FromPort);
                builder.Append("->");
                builder.Append(edge.ToPort);
                builder.Append(':');
                builder.Append(fingerprints[edge.FromNodeId].Value);
            }

            fingerprints[nodeId] = new NodeFingerprint(builder.ToString());
        }

        return fingerprints;
    }

    private static string FormatParameterValue(ParameterValue value)
    {
        return value.Kind switch
        {
            ParameterValueKind.Float => value.AsFloat().ToString("0.######", CultureInfo.InvariantCulture),
            ParameterValueKind.Integer => value.AsInteger().ToString(CultureInfo.InvariantCulture),
            ParameterValueKind.Boolean => value.AsBoolean().ToString(CultureInfo.InvariantCulture),
            ParameterValueKind.Enum => value.AsEnum(),
            _ => string.Empty
        };
    }
}
