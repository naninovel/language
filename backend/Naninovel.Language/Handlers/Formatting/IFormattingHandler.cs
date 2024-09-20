namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_formatting

public interface IFormattingHandler
{
    IReadOnlyList<TextEdit>? Format (string documentUri);
}
