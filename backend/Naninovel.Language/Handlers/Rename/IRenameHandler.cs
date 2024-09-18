namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_rename

public interface IRenameHandler
{
    Range? PrepareRename (string documentUri, Position position);
    WorkspaceEdit? Rename (string documentUri, Position position, string newName);
}
