using System.Collections.Generic;

namespace Naninovel.Language;

public class DocumentHandler : IDocumentHandler
{
    private readonly DocumentFactory factory = new();
    private readonly IDocumentRegistry registry;

    public DocumentHandler (IDocumentRegistry registry)
    {
        this.registry = registry;
    }

    public void UpsertDocuments (IReadOnlyList<DocumentInfo> docs)
    {
        foreach (var doc in docs)
            registry.Upsert(doc.Uri, factory.CreateDocument(doc.Text));
    }

    public void RemoveDocument (string uri)
    {
        registry.Remove(uri);
    }

    public void ChangeDocument (string uri, IReadOnlyList<DocumentChange> changes)
    {
        registry.Change(uri, changes);
    }
}
