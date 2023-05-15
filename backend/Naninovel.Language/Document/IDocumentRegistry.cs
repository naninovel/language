using System.Collections.Generic;

namespace Naninovel.Language;

public interface IDocumentRegistry
{
    IReadOnlyCollection<string> GetAllUris ();
    IDocument Get (string uri);
    bool Contains (string uri, string? label = null);
    bool IsUsed (string name, string? label = null);
    void Upsert (DocumentInfo doc);
    void Remove (string uri);
    LineRange Change (string uri, IReadOnlyList<DocumentChange> changes);
}
