namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#hover

public readonly record struct Hover(MarkupContent Contents, Range? Range)
{
    public Hover (string contents, Range? range) :
        this(new MarkupContent(contents), range) { }
}
