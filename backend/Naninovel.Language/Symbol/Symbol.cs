namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#documentSymbol

public readonly record struct Symbol
{
    public required string Name { get; init; }
    public string? Detail { get; init; }
    public required int Kind { get; init; }
    public IReadOnlyList<SymbolTag>? Tags { get; init; }
    public required Range Range { get; init; }
    public required Range SelectionRange { get; init; }
    public IReadOnlyList<Symbol>? Children { get; init; }
}
