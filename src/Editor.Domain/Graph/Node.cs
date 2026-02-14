namespace Editor.Domain.Graph;

public sealed class Node
{
    private readonly Dictionary<string, ParameterValue> _parameters;

    public Node(NodeId id, string type)
        : this(id, type, null)
    {
    }

    public Node(NodeId id, string type, IReadOnlyDictionary<string, ParameterValue>? parameterOverrides)
    {
        var nodeType = NodeTypeCatalog.GetByName(type);

        Id = id;
        Type = nodeType.Name;
        _parameters = nodeType.CreateDefaultParameterValues();

        if (parameterOverrides is null)
        {
            return;
        }

        foreach (var overrideParameter in parameterOverrides)
        {
            SetParameter(overrideParameter.Key, overrideParameter.Value);
        }
    }

    public NodeId Id { get; }

    public string Type { get; }

    public IReadOnlyDictionary<string, ParameterValue> Parameters => _parameters;

    public ParameterValue GetParameter(string parameterName)
    {
        return _parameters.TryGetValue(parameterName, out var value)
            ? value
            : throw new InvalidOperationException($"Parameter '{parameterName}' does not exist on node type '{Type}'.");
    }

    public void SetParameter(string parameterName, ParameterValue value)
    {
        var nodeType = NodeTypeCatalog.GetByName(Type);
        if (!nodeType.Parameters.TryGetValue(parameterName, out var definition))
        {
            throw new InvalidOperationException($"Parameter '{parameterName}' does not exist on node type '{Type}'.");
        }

        definition.Validate(value);
        _parameters[parameterName] = value;
    }
}
