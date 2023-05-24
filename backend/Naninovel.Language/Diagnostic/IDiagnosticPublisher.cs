using System.Collections.Generic;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/specification-3-16/#textDocument_publishDiagnostics

public interface IDiagnosticPublisher
{
    void PublishDiagnostics (string documentUri, IReadOnlyList<Diagnostic> diagnostics);
}
