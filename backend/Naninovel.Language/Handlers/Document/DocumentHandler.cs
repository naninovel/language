namespace Naninovel.Language;

public class DocumentHandler (IDocumentRegistry registry, IDocumentFactory factory,
    IEndpointRenamer renamer, IEditPublisher editor) : IDocumentHandler, ISettingsObserver
{
    private bool refactorFileRenames = true;

    public void HandleSettingsChanged (Settings settings)
    {
        refactorFileRenames = settings.RefactorFileRenames;
    }

    public void UpsertDocuments (IReadOnlyList<DocumentInfo> infos)
    {
        foreach (var info in infos)
            if (IsScript(info.Uri))
                registry.Upsert(Uri.UnescapeDataString(info.Uri), factory.CreateDocument(info.Text));
    }

    public void RenameDocuments (IReadOnlyList<DocumentRenameInfo> infos)
    {
        var edits = new List<(string oldUri, string newUri, WorkspaceEdit edit)>();
        foreach (var info in infos)
            if (IsScript(info.NewUri)) RenameScript(info);
            else if (IsDirectory(info.NewUri)) RenameDirectory(info);
        foreach (var (oldUri, newUri, edit) in edits)
            editor.PublishEdit($"Rename endpoints '{oldUri}' -> '{newUri}'", edit);

        void RenameScript (DocumentRenameInfo info)
        {
            var oldUri = Uri.UnescapeDataString(info.OldUri);
            var newUri = Uri.UnescapeDataString(info.NewUri);
            registry.Rename(oldUri, newUri);
            if (refactorFileRenames && renamer.RenameScript(oldUri, newUri) is { } edit)
                edits.Add((oldUri, newUri, edit));
        }

        void RenameDirectory (DocumentRenameInfo info)
        {
            var oldUri = Uri.UnescapeDataString(info.OldUri);
            var newUri = Uri.UnescapeDataString(info.NewUri);
            foreach (var uri in registry.GetAllUris().ToArray())
                if (uri.StartsWith(oldUri))
                    registry.Rename(uri, newUri + uri.GetAfterFirst(oldUri));
            if (refactorFileRenames && renamer.RenameDirectory(oldUri, newUri) is { } edit)
                edits.Add((oldUri, newUri, edit));
        }
    }

    public void DeleteDocuments (IReadOnlyList<DocumentDeleteInfo> infos)
    {
        foreach (var info in infos)
            if (IsScript(info.Uri))
                registry.Remove(Uri.UnescapeDataString(info.Uri));
    }

    public void ChangeDocument (string uri, IReadOnlyList<DocumentChange> changes)
    {
        if (IsScript(uri))
            registry.Change(Uri.UnescapeDataString(uri), changes);
    }

    private static bool IsScript (string uri)
    {
        return uri.EndsWithOrdinal(".nani");
    }

    private static bool IsDirectory (string uri)
    {
        return !uri.Contains('.');
    }
}
