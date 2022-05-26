using Naninovel.Parsing;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/specification-3-17/#position

public record Position(int Line, int Character)
{
    public static Position Empty { get; } = new(0, 0);

    public bool IsCursorOver (LineContent content)
    {
        return Character >= content.StartIndex &&
               Character <= content.EndIndex + 1;
    }

    public char GetCharBehindCursor (string lineText)
    {
        if (Character <= 0 || Character > lineText.Length) return default;
        return lineText[Character - 1];
    }
}
