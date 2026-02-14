using Editor.Domain.Graph;
using Editor.Engine;
using Editor.IO;

namespace Editor.Tests;

public class BootstrapSmokeTests
{
    [Fact]
    public void DomainAssembly_DoesNotReferenceAvaloniaOrSkia()
    {
        var references = typeof(NodeGraph)
            .Assembly
            .GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(references, name => name.StartsWith("Avalonia", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(references, name => name.StartsWith("Skia", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BootstrapContracts_AreCallable()
    {
        IEditorEngine engine = new BootstrapEditorEngine();
        IImageLoader loader = new StubImageLoader();
        IImageExporter exporter = new StubImageExporter();

        engine.RequestPreviewRender();

        Assert.Equal("Preview requested", engine.Status);
        Assert.True(loader.TryLoad("input.jpg"));
        Assert.True(exporter.TryExport("output.jpg"));
    }
}
