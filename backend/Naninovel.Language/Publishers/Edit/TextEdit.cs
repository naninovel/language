namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textEdit

public readonly record struct TextEdit (
    Range Range,
    string NewText,
    string? AnnotationId = null
);
