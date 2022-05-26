using System.Collections.Generic;

namespace Naninovel.Language;

internal class TokenBuilder
{
    private readonly List<Token> tokens = new();
    private readonly TokenEncoder encoder = new();

    public void Append (int lineIndex, int charIndex, int length, TokenType type)
    {
        var token = new Token(lineIndex, charIndex, length, type);
        var overlappingIndex = FindOverlappingIndex(token);
        if (overlappingIndex < 0) tokens.Add(token);
        else DistributeOverlapping(token, overlappingIndex);
    }

    public int[] Build () => encoder.Encode(tokens);

    public void Clear () => tokens.Clear();

    private int FindOverlappingIndex (in Token token)
    {
        var overlapping = -1;
        for (int i = tokens.Count - 1; i >= 0; i--)
            if (tokens[i].LineIndex != token.LineIndex) break;
            else if (IsOverlapping(token, tokens[i])) overlapping = i;
        return overlapping;
    }

    private static bool IsOverlapping (in Token a, in Token b)
    {
        return a.CharIndex < b.EndIndex && a.EndIndex > b.CharIndex;
    }

    private void DistributeOverlapping (in Token token, int overlappingIndex)
    {
        var overlapping = tokens[overlappingIndex];
        var prepend = ExtractTokenToPrepend(token, overlapping);
        var append = ExtractTokenToAppend(token, overlapping);
        tokens[overlappingIndex] = prepend ?? token;
        if (prepend.HasValue) tokens.Insert(overlappingIndex + 1, token);
        if (append.HasValue) tokens.Insert(prepend.HasValue ? overlappingIndex + 2 : overlappingIndex + 1, append.Value);
    }

    private static Token? ExtractTokenToPrepend (in Token token, in Token overlapping)
    {
        var length = token.CharIndex - overlapping.CharIndex;
        if (length <= 0) return null;
        return new Token(overlapping.LineIndex, overlapping.CharIndex, length, overlapping.Type);
    }

    private static Token? ExtractTokenToAppend (in Token token, in Token overlapping)
    {
        var length = overlapping.EndIndex - token.EndIndex;
        if (length <= 0) return null;
        return new Token(overlapping.LineIndex, token.EndIndex, length, overlapping.Type);
    }
}
