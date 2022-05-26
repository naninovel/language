namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/specification-3-17/#markupContentInnerDefinition

public record MarkupContent(string Value, string Kind = "markdown");
