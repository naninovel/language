namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#markupContentInnerDefinition

public record MarkupContent(string Value, string Kind = "markdown");
