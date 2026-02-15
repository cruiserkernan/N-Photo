namespace App.Workspace;

internal sealed record WorkspacePanelOption(WorkspacePanelId Id, string Title)
{
    public override string ToString() => Title;
}
