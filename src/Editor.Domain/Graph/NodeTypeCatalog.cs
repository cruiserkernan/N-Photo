namespace Editor.Domain.Graph;

public static class NodeTypeCatalog
{
    private static readonly IReadOnlyDictionary<string, NodeTypeDefinition> Definitions =
        new Dictionary<string, NodeTypeDefinition>(StringComparer.Ordinal)
        {
            [NodeTypes.ImageInput] = new(
                NodeTypes.ImageInput,
                inputs: Array.Empty<NodePortDefinition>(),
                outputs: new[] { new NodePortDefinition(NodePortNames.Image, PortDirection.Output) },
                actions: new[]
                {
                    new NodeActionDefinition(
                        NodeActionIds.PickImageSource,
                        "Source Image",
                        "Choose Image...",
                        "No image selected.")
                }),
            [NodeTypes.Elbow] = new(
                NodeTypes.Elbow,
                inputs: new[] { new NodePortDefinition(NodePortNames.Image, PortDirection.Input) },
                outputs: new[] { new NodePortDefinition(NodePortNames.Image, PortDirection.Output) }),
            [NodeTypes.Transform] = new(
                NodeTypes.Transform,
                inputs: new[]
                {
                    new NodePortDefinition(NodePortNames.Image, PortDirection.Input),
                    new NodePortDefinition(NodePortNames.Mask, PortDirection.Input, NodePortRole.Mask)
                },
                outputs: new[] { new NodePortDefinition(NodePortNames.Image, PortDirection.Output) },
                parameters: ParameterDefinitions(
                    new NodeParameterDefinition("Scale", ParameterValueKind.Float, ParameterValue.Float(1.0f), MinFloat: 0.1f, MaxFloat: 4.0f, EditorPrimitive: "slider"),
                    new NodeParameterDefinition("RotateDegrees", ParameterValueKind.Float, ParameterValue.Float(0.0f), MinFloat: -180.0f, MaxFloat: 180.0f, EditorPrimitive: "slider"))),
            [NodeTypes.Crop] = new(
                NodeTypes.Crop,
                inputs: new[] { new NodePortDefinition(NodePortNames.Image, PortDirection.Input) },
                outputs: new[] { new NodePortDefinition(NodePortNames.Image, PortDirection.Output) },
                parameters: ParameterDefinitions(
                    new NodeParameterDefinition("X", ParameterValueKind.Integer, ParameterValue.Integer(0), MinInt: 0, MaxInt: 8192, EditorPrimitive: "number-input"),
                    new NodeParameterDefinition("Y", ParameterValueKind.Integer, ParameterValue.Integer(0), MinInt: 0, MaxInt: 8192, EditorPrimitive: "number-input"),
                    new NodeParameterDefinition("Width", ParameterValueKind.Integer, ParameterValue.Integer(512), MinInt: 1, MaxInt: 8192, EditorPrimitive: "number-input"),
                    new NodeParameterDefinition("Height", ParameterValueKind.Integer, ParameterValue.Integer(512), MinInt: 1, MaxInt: 8192, EditorPrimitive: "number-input"))),
            [NodeTypes.ExposureContrast] = new(
                NodeTypes.ExposureContrast,
                inputs: new[]
                {
                    new NodePortDefinition(NodePortNames.Image, PortDirection.Input),
                    new NodePortDefinition(NodePortNames.Mask, PortDirection.Input, NodePortRole.Mask)
                },
                outputs: new[] { new NodePortDefinition(NodePortNames.Image, PortDirection.Output) },
                parameters: ParameterDefinitions(
                    new NodeParameterDefinition("Exposure", ParameterValueKind.Float, ParameterValue.Float(0.0f), MinFloat: -5.0f, MaxFloat: 5.0f, EditorPrimitive: "slider"),
                    new NodeParameterDefinition("Contrast", ParameterValueKind.Float, ParameterValue.Float(1.0f), MinFloat: 0.0f, MaxFloat: 3.0f, EditorPrimitive: "slider"))),
            [NodeTypes.Curves] = new(
                NodeTypes.Curves,
                inputs: new[]
                {
                    new NodePortDefinition(NodePortNames.Image, PortDirection.Input),
                    new NodePortDefinition(NodePortNames.Mask, PortDirection.Input, NodePortRole.Mask)
                },
                outputs: new[] { new NodePortDefinition(NodePortNames.Image, PortDirection.Output) },
                parameters: ParameterDefinitions(
                    new NodeParameterDefinition("Gamma", ParameterValueKind.Float, ParameterValue.Float(1.0f), MinFloat: 0.2f, MaxFloat: 4.0f, EditorPrimitive: "slider"))),
            [NodeTypes.Hsl] = new(
                NodeTypes.Hsl,
                inputs: new[]
                {
                    new NodePortDefinition(NodePortNames.Image, PortDirection.Input),
                    new NodePortDefinition(NodePortNames.Mask, PortDirection.Input, NodePortRole.Mask)
                },
                outputs: new[] { new NodePortDefinition(NodePortNames.Image, PortDirection.Output) },
                parameters: ParameterDefinitions(
                    new NodeParameterDefinition("HueShift", ParameterValueKind.Float, ParameterValue.Float(0.0f), MinFloat: -180.0f, MaxFloat: 180.0f, EditorPrimitive: "slider"),
                    new NodeParameterDefinition("Saturation", ParameterValueKind.Float, ParameterValue.Float(1.0f), MinFloat: 0.0f, MaxFloat: 2.0f, EditorPrimitive: "slider"),
                    new NodeParameterDefinition("Lightness", ParameterValueKind.Float, ParameterValue.Float(1.0f), MinFloat: 0.0f, MaxFloat: 2.0f, EditorPrimitive: "slider"))),
            [NodeTypes.Blur] = new(
                NodeTypes.Blur,
                inputs: new[]
                {
                    new NodePortDefinition(NodePortNames.Image, PortDirection.Input),
                    new NodePortDefinition(NodePortNames.Mask, PortDirection.Input, NodePortRole.Mask)
                },
                outputs: new[] { new NodePortDefinition(NodePortNames.Image, PortDirection.Output) },
                parameters: ParameterDefinitions(
                    new NodeParameterDefinition("Radius", ParameterValueKind.Integer, ParameterValue.Integer(2), MinInt: 0, MaxInt: 32, EditorPrimitive: "slider"))),
            [NodeTypes.Sharpen] = new(
                NodeTypes.Sharpen,
                inputs: new[]
                {
                    new NodePortDefinition(NodePortNames.Image, PortDirection.Input),
                    new NodePortDefinition(NodePortNames.Mask, PortDirection.Input, NodePortRole.Mask)
                },
                outputs: new[] { new NodePortDefinition(NodePortNames.Image, PortDirection.Output) },
                parameters: ParameterDefinitions(
                    new NodeParameterDefinition("Amount", ParameterValueKind.Float, ParameterValue.Float(1.0f), MinFloat: 0.0f, MaxFloat: 5.0f, EditorPrimitive: "slider"),
                    new NodeParameterDefinition("Radius", ParameterValueKind.Integer, ParameterValue.Integer(1), MinInt: 0, MaxInt: 8, EditorPrimitive: "slider"))),
            [NodeTypes.Blend] = new(
                NodeTypes.Blend,
                inputs: new[]
                {
                    new NodePortDefinition("Base", PortDirection.Input),
                    new NodePortDefinition("Top", PortDirection.Input),
                    new NodePortDefinition(NodePortNames.Mask, PortDirection.Input, NodePortRole.Mask)
                },
                outputs: new[] { new NodePortDefinition(NodePortNames.Image, PortDirection.Output) },
                parameters: ParameterDefinitions(
                    new NodeParameterDefinition("Mode", ParameterValueKind.Enum, ParameterValue.Enum("over"), EnumValues: new[] { "over", "multiply", "screen" }),
                    new NodeParameterDefinition("Opacity", ParameterValueKind.Float, ParameterValue.Float(1.0f), MinFloat: 0.0f, MaxFloat: 1.0f, EditorPrimitive: "slider"))),
            [NodeTypes.Output] = new(
                NodeTypes.Output,
                inputs: new[] { new NodePortDefinition(NodePortNames.Image, PortDirection.Input) },
                outputs: new[] { new NodePortDefinition(NodePortNames.Image, PortDirection.Output) })
        };

    public static IEnumerable<NodeTypeDefinition> All => Definitions.Values;

    public static NodeTypeDefinition GetByName(string type)
    {
        return Definitions.TryGetValue(type, out var definition)
            ? definition
            : throw new InvalidOperationException($"Node type '{type}' is not registered.");
    }

    private static IReadOnlyDictionary<string, NodeParameterDefinition> ParameterDefinitions(
        params NodeParameterDefinition[] definitions)
    {
        return definitions.ToDictionary(definition => definition.Name, StringComparer.Ordinal);
    }
}
