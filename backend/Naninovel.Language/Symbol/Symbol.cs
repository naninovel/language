namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#documentSymbol

public record Symbol
{
    public string Name { get; init; } = "";
    public string? Detail { get; init; }
    public SymbolKind Kind { get; init; }
    public SymbolTag[]? Tags { get; init; }
    public Range Range { get; init; } = Range.Empty;
    public Range SelectionRange { get; init; } = Range.Empty;
    public Symbol[]? Children { get; init; }
}
