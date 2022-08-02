namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#diagnostic

public record Diagnostic(Range Range, DiagnosticSeverity Severity, string Message);
