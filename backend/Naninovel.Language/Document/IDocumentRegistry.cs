using System.Collections.Generic;

namespace Naninovel.Language;

public interface IDocumentRegistry
{
    IReadOnlyCollection<string> GetAllUris ();
    IDocument Get (string uri);
    bool Contains (string uri);
    void Upsert (string uri, Document document);
    void Remove (string uri);
    void Change (string uri, IReadOnlyList<DocumentChange> changes);
}
