using Editor.Domain.Graph;
using Editor.IO;
using Editor.Tests.Common;

namespace Editor.IO.Tests;

public sealed class ProjectDocumentStoreTests
{
    [Fact]
    public void SaveThenLoad_RoundTripsGraphUiAssetsAndTypedParameters()
    {
        var store = new JsonProjectDocumentStore();
        var inputId = Guid.NewGuid();
        var blurId = Guid.NewGuid();
        var outputId = Guid.NewGuid();
        var document = new ProjectDocument
        {
            FormatVersion = ProjectDocument.CurrentFormatVersion,
            Graph = new ProjectGraph
            {
                InputNodeId = inputId,
                OutputNodeId = outputId,
                Nodes =
                [
                    new ProjectNode
                    {
                        Id = inputId,
                        Type = NodeTypes.ImageInput,
                        Parameters = Array.Empty<ProjectParameter>()
                    },
                    new ProjectNode
                    {
                        Id = blurId,
                        Type = NodeTypes.Blur,
                        Parameters =
                        [
                            new ProjectParameter
                            {
                                Name = "Radius",
                                Value = new ProjectParameterValue
                                {
                                    Kind = nameof(ParameterValueKind.Integer),
                                    IntegerValue = 4
                                }
                            }
                        ]
                    },
                    new ProjectNode
                    {
                        Id = outputId,
                        Type = NodeTypes.Output,
                        Parameters = Array.Empty<ProjectParameter>()
                    }
                ],
                Edges =
                [
                    new ProjectEdge
                    {
                        FromNodeId = inputId,
                        FromPort = NodePortNames.Image,
                        ToNodeId = blurId,
                        ToPort = NodePortNames.Image
                    },
                    new ProjectEdge
                    {
                        FromNodeId = blurId,
                        FromPort = NodePortNames.Image,
                        ToNodeId = outputId,
                        ToPort = NodePortNames.Image
                    }
                ]
            },
            Ui = new ProjectUiState
            {
                SelectedNodeId = blurId,
                ActivePreviewSlot = 2,
                NodePositions =
                [
                    new ProjectNodePosition { NodeId = inputId, X = 10, Y = 20 },
                    new ProjectNodePosition { NodeId = blurId, X = 120, Y = 180 },
                    new ProjectNodePosition { NodeId = outputId, X = 260, Y = 180 }
                ],
                PreviewSlots =
                [
                    new ProjectPreviewSlotBinding { Slot = 2, NodeId = blurId }
                ]
            },
            Assets = new ProjectAssets
            {
                ImageInputs =
                [
                    new ProjectImageBinding { NodeId = inputId, Path = @"assets\source.png" }
                ]
            }
        };

        using var tempDir = new TempDirectory();
        var path = tempDir.File("roundtrip.nphoto");

        Assert.True(store.TrySave(document, path, out var saveError), saveError);
        Assert.True(store.TryLoad(path, out var loaded, out var loadError), loadError);
        Assert.NotNull(loaded);

        Assert.Equal(ProjectDocument.CurrentFormatVersion, loaded!.FormatVersion);
        Assert.Equal(inputId, loaded.Graph.InputNodeId);
        Assert.Equal(outputId, loaded.Graph.OutputNodeId);
        Assert.Equal(3, loaded.Graph.Nodes.Count);
        Assert.Equal(2, loaded.Graph.Edges.Count);
        Assert.Equal(3, loaded.Ui.NodePositions.Count);
        Assert.Equal(blurId, loaded.Ui.SelectedNodeId);
        Assert.Equal(2, loaded.Ui.ActivePreviewSlot);
        Assert.Single(loaded.Ui.PreviewSlots);
        Assert.Single(loaded.Assets.ImageInputs);
        Assert.Equal(@"assets\source.png", loaded.Assets.ImageInputs[0].Path);

        var blurNode = loaded.Graph.Nodes.Single(node => node.Id == blurId);
        var radiusParameter = blurNode.Parameters.Single(parameter => parameter.Name == "Radius");
        Assert.Equal(nameof(ParameterValueKind.Integer), radiusParameter.Value.Kind);
        Assert.Equal(4, radiusParameter.Value.IntegerValue);
    }

    [Fact]
    public void Load_UnsupportedFormatVersion_FailsWithClearError()
    {
        var store = new JsonProjectDocumentStore();
        using var tempDir = new TempDirectory();
        var path = tempDir.File("unsupported.nphoto");
        File.WriteAllText(path, """
            {
              "formatVersion": 999,
              "graph": {},
              "ui": {},
              "assets": {}
            }
            """);

        var success = store.TryLoad(path, out var document, out var errorMessage);

        Assert.False(success);
        Assert.Null(document);
        Assert.Contains("Unsupported project format version", errorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void SaveThenLoad_KeepsRelativeAssetPath()
    {
        var store = new JsonProjectDocumentStore();
        var inputId = Guid.NewGuid();
        var outputId = Guid.NewGuid();
        var document = new ProjectDocument
        {
            FormatVersion = ProjectDocument.CurrentFormatVersion,
            Graph = new ProjectGraph
            {
                InputNodeId = inputId,
                OutputNodeId = outputId,
                Nodes =
                [
                    new ProjectNode
                    {
                        Id = inputId,
                        Type = NodeTypes.ImageInput,
                        Parameters = Array.Empty<ProjectParameter>()
                    },
                    new ProjectNode
                    {
                        Id = outputId,
                        Type = NodeTypes.Output,
                        Parameters = Array.Empty<ProjectParameter>()
                    }
                ],
                Edges =
                [
                    new ProjectEdge
                    {
                        FromNodeId = inputId,
                        FromPort = NodePortNames.Image,
                        ToNodeId = outputId,
                        ToPort = NodePortNames.Image
                    }
                ]
            },
            Assets = new ProjectAssets
            {
                ImageInputs =
                [
                    new ProjectImageBinding
                    {
                        NodeId = inputId,
                        Path = @"textures\input.png"
                    }
                ]
            }
        };

        using var tempDir = new TempDirectory();
        var path = tempDir.File("relative.nphoto");

        Assert.True(store.TrySave(document, path, out var saveError), saveError);
        Assert.True(store.TryLoad(path, out var loaded, out var loadError), loadError);
        Assert.NotNull(loaded);
        Assert.Equal(@"textures\input.png", loaded!.Assets.ImageInputs.Single().Path);
    }
}
