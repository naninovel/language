namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#markupContentInnerDefinition

public readonly record struct MarkupContent(
    string Value,
    string Kind = "markdown"
);
