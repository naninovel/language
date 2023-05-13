using System.Collections.Generic;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_synchronization

public class DocumentHandler
{
    private readonly IDocumentRegistry registry;

    public DocumentHandler (IDocumentRegistry registry)
    {
        this.registry = registry;
    }

    public void Open (IReadOnlyList<DocumentInfo> docs)
    {
        registry.Upsert(docs);
    }

    public void Close (string uri)
    {
        registry.Remove(uri);
    }

    public void Change (string uri, IReadOnlyList<DocumentChange> changes)
    {
        registry.Change(uri, changes);
    }
}
