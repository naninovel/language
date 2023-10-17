namespace Naninovel.Language;

internal class TokenEncoder
{
    private int index, lastLine, lastChar;
    private int[] data = Array.Empty<int>();

    public int[] Encode (IReadOnlyList<Token> tokens)
    {
        ResetState(tokens.Count);
        for (index = 0; index < tokens.Count; index++)
            EncodeToken(tokens[index]);
        return data;
    }

    private void ResetState (int tokensCount)
    {
        lastLine = lastChar = -1;
        data = new int[tokensCount * 5];
    }

    private void EncodeToken (in Token token)
    {
        if (lastLine != token.LineIndex) lastChar = -1;
        WriteData(token);
        lastLine = token.LineIndex;
        lastChar = token.CharIndex;
    }

    private void WriteData (in Token token)
    {
        data[index * 5] = DeltaLine(token.LineIndex);
        data[index * 5 + 1] = DeltaChar(token.CharIndex);
        data[index * 5 + 2] = token.Length;
        data[index * 5 + 3] = (int)token.Type;
        data[index * 5 + 4] = 0;
    }

    private int DeltaLine (int idx) => lastLine < 0 ? idx : idx - lastLine;
    private int DeltaChar (int idx) => lastChar < 0 ? idx : idx - lastChar;
}
