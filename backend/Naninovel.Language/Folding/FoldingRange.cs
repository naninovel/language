namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#foldingRange

public readonly record struct FoldingRange(
    int StartLine,
    int EndLine
);
