using System.Collections.Generic;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_synchronization

public interface IDocumentHandler
{
    void OpenDocument (IReadOnlyList<DocumentInfo> docs);
    void CloseDocument (string uri);
    void ChangeDocument (string uri, IReadOnlyList<DocumentChange> changes);
}
