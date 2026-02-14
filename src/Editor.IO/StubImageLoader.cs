namespace Editor.IO;

public sealed class StubImageLoader : IImageLoader
{
    public bool TryLoad(string path)
    {
        return !string.IsNullOrWhiteSpace(path);
    }
}
