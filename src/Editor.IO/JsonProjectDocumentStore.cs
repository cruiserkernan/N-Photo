using System.Text.Json;
using System.Text.Json.Serialization;

namespace Editor.IO;

public sealed class JsonProjectDocumentStore : IProjectDocumentStore
{
    private static readonly JsonSerializerOptions LoadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions SaveOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions CanonicalOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public bool TryLoad(string path, out ProjectDocument? document, out string errorMessage)
    {
        document = null;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            errorMessage = "Path is required.";
            return false;
        }

        if (!File.Exists(path))
        {
            errorMessage = $"File not found: '{path}'.";
            return false;
        }

        try
        {
            var json = File.ReadAllText(path);
            var raw = JsonSerializer.Deserialize<ProjectDocument>(json, LoadOptions);
            if (raw is null)
            {
                errorMessage = "Project document is empty.";
                return false;
            }

            if (!TryNormalize(raw, out var normalized, out errorMessage))
            {
                return false;
            }

            document = normalized;
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }

    public bool TrySave(ProjectDocument document, string path, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            errorMessage = "Path is required.";
            return false;
        }

        if (!TryNormalize(document, out var normalized, out errorMessage))
        {
            return false;
        }

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(normalized, SaveOptions);
            File.WriteAllText(path, json);
            return true;
        }
        catch (Exception exception)
        {
            errorMessage = exception.Message;
            return false;
        }
    }

    public string CreateCanonicalSignature(ProjectDocument document)
    {
        if (!TryNormalize(document, out var normalized, out var errorMessage))
        {
            throw new InvalidOperationException($"Cannot create canonical signature: {errorMessage}");
        }

        return JsonSerializer.Serialize(normalized, CanonicalOptions);
    }

    private static bool TryNormalize(ProjectDocument document, out ProjectDocument normalized, out string errorMessage)
    {
        normalized = null!;
        errorMessage = string.Empty;

        if (document is null)
        {
            errorMessage = "Project document is required.";
            return false;
        }

        if (document.FormatVersion != ProjectDocument.CurrentFormatVersion)
        {
            errorMessage = $"Unsupported project format version '{document.FormatVersion}'.";
            return false;
        }

        var graph = document.Graph ?? new ProjectGraph();
        if (graph.Nodes is null || graph.Nodes.Count == 0)
        {
            errorMessage = "Project graph must contain at least one node.";
            return false;
        }

        if (graph.Edges is null)
        {
            errorMessage = "Project graph edges are required.";
            return false;
        }

        var normalizedNodes = new List<ProjectNode>(graph.Nodes.Count);
        var nodeIds = new HashSet<Guid>();
        foreach (var node in graph.Nodes)
        {
            if (node.Id == Guid.Empty)
            {
                errorMessage = "Project node id cannot be empty.";
                return false;
            }

            if (!nodeIds.Add(node.Id))
            {
                errorMessage = $"Duplicate project node id '{node.Id}'.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(node.Type))
            {
                errorMessage = $"Project node '{node.Id}' has no type.";
                return false;
            }

            if (node.Parameters is null)
            {
                errorMessage = $"Project node '{node.Id}' parameters are required.";
                return false;
            }

            var parameterNames = new HashSet<string>(StringComparer.Ordinal);
            var normalizedParameters = new List<ProjectParameter>(node.Parameters.Count);
            foreach (var parameter in node.Parameters)
            {
                if (string.IsNullOrWhiteSpace(parameter.Name))
                {
                    errorMessage = $"Project node '{node.Id}' has a parameter with no name.";
                    return false;
                }

                if (!parameterNames.Add(parameter.Name))
                {
                    errorMessage = $"Project node '{node.Id}' contains duplicate parameter '{parameter.Name}'.";
                    return false;
                }

                if (!ProjectParameterValueCodec.TryToDomain(parameter.Value, out var domainValue, out var valueError))
                {
                    errorMessage = $"Project node '{node.Id}' parameter '{parameter.Name}' is invalid: {valueError}";
                    return false;
                }

                normalizedParameters.Add(new ProjectParameter
                {
                    Name = parameter.Name,
                    Value = ProjectParameterValueCodec.FromDomain(domainValue)
                });
            }

            normalizedNodes.Add(new ProjectNode
            {
                Id = node.Id,
                Type = node.Type,
                Parameters = normalizedParameters
                    .OrderBy(parameter => parameter.Name, StringComparer.Ordinal)
                    .ToArray()
            });
        }

        if (graph.InputNodeId == Guid.Empty || graph.OutputNodeId == Guid.Empty)
        {
            errorMessage = "Input/output node ids are required.";
            return false;
        }

        if (!nodeIds.Contains(graph.InputNodeId))
        {
            errorMessage = $"Input node '{graph.InputNodeId}' does not exist in project nodes.";
            return false;
        }

        if (!nodeIds.Contains(graph.OutputNodeId))
        {
            errorMessage = $"Output node '{graph.OutputNodeId}' does not exist in project nodes.";
            return false;
        }

        var normalizedEdges = new List<ProjectEdge>(graph.Edges.Count);
        foreach (var edge in graph.Edges)
        {
            if (!nodeIds.Contains(edge.FromNodeId) || !nodeIds.Contains(edge.ToNodeId))
            {
                errorMessage = "Project edge references node ids that do not exist.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(edge.FromPort) || string.IsNullOrWhiteSpace(edge.ToPort))
            {
                errorMessage = "Project edge ports must be provided.";
                return false;
            }

            normalizedEdges.Add(new ProjectEdge
            {
                FromNodeId = edge.FromNodeId,
                FromPort = edge.FromPort,
                ToNodeId = edge.ToNodeId,
                ToPort = edge.ToPort
            });
        }

        var ui = document.Ui ?? new ProjectUiState();
        if (ui.NodePositions is null || ui.PreviewSlots is null)
        {
            errorMessage = "Project UI state is malformed.";
            return false;
        }

        if (ui.SelectedNodeId.HasValue && !nodeIds.Contains(ui.SelectedNodeId.Value))
        {
            errorMessage = $"Selected node '{ui.SelectedNodeId.Value}' does not exist.";
            return false;
        }

        var normalizedNodePositions = new List<ProjectNodePosition>(ui.NodePositions.Count);
        var positionedNodes = new HashSet<Guid>();
        foreach (var position in ui.NodePositions)
        {
            if (!nodeIds.Contains(position.NodeId))
            {
                errorMessage = $"Node position references unknown node '{position.NodeId}'.";
                return false;
            }

            if (!positionedNodes.Add(position.NodeId))
            {
                errorMessage = $"Duplicate node position for '{position.NodeId}'.";
                return false;
            }

            normalizedNodePositions.Add(new ProjectNodePosition
            {
                NodeId = position.NodeId,
                X = position.X,
                Y = position.Y
            });
        }

        var normalizedPreviewSlots = new List<ProjectPreviewSlotBinding>(ui.PreviewSlots.Count);
        var usedSlots = new HashSet<int>();
        foreach (var slot in ui.PreviewSlots)
        {
            if (slot.Slot <= 0)
            {
                errorMessage = "Preview slots must be positive integers.";
                return false;
            }

            if (!nodeIds.Contains(slot.NodeId))
            {
                errorMessage = $"Preview slot {slot.Slot} references unknown node '{slot.NodeId}'.";
                return false;
            }

            if (!usedSlots.Add(slot.Slot))
            {
                errorMessage = $"Preview slot '{slot.Slot}' is assigned multiple times.";
                return false;
            }

            normalizedPreviewSlots.Add(new ProjectPreviewSlotBinding
            {
                Slot = slot.Slot,
                NodeId = slot.NodeId
            });
        }

        if (ui.ActivePreviewSlot.HasValue && !usedSlots.Contains(ui.ActivePreviewSlot.Value))
        {
            errorMessage = $"Active preview slot '{ui.ActivePreviewSlot.Value}' is not assigned.";
            return false;
        }

        var assets = document.Assets ?? new ProjectAssets();
        if (assets.ImageInputs is null)
        {
            errorMessage = "Project assets are malformed.";
            return false;
        }

        var normalizedImageInputs = new List<ProjectImageBinding>(assets.ImageInputs.Count);
        var imageBindingNodes = new HashSet<Guid>();
        foreach (var binding in assets.ImageInputs)
        {
            if (!nodeIds.Contains(binding.NodeId))
            {
                errorMessage = $"Image binding references unknown node '{binding.NodeId}'.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(binding.Path))
            {
                errorMessage = $"Image binding for node '{binding.NodeId}' has no path.";
                return false;
            }

            if (!imageBindingNodes.Add(binding.NodeId))
            {
                errorMessage = $"Duplicate image binding for node '{binding.NodeId}'.";
                return false;
            }

            normalizedImageInputs.Add(new ProjectImageBinding
            {
                NodeId = binding.NodeId,
                Path = binding.Path
            });
        }

        normalized = new ProjectDocument
        {
            FormatVersion = ProjectDocument.CurrentFormatVersion,
            Graph = new ProjectGraph
            {
                InputNodeId = graph.InputNodeId,
                OutputNodeId = graph.OutputNodeId,
                Nodes = normalizedNodes
                    .OrderBy(node => node.Id)
                    .ToArray(),
                Edges = normalizedEdges
                    .OrderBy(edge => edge.FromNodeId)
                    .ThenBy(edge => edge.FromPort, StringComparer.Ordinal)
                    .ThenBy(edge => edge.ToNodeId)
                    .ThenBy(edge => edge.ToPort, StringComparer.Ordinal)
                    .ToArray()
            },
            Ui = new ProjectUiState
            {
                NodePositions = normalizedNodePositions
                    .OrderBy(position => position.NodeId)
                    .ToArray(),
                SelectedNodeId = ui.SelectedNodeId,
                PreviewSlots = normalizedPreviewSlots
                    .OrderBy(slot => slot.Slot)
                    .ToArray(),
                ActivePreviewSlot = ui.ActivePreviewSlot
            },
            Assets = new ProjectAssets
            {
                ImageInputs = normalizedImageInputs
                    .OrderBy(binding => binding.NodeId)
                    .ToArray()
            }
        };

        return true;
    }
}
