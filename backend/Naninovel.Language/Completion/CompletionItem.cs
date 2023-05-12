namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#completionItem

public readonly record struct CompletionItem
{
    public string Label { get; init; }
    public CompletionItemKind? Kind { get; init; }
    public string? Detail { get; init; }
    public MarkupContent? Documentation { get; init; }
    public string[]? CommitCharacters { get; init; }
    public string? InsertText { get; init; }
}
