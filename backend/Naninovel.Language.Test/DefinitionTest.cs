using Moq;
using Naninovel.Parsing;
using Xunit;

namespace Naninovel.Language.Test;

public class DefinitionTest
{
    private readonly Mock<IEndpointResolver> resolver = new();
    private readonly DocumentRegistry registry = new();
    private readonly DefinitionHandler handler;

    public DefinitionTest ()
    {
        handler = new(registry, resolver.Object);
    }

    [Fact]
    public void ReturnsNullWhenNoEndpointResolved ()
    {
        SetupScript("/foo.nani",
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
        string script = "foo", label = null;
        resolver.Setup(r => r.TryResolve(It.Is<Command>(c => c.Identifier == "goto"), out script, out label)).Returns(true);
        SetupScript("/foo.nani",
            "text",
            "more text",
            "# start",
            "@goto foo"
        );
        Assert.Equal(new(null, "/foo.nani", new(new(0, 0), new(1, 9)), new(new(0, 0), new(0, 4))), handler.GotoDefinition("/foo.nani", new(3, 7))![0]);
    }

    [Fact]
    public void CanNavigateToLabel ()
    {
        string script = null, label = "start";
        resolver.Setup(r => r.TryResolve(It.Is<Command>(c => c.Identifier == "goto"), out script, out label)).Returns(true);
        SetupScript("/foo.nani",
            "text",
            "# start",
            "@goto .start"
        );
        Assert.Equal(new(null, "/foo.nani", new(new(1, 0), new(2, 12)), new(new(1, 0), new(1, 7))), handler.GotoDefinition("/foo.nani", new(2, 8))![0]);
    }

    [Fact]
    public void WhenCantFindDocumentReturnsNull ()
    {
        string script = "bar", label = null;
        resolver.Setup(r => r.TryResolve(It.Is<Command>(c => c.Identifier == "goto"), out script, out label)).Returns(true);
        SetupScript("/foo.nani", "@goto bar");
        Assert.Null(handler.GotoDefinition("/foo.nani", new(0, 7)));
    }

    [Fact]
    public void WhenCantFindLabelNavigatesToFirstLine ()
    {
        string script = "foo", label = "bar";
        resolver.Setup(r => r.TryResolve(It.Is<Command>(c => c.Identifier == "goto"), out script, out label)).Returns(true);
        SetupScript("/foo.nani",
            "text",
            "# start",
            "@goto foo.start"
        );
        Assert.Equal(new(null, "/foo.nani", new(new(0, 0), new(0, 4)), new(new(0, 0), new(0, 4))), handler.GotoDefinition("/foo.nani", new(2, 8))![0]);
    }

    [Fact]
    public void WhenNoNextLabelsSelectsWholeScript ()
    {
        string script = "foo", label = null;
        resolver.Setup(r => r.TryResolve(It.Is<Command>(c => c.Identifier == "goto"), out script, out label)).Returns(true);
        SetupScript("/foo.nani",
            "text",
            "more text",
            "@goto foo"
        );
        Assert.Equal(new(null, "/foo.nani", new(new(0, 0), new(2, 9)), new(new(0, 0), new(0, 4))), handler.GotoDefinition("/foo.nani", new(2, 8))![0]);
    }

    private void SetupScript (string uri, params string[] lines)
    {
        var text = string.Join('\n', lines);
        new DocumentHandler(registry, new MockDiagnoser()).Open(uri, text);
    }
}
