namespace Editor.IO;

public interface IProjectDocumentStore
{
    bool TryLoad(string path, out ProjectDocument? document, out string errorMessage);

    bool TrySave(ProjectDocument document, string path, out string errorMessage);

    string CreateCanonicalSignature(ProjectDocument document);
}
