namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#deleteFilesParams

public readonly record struct DocumentDeleteInfo (
    string Uri
);
