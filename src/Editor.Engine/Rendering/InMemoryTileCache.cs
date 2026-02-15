using Editor.Domain.Imaging;
using Editor.Engine.Abstractions.Rendering;

namespace Editor.Engine.Rendering;

public sealed class InMemoryTileCache : ITileCache
{
    private readonly Dictionary<TileKey, RgbaImage> _cache = new();

    public bool TryGet(TileKey key, out RgbaImage image)
    {
        if (_cache.TryGetValue(key, out var stored))
        {
            image = stored.Clone();
            return true;
        }

        image = null!;
        return false;
    }

    public void Set(TileKey key, RgbaImage image)
    {
        _cache[key] = image.Clone();
    }

    public void Clear()
    {
        _cache.Clear();
    }
}
