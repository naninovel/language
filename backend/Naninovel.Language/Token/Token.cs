namespace Naninovel.Language;

internal readonly struct Token
{
    public int LineIndex { get; }
    public int CharIndex { get; }
    public int Length { get; }
    public TokenType Type { get; }
    public int EndIndex => CharIndex + Length;

    public Token (int lineIndex, int charIndex, int length, TokenType type)
    {
        LineIndex = lineIndex;
        CharIndex = charIndex;
        Length = length;
        Type = type;
    }
}
