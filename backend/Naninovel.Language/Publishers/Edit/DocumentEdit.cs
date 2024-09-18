namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocumentEdit

/// <param name="TextDocument">Document URI.</param>
public readonly record struct DocumentEdit (
    string TextDocument,
    IReadOnlyList<TextEdit> Edits
);
