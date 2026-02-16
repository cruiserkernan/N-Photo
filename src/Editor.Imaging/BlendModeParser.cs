using Editor.Domain.Imaging;

namespace Editor.Imaging;

public static class BlendModeParser
{
    public static BlendMode Parse(string? mode)
    {
        if (string.Equals(mode, "multiply", StringComparison.OrdinalIgnoreCase))
        {
            return BlendMode.Multiply;
        }

        if (string.Equals(mode, "screen", StringComparison.OrdinalIgnoreCase))
        {
            return BlendMode.Screen;
        }

        return BlendMode.Over;
    }
}
