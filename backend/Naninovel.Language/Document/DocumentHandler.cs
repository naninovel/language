using System.Collections.Generic;

namespace Naninovel.Language;

public class DocumentHandler : IDocumentHandler
{
    private readonly IDocumentRegistry registry;

    public DocumentHandler (IDocumentRegistry registry)
    {
        this.registry = registry;
    }

    public void OpenDocument (IReadOnlyList<DocumentInfo> docs)
    {
        registry.Upsert(docs);
    }

    public void CloseDocument (string uri)
    {
        registry.Remove(uri);
    }

    public void ChangeDocument (string uri, IReadOnlyList<DocumentChange> changes)
    {
        registry.Change(uri, changes);
    }
}
