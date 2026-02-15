using Editor.Domain.Graph;
using Editor.Domain.Imaging;
using Editor.Engine;
using Editor.Tests.Common;

namespace Editor.Engine.Tests;

public class EditorEngineTests
{
    [Fact]
    public void Commands_AddConnectSetParam_SupportUndoRedo()
    {
        var engine = new BootstrapEditorEngine();
        var transformId = engine.AddNode(NodeTypes.Transform);
        engine.Connect(engine.InputNodeId, "Image", transformId, "Image");
        engine.Connect(transformId, "Image", engine.OutputNodeId, "Image");
        engine.SetParameter(transformId, "Scale", ParameterValue.Float(1.5f));

        var transformNode = engine.Nodes.Single(node => node.Id == transformId);
        Assert.Equal(1.5f, transformNode.GetParameter("Scale").AsFloat());
        Assert.True(engine.CanUndo);

        engine.Undo();
        transformNode = engine.Nodes.Single(node => node.Id == transformId);
        Assert.Equal(1.0f, transformNode.GetParameter("Scale").AsFloat());

        engine.Redo();
        transformNode = engine.Nodes.Single(node => node.Id == transformId);
        Assert.Equal(1.5f, transformNode.GetParameter("Scale").AsFloat());
    }

    [Fact]
    public void Render_IsDeterministic_ForSameGraphAndInput()
    {
        var engine = new BootstrapEditorEngine();
        var transform = engine.AddNode(NodeTypes.Transform);
        var exposure = engine.AddNode(NodeTypes.ExposureContrast);
        var blur = engine.AddNode(NodeTypes.Blur);
        var sharpen = engine.AddNode(NodeTypes.Sharpen);

        engine.Connect(engine.InputNodeId, "Image", transform, "Image");
        engine.Connect(transform, "Image", exposure, "Image");
        engine.Connect(exposure, "Image", blur, "Image");
        engine.Connect(blur, "Image", sharpen, "Image");
        engine.Connect(sharpen, "Image", engine.OutputNodeId, "Image");

        engine.SetParameter(transform, "Scale", ParameterValue.Float(1.15f));
        engine.SetParameter(exposure, "Exposure", ParameterValue.Float(0.3f));
        engine.SetParameter(blur, "Radius", ParameterValue.Integer(2));
        engine.SetParameter(sharpen, "Amount", ParameterValue.Float(1.2f));

        var input = TestImageFactory.CreateGradient();
        engine.SetInputImage(input);

        Assert.True(engine.TryRenderOutput(out var first, out var firstError), firstError);
        Assert.True(engine.TryRenderOutput(out var second, out var secondError), secondError);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.ToRgba8(), second!.ToRgba8());
    }

    [Fact]
    public void TryRenderOutput_TargetNode_RendersIntermediateNode()
    {
        var engine = new BootstrapEditorEngine();
        var transform = engine.AddNode(NodeTypes.Transform);
        var exposure = engine.AddNode(NodeTypes.ExposureContrast);

        engine.Connect(engine.InputNodeId, "Image", transform, "Image");
        engine.Connect(transform, "Image", exposure, "Image");
        engine.Connect(exposure, "Image", engine.OutputNodeId, "Image");
        engine.SetParameter(exposure, "Exposure", ParameterValue.Float(0.4f));

        engine.SetInputImage(TestImageFactory.CreateGradient());

        Assert.True(engine.TryRenderOutput(out var transformPreview, out var transformError, transform), transformError);
        Assert.True(engine.TryRenderOutput(out var outputPreview, out var outputError), outputError);
        Assert.NotNull(transformPreview);
        Assert.NotNull(outputPreview);
        Assert.NotEqual(transformPreview!.ToRgba8(), outputPreview!.ToRgba8());
    }

    [Fact]
    public void TryRenderOutput_TargetNode_ReturnsNull_WhenNodeIsDisconnected()
    {
        var engine = new BootstrapEditorEngine();
        var disconnected = engine.AddNode(NodeTypes.Transform);
        engine.SetInputImage(TestImageFactory.CreateGradient());

        Assert.False(engine.TryRenderOutput(out var image, out var errorMessage, disconnected));
        Assert.Null(image);
        Assert.Equal(string.Empty, errorMessage);
    }

    [Fact]
    public void SetInputImage_NodeSpecificImages_RenderPerImageInputNode()
    {
        var engine = new BootstrapEditorEngine();
        var secondInput = engine.AddNode(NodeTypes.ImageInput);

        engine.Connect(secondInput, "Image", engine.OutputNodeId, "Image");

        var firstImage = TestImageFactory.CreateGradient(8, 8);
        var secondImage = CreateSolidImage(8, 8, new RgbaColor(1.0f, 0.0f, 0.0f, 1.0f));

        engine.SetInputImage(firstImage);
        engine.SetInputImage(secondInput, secondImage);

        Assert.True(engine.TryRenderOutput(out var firstPreview, out var firstError, engine.InputNodeId), firstError);
        Assert.True(engine.TryRenderOutput(out var secondPreview, out var secondError, secondInput), secondError);
        Assert.True(engine.TryRenderOutput(out var outputPreview, out var outputError), outputError);

        Assert.NotNull(firstPreview);
        Assert.NotNull(secondPreview);
        Assert.NotNull(outputPreview);
        Assert.Equal(firstImage.ToRgba8(), firstPreview!.ToRgba8());
        Assert.Equal(secondImage.ToRgba8(), secondPreview!.ToRgba8());
        Assert.Equal(secondImage.ToRgba8(), outputPreview!.ToRgba8());
    }

    [Fact]
    public void SetInputImage_Throws_WhenNodeIsNotImageInput()
    {
        var engine = new BootstrapEditorEngine();
        var transform = engine.AddNode(NodeTypes.Transform);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            engine.SetInputImage(transform, TestImageFactory.CreateGradient()));

        Assert.Contains(NodeTypes.Transform, exception.Message);
    }

    [Fact]
    public void MaskInput_PortInfluencesNodeEvaluation()
    {
        var engine = new BootstrapEditorEngine();
        var exposure = engine.AddNode(NodeTypes.ExposureContrast);
        var maskInput = engine.AddNode(NodeTypes.ImageInput);

        engine.Connect(engine.InputNodeId, "Image", exposure, "Image");
        engine.Connect(maskInput, "Image", exposure, NodePortNames.Mask);
        engine.Connect(exposure, "Image", engine.OutputNodeId, "Image");
        engine.SetParameter(exposure, "Exposure", ParameterValue.Float(1.0f));

        engine.SetInputImage(CreateSolidImage(1, 1, new RgbaColor(0.2f, 0.2f, 0.2f, 1.0f)));
        engine.SetInputImage(maskInput, CreateSolidImage(1, 1, new RgbaColor(0.0f, 0.0f, 0.0f, 0.5f)));

        Assert.True(engine.TryRenderOutput(out var image, out var error), error);
        Assert.NotNull(image);

        var pixel = image!.GetPixel(0, 0);
        Assert.InRange(pixel.R, 0.29f, 0.31f);
        Assert.InRange(pixel.G, 0.29f, 0.31f);
        Assert.InRange(pixel.B, 0.29f, 0.31f);
    }

    private static RgbaImage CreateSolidImage(int width, int height, RgbaColor color)
    {
        var image = new RgbaImage(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                image.SetPixel(x, y, color);
            }
        }

        return image;
    }
}
