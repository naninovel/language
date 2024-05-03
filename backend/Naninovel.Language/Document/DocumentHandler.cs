namespace Naninovel.Language;

public class DocumentHandler (IDocumentRegistry registry, IDocumentFactory factory) : IDocumentHandler
{
    public void UpsertDocuments (IReadOnlyList<DocumentInfo> docs)
    {
        foreach (var doc in docs)
            registry.Upsert(Uri.UnescapeDataString(doc.Uri), factory.CreateDocument(doc.Text));
    }

    public void RenameDocuments (IReadOnlyList<DocumentRenameInfo> docs)
    {
        foreach (var doc in docs)
            registry.Rename(Uri.UnescapeDataString(doc.OldUri), Uri.UnescapeDataString(doc.NewUri));
    }

    public void DeleteDocuments (IReadOnlyList<DocumentDeleteInfo> docs)
    {
        foreach (var doc in docs)
            registry.Remove(Uri.UnescapeDataString(doc.Uri));
    }

    public void ChangeDocument (string uri, IReadOnlyList<DocumentChange> changes)
    {
        registry.Change(Uri.UnescapeDataString(uri), changes);
    }
}
