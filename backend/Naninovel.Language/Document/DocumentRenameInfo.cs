namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#renameFilesParams

public readonly record struct DocumentRenameInfo(
    string OldUri,
    string NewUri
);
