namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/specification-3-16/#textDocument_hover

public interface IHoverHandler
{
    Hover? Hover (string documentUri, Position position);
}
