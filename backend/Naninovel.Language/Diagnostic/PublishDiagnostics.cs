namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/specification-3-17/#publishDiagnosticsParams

public delegate void PublishDiagnostics (string documentUri, Diagnostic[] diagnostics);
