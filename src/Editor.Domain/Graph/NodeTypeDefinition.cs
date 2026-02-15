namespace Editor.Domain.Graph;

public sealed class NodeTypeDefinition
{
    public NodeTypeDefinition(
        string name,
        IReadOnlyList<NodePortDefinition> inputs,
        IReadOnlyList<NodePortDefinition> outputs,
        IReadOnlyDictionary<string, NodeParameterDefinition>? parameters = null,
        IReadOnlyList<NodeActionDefinition>? actions = null)
    {
        Name = name;
        Inputs = inputs;
        Outputs = outputs;
        Parameters = parameters ?? new Dictionary<string, NodeParameterDefinition>(StringComparer.Ordinal);
        Actions = actions ?? Array.Empty<NodeActionDefinition>();
    }

    public string Name { get; }

    public IReadOnlyList<NodePortDefinition> Inputs { get; }

    public IReadOnlyList<NodePortDefinition> Outputs { get; }

    public IReadOnlyDictionary<string, NodeParameterDefinition> Parameters { get; }

    public IReadOnlyList<NodeActionDefinition> Actions { get; }

    public bool HasInputPort(string name) => Inputs.Any(port => string.Equals(port.Name, name, StringComparison.Ordinal));

    public bool HasOutputPort(string name) => Outputs.Any(port => string.Equals(port.Name, name, StringComparison.Ordinal));

    public Dictionary<string, ParameterValue> CreateDefaultParameterValues()
    {
        var values = new Dictionary<string, ParameterValue>(StringComparer.Ordinal);
        foreach (var parameter in Parameters.Values)
        {
            values.Add(parameter.Name, parameter.DefaultValue);
        }

        return values;
    }
}
