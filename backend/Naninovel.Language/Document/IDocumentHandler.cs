using System.Collections.Generic;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_synchronization

public interface IDocumentHandler
{
    void UpsertDocuments (IReadOnlyList<DocumentInfo> docs);
    void RemoveDocument (string uri);
    void ChangeDocument (string uri, IReadOnlyList<DocumentChange> changes);
}
