using Editor.Domain.Imaging;

namespace Editor.IO;

public interface IImageLoader
{
    bool TryLoad(string path, out RgbaImage? image, out string errorMessage);
}
