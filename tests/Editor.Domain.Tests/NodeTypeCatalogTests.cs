using Editor.Domain.Graph;

namespace Editor.Domain.Tests;

public class NodeTypeCatalogTests
{
    [Fact]
    public void ElbowNodeType_HasSingleImageInputAndOutput()
    {
        var elbow = NodeTypeCatalog.GetByName(NodeTypes.Elbow);

        Assert.Single(elbow.Inputs);
        Assert.Single(elbow.Outputs);
        Assert.Equal(NodePortNames.Image, elbow.Inputs[0].Name);
        Assert.Equal(NodePortNames.Image, elbow.Outputs[0].Name);
    }

    [Theory]
    [InlineData(NodeTypes.Transform)]
    [InlineData(NodeTypes.ExposureContrast)]
    [InlineData(NodeTypes.Curves)]
    [InlineData(NodeTypes.Hsl)]
    [InlineData(NodeTypes.Blur)]
    [InlineData(NodeTypes.Sharpen)]
    [InlineData(NodeTypes.Blend)]
    public void MaskEnabledNodeTypes_ExposeMaskInput(string nodeTypeName)
    {
        var nodeType = NodeTypeCatalog.GetByName(nodeTypeName);

        Assert.Contains(
            nodeType.Inputs,
            port => port.Direction == PortDirection.Input &&
                    port.Role == NodePortRole.Mask &&
                    string.Equals(port.Name, NodePortNames.Mask, StringComparison.Ordinal));
    }
}
