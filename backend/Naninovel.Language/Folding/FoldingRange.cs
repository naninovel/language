namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#foldingRange

public record FoldingRange(int StartLine, int EndLine)
{
    public int StartLine { get; set; } = StartLine;
    public int EndLine { get; set; } = EndLine;
}
