namespace Editor.Engine;

public interface IEditorEngine
{
    string Status { get; }

    void RequestPreviewRender();
}
