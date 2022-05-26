namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/specification-3-17/#foldingRange

public record FoldingRange(int StartLine, int EndLine)
{
    public int StartLine { get; set; } = StartLine;
    public int EndLine { get; set; } = EndLine;
}
