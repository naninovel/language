namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/specification-3-17/#hover

public record Hover(MarkupContent Contents, Range? Range)
{
    public Hover (string contents, Range? range) :
        this(new MarkupContent(contents), range) { }
}
