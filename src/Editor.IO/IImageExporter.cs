using Editor.Domain.Imaging;

namespace Editor.IO;

public interface IImageExporter
{
    bool TryExport(RgbaImage image, string path, out string errorMessage);
}
