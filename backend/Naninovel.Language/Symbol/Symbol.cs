using System.Collections.Generic;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#documentSymbol

public readonly record struct Symbol(
    string Name,
    string? Detail,
    SymbolKind Kind,
    IReadOnlyList<SymbolTag>? Tags,
    Range Range,
    Range SelectionRange,
    IReadOnlyList<Symbol>? Children
);
