namespace Editor.Engine.Abstractions.Rendering;

public readonly record struct NodeFingerprint(string Value)
{
    public override string ToString() => Value;
}
