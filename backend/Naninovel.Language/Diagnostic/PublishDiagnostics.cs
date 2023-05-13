using System.Collections.Generic;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#publishDiagnosticsParams

public delegate void PublishDiagnostics (string documentUri, IReadOnlyList<Diagnostic> diagnostics);
