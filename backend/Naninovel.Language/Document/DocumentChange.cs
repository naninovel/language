namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/specification-3-17/#textDocumentContentChangeEvent

public record DocumentChange(Range Range, string Text);
