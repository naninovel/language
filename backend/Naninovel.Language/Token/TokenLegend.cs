using System.Collections.Generic;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#semanticTokensLegend

public readonly record struct TokenLegend(
    IReadOnlyList<string> TokenTypes,
    IReadOnlyList<string> TokenModifiers
);
