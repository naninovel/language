using System;
using System.Collections.Generic;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#semanticTokensLegend

public record TokenLegend
{
    public IReadOnlyList<string> TokenTypes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> TokenModifiers { get; set; } = Array.Empty<string>();
}
