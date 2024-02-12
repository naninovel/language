using Naninovel.Parsing;
using static Naninovel.Language.Test.Common;

namespace Naninovel.Language.Test;

public class DocumentFactoryTest
{
    [Fact]
    public void DocumentWithEmptyContentHasSingleEmptyLine ()
    {
        var doc = CreateDocument("");
        Assert.Equal(1, doc.LineCount);
        Assert.Empty(doc[0].Text);
    }

    [Fact]
    public void DocumentTextLinesArePreserved ()
    {
        var doc = CreateDocument("generic", "@command", "#label", ";comment");
        Assert.Equal("generic", doc[0].Text);
        Assert.Equal("@command", doc[1].Text);
        Assert.Equal("#label", doc[2].Text);
        Assert.Equal(";comment", doc[3].Text);
    }

    [Fact]
    public void DocumentTextIsParsed ()
    {
        var document = CreateDocument("generic", "@command", "#label", ";comment");
        Assert.IsType<GenericLine>(document[0].Script);
        Assert.IsType<CommandLine>(document[1].Script);
        Assert.IsType<LabelLine>(document[2].Script);
        Assert.IsType<CommentLine>(document[3].Script);
    }

    [Fact]
    public void WhenCantGetLineRangeReturnsEmpty ()
    {
        var line = new DocumentLine("", new LabelLine(""), Array.Empty<ParseError>(), new());
        Assert.Equal(new InlineRange(0, 0), line.GetLineRange(null));
        Assert.Equal(new InlineRange(0, 0), line.GetLineRange(new PlainText("")));
    }

    [Fact]
    public void WhenCantExtractTextReturnsEmpty ()
    {
        var line = new DocumentLine("", new LabelLine(""), Array.Empty<ParseError>(), new());
        Assert.Empty(line.Extract(null));
        Assert.Empty(line.Extract(new PlainText("")));
        Assert.Empty(line.Extract(new InlineRange(9, 1)));
        Assert.Empty(line.Extract(new InlineRange(0, 0)));
    }

    [Fact]
    public void WhenGettingRangeOfInvalidComponentReturnsEmpty ()
    {
        var line = new DocumentLine("", new LabelLine(""), Array.Empty<ParseError>(), new());
        Assert.Equal(Range.Empty, line.GetRange(null, 0));
    }
}
