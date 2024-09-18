namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#diagnostic

public readonly record struct Diagnostic (
    Range Range,
    DiagnosticSeverity Severity,
    string Message,
    IReadOnlyList<DiagnosticTag>? Tags = null
);
