namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_semanticTokens

public interface ITokenHandler
{
    TokenLegend GetTokenLegend ();
    Tokens GetAllTokens (string documentUri);
    Tokens GetTokens (string documentUri, Range range);
}
