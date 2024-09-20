namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_synchronization
// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#workspaceFeatures

public interface IDocumentHandler
{
    void UpsertDocuments (IReadOnlyList<DocumentInfo> infos);
    void RenameDocuments (IReadOnlyList<DocumentRenameInfo> infos);
    void DeleteDocuments (IReadOnlyList<DocumentDeleteInfo> infos);
    void ChangeDocument (string uri, IReadOnlyList<DocumentChange> changes);
}
