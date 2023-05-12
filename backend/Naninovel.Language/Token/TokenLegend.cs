using System;
using System.Collections.Generic;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#semanticTokensLegend

public readonly record struct TokenLegend()
{
    public IReadOnlyList<string> TokenTypes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> TokenModifiers { get; init; } = Array.Empty<string>();
}
