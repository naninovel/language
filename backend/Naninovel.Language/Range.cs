using Naninovel.Parsing;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/specification-3-17/#range

public record Range(Position Start, Position End)
{
    public static Range Empty { get; } = new(Position.Empty, Position.Empty);

    public static Range FromContent (LineContent content, int lineIndex)
    {
        var start = new Position(lineIndex, content.StartIndex);
        var end = new Position(lineIndex, content.EndIndex + 1);
        return new Range(start, end);
    }
}
