namespace Editor.Domain.Graph;

public interface IDagValidator
{
    GraphValidationResult Validate(NodeGraph graph);

    bool CanConnect(NodeGraph graph, Edge edge);
}
