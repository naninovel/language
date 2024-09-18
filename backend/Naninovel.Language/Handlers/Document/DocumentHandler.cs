namespace Naninovel.Language;

public class DocumentHandler (IDocumentRegistry registry, IDocumentFactory factory,
    IEndpointRenamer renamer, IEditPublisher editor) : IDocumentHandler
{
    public void UpsertDocuments (IReadOnlyList<DocumentInfo> docs)
    {
        foreach (var doc in docs)
            if (IsScript(doc.Uri))
                registry.Upsert(Uri.UnescapeDataString(doc.Uri), factory.CreateDocument(doc.Text));
    }

    public void RenameDocuments (IReadOnlyList<DocumentRenameInfo> docs)
    {
        var edits = new List<(string oldUri, string newUri, WorkspaceEdit edit)>();
        foreach (var doc in docs)
        {
            var oldUri = Uri.UnescapeDataString(doc.OldUri);
            var newUri = Uri.UnescapeDataString(doc.NewUri);
            if (IsScript(newUri))
            {
                registry.Rename(oldUri, newUri);
                if (renamer.RenameScript(oldUri, newUri) is { } edit)
                    edits.Add((oldUri, newUri, edit));
            }
            else if (IsFolder(newUri))
            {
                foreach (var uri in registry.GetAllUris().ToArray())
                    if (uri.StartsWith(oldUri))
                        registry.Rename(uri, newUri + uri.GetAfterFirst(oldUri));
                if (renamer.RenameDirectory(oldUri, newUri) is { } edit)
                    edits.Add((oldUri, newUri, edit));
            }
        }
        foreach (var (oldUri, newUri, edit) in edits)
            editor.PublishEdit($"Rename endpoints '{oldUri}' -> '{newUri}'", edit);
    }

    public void DeleteDocuments (IReadOnlyList<DocumentDeleteInfo> docs)
    {
        foreach (var doc in docs)
            if (IsScript(doc.Uri))
                registry.Remove(Uri.UnescapeDataString(doc.Uri));
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

    private static bool IsFolder (string uri)
    {
        return !uri.Contains('.');
    }
}
