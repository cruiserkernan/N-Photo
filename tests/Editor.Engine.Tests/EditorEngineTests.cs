using Editor.Domain.Graph;
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
}
