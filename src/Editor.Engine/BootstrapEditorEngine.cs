namespace Editor.Engine;

public sealed class BootstrapEditorEngine : IEditorEngine
{
    public string Status { get; private set; } = "Idle";

    public void RequestPreviewRender()
    {
        Status = "Preview requested";
    }
}
