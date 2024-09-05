using Moq;

namespace Naninovel.Language.Test;

public class DefinitionTest
{
    private readonly MetadataMock meta = new();
    private readonly Mock<IDocumentRegistry> docs = new();
    private readonly DefinitionHandler handler;

    public DefinitionTest ()
    {
        handler = new(meta, docs.Object);
    }

    [Fact]
    public void ReturnsNullWhenNoEndpointResolved ()
    {
        docs.SetupScript("/foo.nani",
            "# label",
            "; comment",
            "@cmd p:v",
            "generic [c]"
        );
        Assert.Null(handler.GotoDefinition("/foo.nani", new(0, 3)));
        Assert.Null(handler.GotoDefinition("/foo.nani", new(1, 3)));
        Assert.Null(handler.GotoDefinition("/foo.nani", new(2, 1)));
        Assert.Null(handler.GotoDefinition("/foo.nani", new(2, 6)));
        Assert.Null(handler.GotoDefinition("/foo.nani", new(2, 7)));
        Assert.Null(handler.GotoDefinition("/foo.nani", new(3, 0)));
        Assert.Null(handler.GotoDefinition("/foo.nani", new(3, 9)));
    }

    [Fact]
    public void CanNavigateToScript ()
    {
        meta.SetupNavigationCommands();
        docs.SetupScript("/foo.nani",
            "text",
            "more text",
            "# start",
            "@goto foo"
        );
        Assert.Equal(new(null, "/foo.nani", new(new(0, 0), new(1, 9)), new(new(0, 0), new(0, 4))), Goto("/foo.nani", new(3, 7)));
    }

    [Fact]
    public void CanNavigateToLabel ()
    {
        meta.SetupNavigationCommands();
        docs.SetupScript("/foo.nani",
            "text",
            "# start",
            "@goto .start"
        );
        Assert.Equal(new(null, "/foo.nani", new(new(1, 0), new(2, 12)), new(new(1, 0), new(1, 7))), Goto("/foo.nani", new(2, 8)));
    }

    [Fact]
    public void WhenCantFindDocumentReturnsNull ()
    {
        meta.SetupNavigationCommands();
        docs.SetupScript("/foo.nani", "@goto bar");
        Assert.Null(handler.GotoDefinition("/foo.nani", new(0, 7)));
    }

    [Fact]
    public void WhenCantFindLabelNavigatesToFirstLine ()
    {
        meta.SetupNavigationCommands();
        docs.SetupScript("/foo.nani",
            "text",
            "# foo",
            "@goto foo.bar"
        );
        Assert.Equal(new(null, "/foo.nani", new(new(0, 0), new(0, 4)), new(new(0, 0), new(0, 4))), Goto("/foo.nani", new(2, 8)));
    }

    [Fact]
    public void WhenNoNextLabelsSelectsWholeScript ()
    {
        meta.SetupNavigationCommands();
        docs.SetupScript("/foo.nani",
            "text",
            "more text",
            "@goto foo"
        );
        Assert.Equal(new(null, "/foo.nani", new(new(0, 0), new(2, 9)), new(new(0, 0), new(0, 4))), Goto("/foo.nani", new(2, 8)));
    }

    private LocationLink Goto (string documentUri, Position position)
    {
        return handler.GotoDefinition(documentUri, position)![0];
    }
}
