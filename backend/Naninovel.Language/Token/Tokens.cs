namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#semanticTokens

public readonly record struct Tokens(
    IReadOnlyList<int> Data
);
