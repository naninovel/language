using System;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/specification-3-17/#semanticTokensLegend

public record TokenLegend
{
    public string[] TokenTypes { get; set; } = Array.Empty<string>();
    public string[] TokenModifiers { get; set; } = Array.Empty<string>();
}
