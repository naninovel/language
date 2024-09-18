namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#changeAnnotation

public readonly record struct EditAnnotation (
    string Label,
    string? Description = null,
    bool? NeedsConfirmation = null
);
