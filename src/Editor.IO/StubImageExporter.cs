namespace Editor.IO;

public sealed class StubImageExporter : IImageExporter
{
    public bool TryExport(string path)
    {
        return !string.IsNullOrWhiteSpace(path);
    }
}
