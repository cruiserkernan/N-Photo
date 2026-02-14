using Editor.IO;
using Editor.Tests.Common;

namespace Editor.IO.Tests;

public class SkiaImageIoTests
{
    [Fact]
    public void ExportThenLoad_RoundTripsPng()
    {
        var loader = new SkiaImageLoader();
        var exporter = new SkiaImageExporter();
        var source = TestImageFactory.CreateGradient(8, 8);

        using var tempDir = new TempDirectory();
        var path = tempDir.File("roundtrip.png");

        Assert.True(exporter.TryExport(source, path, out var exportError), exportError);
        Assert.True(loader.TryLoad(path, out var loaded, out var loadError), loadError);
        Assert.NotNull(loaded);
        Assert.Equal(8, loaded!.Width);
        Assert.Equal(8, loaded.Height);
    }
}
