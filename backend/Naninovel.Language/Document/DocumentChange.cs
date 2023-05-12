namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocumentContentChangeEvent

public readonly record struct DocumentChange(
    Range Range,
    string Text
);
