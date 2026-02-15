namespace Editor.Engine.Abstractions;

public readonly record struct NodeTypeId
{
    public NodeTypeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Node type id cannot be empty.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;

    public static implicit operator NodeTypeId(string value) => new(value);

    public static implicit operator string(NodeTypeId id) => id.Value;
}
