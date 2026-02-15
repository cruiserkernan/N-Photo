using Editor.Domain.Graph;

namespace Editor.Engine.Abstractions.Rendering;

public interface IGraphCompiler
{
    GraphExecutionPlan Compile(NodeGraph graph, NodeId targetNodeId);
}
