namespace Naninovel.Language;

internal readonly record struct Token(
    int LineIndex,
    int CharIndex,
    int Length,
    TokenType Type
)
{
    public int EndIndex => CharIndex + Length;
}
