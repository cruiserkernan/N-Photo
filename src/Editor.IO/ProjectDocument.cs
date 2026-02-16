namespace Editor.IO;

public sealed class ProjectDocument
{
    public const int CurrentFormatVersion = 1;

    public int FormatVersion { get; init; } = CurrentFormatVersion;

    public ProjectGraph Graph { get; init; } = new();

    public ProjectUiState Ui { get; init; } = new();

    public ProjectAssets Assets { get; init; } = new();
}

public sealed class ProjectGraph
{
    public Guid InputNodeId { get; init; }

    public Guid OutputNodeId { get; init; }

    public IReadOnlyList<ProjectNode> Nodes { get; init; } = Array.Empty<ProjectNode>();

    public IReadOnlyList<ProjectEdge> Edges { get; init; } = Array.Empty<ProjectEdge>();
}

public sealed class ProjectNode
{
    public Guid Id { get; init; }

    public string Type { get; init; } = string.Empty;

    public IReadOnlyList<ProjectParameter> Parameters { get; init; } = Array.Empty<ProjectParameter>();
}

public sealed class ProjectEdge
{
    public Guid FromNodeId { get; init; }

    public string FromPort { get; init; } = string.Empty;

    public Guid ToNodeId { get; init; }

    public string ToPort { get; init; } = string.Empty;
}

public sealed class ProjectParameter
{
    public string Name { get; init; } = string.Empty;

    public ProjectParameterValue Value { get; init; } = new();
}

public sealed class ProjectParameterValue
{
    public string Kind { get; init; } = string.Empty;

    public float? FloatValue { get; init; }

    public int? IntegerValue { get; init; }

    public bool? BooleanValue { get; init; }

    public string? EnumValue { get; init; }

    public ProjectColorValue? ColorValue { get; init; }
}

public sealed class ProjectColorValue
{
    public float R { get; init; }

    public float G { get; init; }

    public float B { get; init; }

    public float A { get; init; }
}

public sealed class ProjectUiState
{
    public IReadOnlyList<ProjectNodePosition> NodePositions { get; init; } = Array.Empty<ProjectNodePosition>();

    public Guid? SelectedNodeId { get; init; }

    public IReadOnlyList<ProjectPreviewSlotBinding> PreviewSlots { get; init; } = Array.Empty<ProjectPreviewSlotBinding>();

    public int? ActivePreviewSlot { get; init; }
}

public sealed class ProjectNodePosition
{
    public Guid NodeId { get; init; }

    public double X { get; init; }

    public double Y { get; init; }
}

public sealed class ProjectPreviewSlotBinding
{
    public int Slot { get; init; }

    public Guid NodeId { get; init; }
}

public sealed class ProjectAssets
{
    public IReadOnlyList<ProjectImageBinding> ImageInputs { get; init; } = Array.Empty<ProjectImageBinding>();
}

public sealed class ProjectImageBinding
{
    public Guid NodeId { get; init; }

    public string Path { get; init; } = string.Empty;
}
