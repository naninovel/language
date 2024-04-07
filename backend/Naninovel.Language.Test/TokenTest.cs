using Moq;

namespace Naninovel.Language.Test;

public class TokenTest
{
    private record Token (int Line, int Char, int Length, TokenType Type);

    private readonly Mock<IDocumentRegistry> docs = new();
    private readonly TokenHandler handler;

    public TokenTest ()
    {
        handler = new(docs.Object);
    }

    [Fact]
    public void LegendModifiersAreEmpty ()
    {
        var legend = handler.GetTokenLegend();
        Assert.Empty(legend.TokenModifiers);
    }

    [Fact]
    public void LegendTypesAreEqualToTokenEnumNames ()
    {
        var legend = handler.GetTokenLegend();
        Assert.Equal(Enum.GetNames<TokenType>(), legend.TokenTypes);
    }

    [Fact]
    public void TokenDataDecodedCorrectly ()
    {
        var tokens = DecodeTokenData(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
        Assert.Equal(new(0, 1, 2, (TokenType)3), tokens[0]);
        Assert.Equal(new(5, 6, 7, (TokenType)8), tokens[1]);
    }

    [Fact]
    public void EmptyLinesDontHaveTokens ()
    {
        Assert.Empty(GetTokens(new Range(new(0, 0), new(1, 0)), "", ""));
    }

    [Fact]
    public void EmptyCommentLineTokenizedCorrectly ()
    {
        var tokens = GetTokens("; ");
        Assert.Single(tokens);
        Assert.Equal(new(0, 0, 2, TokenType.CommentLine), tokens[0]);
    }

    [Fact]
    public void CommentLineTokenizedCorrectly ()
    {
        var tokens = GetTokens("; comment");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(new(0, 0, 2, TokenType.CommentLine), tokens[0]);
        Assert.Equal(new(0, 2, 7, TokenType.CommentText), tokens[1]);
    }

    [Fact]
    public void EmptyLabelLineTokenizedCorrectly ()
    {
        var tokens = GetTokens("# ");
        Assert.Single(tokens);
        Assert.Equal(new(0, 0, 2, TokenType.LabelLine), tokens[0]);
    }

    [Fact]
    public void LabelLineTokenizedCorrectly ()
    {
        var tokens = GetTokens("# label");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(new(0, 0, 2, TokenType.LabelLine), tokens[0]);
        Assert.Equal(new(0, 2, 5, TokenType.LabelText), tokens[1]);
    }

    [Fact]
    public void EmptyCommandLineTokenizedCorrectly ()
    {
        var tokens = GetTokens("@");
        Assert.Single(tokens);
        Assert.Equal(new(0, 0, 1, TokenType.CommandLine), tokens[0]);
    }

    [Fact]
    public void CommandLineTokenizedCorrectly ()
    {
        var tokens = GetTokens("@cmd");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(new(0, 0, 1, TokenType.CommandLine), tokens[0]);
        Assert.Equal(new(0, 1, 3, TokenType.CommandIdentifier), tokens[1]);
    }

    [Fact]
    public void NamelessParameterTokenizedCorrectly ()
    {
        var tokens = GetTokens("@cmd nameless");
        Assert.Equal(4, tokens.Count);
        Assert.Equal(new(0, 3, 1, TokenType.Command), tokens[2]);
        Assert.Equal(new(0, 1, 8, TokenType.ParameterValue), tokens[3]);
    }

    [Fact]
    public void NamelessParameterWithTextIdTokenizedCorrectly ()
    {
        var tokens = GetTokens("@cmd nameless|#id|");
        Assert.Equal(5, tokens.Count);
        Assert.Equal(new(0, 3, 1, TokenType.Command), tokens[2]);
        Assert.Equal(new(0, 1, 8, TokenType.ParameterValue), tokens[3]);
        Assert.Equal(new(0, 8, 5, TokenType.TextIdentifier), tokens[4]);
    }

    [Fact]
    public void ParameterTokenizedCorrectly ()
    {
        var tokens = GetTokens("@cmd p:v");
        Assert.Equal(6, tokens.Count);
        Assert.Equal(new(0, 1, 1, TokenType.ParameterIdentifier), tokens[3]);
        Assert.Equal(new(0, 1, 1, TokenType.Parameter), tokens[4]);
        Assert.Equal(new(0, 1, 1, TokenType.ParameterValue), tokens[5]);
    }

    [Fact]
    public void ParameterWithTextIdTokenizedCorrectly ()
    {
        var tokens = GetTokens("@cmd p:v|#id|");
        Assert.Equal(7, tokens.Count);
        Assert.Equal(new(0, 1, 1, TokenType.ParameterIdentifier), tokens[3]);
        Assert.Equal(new(0, 1, 1, TokenType.Parameter), tokens[4]);
        Assert.Equal(new(0, 1, 1, TokenType.ParameterValue), tokens[5]);
        Assert.Equal(new(0, 1, 5, TokenType.TextIdentifier), tokens[6]);
    }

    [Fact]
    public void ParameterWithEmptyTextIdTokenizedCorrectly ()
    {
        var tokens = GetTokens("@cmd p:v|#|");
        Assert.Equal(7, tokens.Count);
        Assert.Equal(new(0, 1, 1, TokenType.ParameterIdentifier), tokens[3]);
        Assert.Equal(new(0, 1, 1, TokenType.Parameter), tokens[4]);
        Assert.Equal(new(0, 1, 1, TokenType.ParameterValue), tokens[5]);
        Assert.Equal(new(0, 1, 3, TokenType.TextIdentifier), tokens[6]);
    }

    [Fact]
    public void ExpressionInCommandTokenizedCorrectly ()
    {
        var tokens = GetTokens("@cmd p:{exp}");
        Assert.Equal(6, tokens.Count);
        Assert.Equal(new(0, 1, 5, TokenType.Expression), tokens[5]);
    }

    [Fact]
    public void ExpressionWithTextIdInCommandTokenizedCorrectly ()
    {
        var tokens = GetTokens("@cmd p:x|#x|{exp}x|#x|");
        Assert.Equal(10, tokens.Count);
        Assert.Equal(new(0, 1, 1, TokenType.ParameterValue), tokens[5]);
        Assert.Equal(new(0, 1, 4, TokenType.TextIdentifier), tokens[6]);
        Assert.Equal(new(0, 4, 5, TokenType.Expression), tokens[7]);
        Assert.Equal(new(0, 5, 1, TokenType.ParameterValue), tokens[8]);
        Assert.Equal(new(0, 1, 4, TokenType.TextIdentifier), tokens[9]);
    }

    [Fact]
    public void PositiveBoolFlagInCommandLineTokenizedCorrectly ()
    {
        var tokens = GetTokens("@c p!");
        Assert.Equal(5, tokens.Count);
        Assert.Equal(new(0, 1, 1, TokenType.ParameterIdentifier), tokens[3]);
        Assert.Equal(new(0, 1, 1, TokenType.ParameterValue), tokens[4]);
    }

    [Fact]
    public void NegativeBoolFlagInCommandLineTokenizedCorrectly ()
    {
        var tokens = GetTokens("@c !p");
        Assert.Equal(5, tokens.Count);
        Assert.Equal(new(0, 1, 1, TokenType.ParameterValue), tokens[3]);
        Assert.Equal(new(0, 1, 1, TokenType.ParameterIdentifier), tokens[4]);
    }

    [Fact]
    public void GenericLineTokenizedCorrectly ()
    {
        var tokens = GetTokens("hello");
        Assert.Single(tokens);
        Assert.Equal(new(0, 0, 5, TokenType.GenericTextLine), tokens[0]);
    }

    [Fact]
    public void GenericAuthorTokenizedCorrectly ()
    {
        var tokens = GetTokens("author: hello");
        Assert.Equal(3, tokens.Count);
        Assert.Equal(new(0, 0, 6, TokenType.GenericTextAuthor), tokens[0]);
        Assert.Equal(new(0, 6, 2, TokenType.GenericTextPrefix), tokens[1]);
        Assert.Equal(new(0, 2, 5, TokenType.GenericTextLine), tokens[2]);
    }

    [Fact]
    public void GenericAppearanceTokenizedCorrectly ()
    {
        var tokens = GetTokens("author.appearance: hello");
        Assert.Equal(5, tokens.Count);
        Assert.Equal(new(0, 0, 6, TokenType.GenericTextAuthor), tokens[0]);
        Assert.Equal(new(0, 6, 1, TokenType.GenericTextPrefix), tokens[1]);
        Assert.Equal(new(0, 1, 10, TokenType.GenericTextAuthorAppearance), tokens[2]);
        Assert.Equal(new(0, 10, 2, TokenType.GenericTextPrefix), tokens[3]);
        Assert.Equal(new(0, 2, 5, TokenType.GenericTextLine), tokens[4]);
    }

    [Fact]
    public void InlinedCommandTokenizedCorrectly ()
    {
        var tokens = GetTokens("[i] text");
        Assert.Equal(4, tokens.Count);
        Assert.Equal(new(0, 0, 1, TokenType.InlinedCommand), tokens[0]);
        Assert.Equal(new(0, 1, 1, TokenType.CommandIdentifier), tokens[1]);
        Assert.Equal(new(0, 1, 1, TokenType.InlinedCommand), tokens[2]);
        Assert.Equal(new(0, 1, 5, TokenType.GenericTextLine), tokens[3]);
    }

    [Fact]
    public void ExpressionInGenericTextTokenizedCorrectly ()
    {
        var tokens = GetTokens("{exp} text");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(new(0, 0, 5, TokenType.Expression), tokens[0]);
        Assert.Equal(new(0, 5, 5, TokenType.GenericTextLine), tokens[1]);
    }

    [Fact]
    public void TextIdInGenericTextTokenizedCorrectly ()
    {
        var tokens = GetTokens("x|#id|");
        Assert.Equal(2, tokens.Count);
        Assert.Equal(new(0, 0, 1, TokenType.GenericTextLine), tokens[0]);
        Assert.Equal(new(0, 1, 5, TokenType.TextIdentifier), tokens[1]);
    }

    [Fact]
    public void WhenGettingSpecificRangeOnlyAffectedContentIsTokenized ()
    {
        var tokens = GetTokens(new Range(new(1, 2), new(2, 2)), "generic", "# label", "; comment");
        Assert.Equal(2, tokens.Count);
    }

    [Fact]
    public void WhenGettingAllTokensFromEmptyDocumentResultIsEmpty ()
    {
        docs.SetupScript("@", "");
        Assert.Empty(handler.GetAllTokens("@").Data);
    }

    [Fact]
    public void WhenGettingAllTokensAllDocumentLinesAreTokenized ()
    {
        docs.SetupScript("@", "; comment", "# label", "generic");
        var tokens = DecodeTokenData(handler.GetAllTokens("@").Data);
        Assert.Equal(5, tokens.Length);
    }

    private IReadOnlyList<Token> GetTokens (params string[] lines)
    {
        var range = new Range(new(0, 0), new(0, lines.Length));
        return GetTokens(range, lines);
    }

    private IReadOnlyList<Token> GetTokens (Range range, params string[] lines)
    {
        docs.SetupScript("@", lines);
        return DecodeTokenData(handler.GetTokens("@", range).Data);
    }

    private Token[] DecodeTokenData (IEnumerable<int> data)
    {
        // Data format as per the LSP protocol:
        //
        //    1st token,  2nd token,  ...
        // [  0,1,2,3,4,  0,1,2,3,4,  ...  ]
        //
        // 0. Line index delta over previous token's line index (actual index for the first line in document)
        // 1. Start char index delta over previous start char (actual index for the first char in line)
        // 2. Token length
        // 3. Token type enum index from the legend, zero-based
        // 4. Token modifier bit-mask from the legend (always 0 in our case)
        //
        // Note: VS Code isn't capable of `overLappingTokenSupport`, hence the ranges shouldn't overlap.

        return data.Select((v, i) => new { index = i, value = v })
            .GroupBy(x => x.index / 5).Select(x => x.Select(v => v.value).ToArray())
            .Select(d => new Token(d[0], d[1], d[2], (TokenType)d[3])).ToArray();
    }
}
