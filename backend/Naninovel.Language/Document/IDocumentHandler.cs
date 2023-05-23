using System.Collections.Generic;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_synchronization
// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#workspaceFeatures

public interface IDocumentHandler
{
    void UpsertDocuments (IReadOnlyList<DocumentInfo> docs);
    void RenameDocuments (IReadOnlyList<DocumentRenameInfo> docs);
    void DeleteDocuments (IReadOnlyList<DocumentDeleteInfo> docs);
    void ChangeDocument (string uri, IReadOnlyList<DocumentChange> changes);
}
