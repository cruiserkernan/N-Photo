using Editor.Domain.Imaging;

namespace Editor.Engine.Abstractions.Rendering;

public interface ITileCache
{
    bool TryGet(TileKey key, out RgbaImage image);

    void Set(TileKey key, RgbaImage image);

    void Clear();
}
