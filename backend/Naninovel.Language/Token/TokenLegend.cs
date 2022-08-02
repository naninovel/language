using System;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#semanticTokensLegend

public record TokenLegend
{
    public string[] TokenTypes { get; set; } = Array.Empty<string>();
    public string[] TokenModifiers { get; set; } = Array.Empty<string>();
}
