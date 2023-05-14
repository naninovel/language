using System.Collections.Generic;

namespace Naninovel.Language;

public interface IDocumentRegistry
{
    IReadOnlyCollection<string> GetAllUris ();
    IDocument Get (string uri);
    bool Contains (string uri, string? label = null);
    void Upsert (IReadOnlyList<DocumentInfo> docs);
    void Remove (string uri);
    void Change (string uri, IReadOnlyList<DocumentChange> changes);
}
