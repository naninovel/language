namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/specification-3-17/#diagnostic

public record Diagnostic(Range Range, DiagnosticSeverity Severity, string Message);
