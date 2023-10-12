using System.Collections.Generic;

namespace Naninovel.Language;

public class DocumentHandler(IDocumentRegistry registry) : IDocumentHandler
{
    private readonly DocumentFactory factory = new();

    public void UpsertDocuments (IReadOnlyList<DocumentInfo> docs)
    {
        foreach (var doc in docs)
            registry.Upsert(doc.Uri, factory.CreateDocument(doc.Text));
    }

    public void RenameDocuments (IReadOnlyList<DocumentRenameInfo> docs)
    {
        foreach (var doc in docs)
            registry.Rename(doc.OldUri, doc.NewUri);
    }

    public void DeleteDocuments (IReadOnlyList<DocumentDeleteInfo> docs)
    {
        foreach (var doc in docs)
            registry.Remove(doc.Uri);
    }

    public void ChangeDocument (string uri, IReadOnlyList<DocumentChange> changes)
    {
        registry.Change(uri, changes);
    }
}
