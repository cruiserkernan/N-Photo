using Editor.Domain.Graph;
using Editor.Engine;
using Editor.IO;
using Editor.Tests.Common;

namespace Editor.Integration.Tests;

public class LoadProcessExportIntegrationTests
{
    [Fact]
    public void LoadProcessExport_EndToEnd_WritesReadableOutput()
    {
        var engine = new BootstrapEditorEngine();
        var loader = new SkiaImageLoader();
        var exporter = new SkiaImageExporter();
        using var tempDir = new TempDirectory();

        var inputPath = tempDir.File("input.png");
        var outputPath = tempDir.File("output.jpg");

        var source = TestImageFactory.CreateGradient(32, 24);
        Assert.True(exporter.TryExport(source, inputPath, out var writeInputError), writeInputError);
        Assert.True(loader.TryLoad(inputPath, out var loaded, out var loadError), loadError);
        Assert.NotNull(loaded);

        engine.SetInputImage(loaded!);

        var transform = engine.AddNode(NodeTypes.Transform);
        var exposure = engine.AddNode(NodeTypes.ExposureContrast);
        var blur = engine.AddNode(NodeTypes.Blur);
        var sharpen = engine.AddNode(NodeTypes.Sharpen);

        engine.Connect(engine.InputNodeId, "Image", transform, "Image");
        engine.Connect(transform, "Image", exposure, "Image");
        engine.Connect(exposure, "Image", blur, "Image");
        engine.Connect(blur, "Image", sharpen, "Image");
        engine.Connect(sharpen, "Image", engine.OutputNodeId, "Image");

        engine.SetParameter(transform, "RotateDegrees", ParameterValue.Float(10.0f));
        engine.SetParameter(exposure, "Exposure", ParameterValue.Float(0.2f));
        engine.SetParameter(blur, "Radius", ParameterValue.Integer(2));
        engine.SetParameter(sharpen, "Amount", ParameterValue.Float(1.1f));

        Assert.True(engine.TryRenderOutput(out var rendered, out var renderError), renderError);
        Assert.NotNull(rendered);
        Assert.True(exporter.TryExport(rendered!, outputPath, out var exportError), exportError);
        Assert.True(File.Exists(outputPath));
        Assert.True(loader.TryLoad(outputPath, out var roundtrip, out var roundtripError), roundtripError);
        Assert.NotNull(roundtrip);
        Assert.True(roundtrip!.Width > 0);
        Assert.True(roundtrip.Height > 0);
    }
}
