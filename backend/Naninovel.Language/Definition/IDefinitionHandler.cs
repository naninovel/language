namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_definition

public interface IDefinitionHandler
{
    IReadOnlyList<LocationLink>? GotoDefinition (string documentUri, Position position);
}
