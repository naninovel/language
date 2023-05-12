using Xunit;

namespace Naninovel.Language.Test;

public class FoldingTest
{
    [Fact]
    public void WhenEmptyDocumentResultIsEmpty ()
    {
        Assert.Empty(GetRanges(""));
    }

    [Fact]
    public void CommandLineIsFolded ()
    {
        var ranges = GetRanges("@command");
        Assert.Single(ranges);
        Assert.Equal(new FoldingRange(0, 0), ranges[0]);
    }

    [Fact]
    public void CommentLineIsFolded ()
    {
        var ranges = GetRanges("; comment");
        Assert.Single(ranges);
        Assert.Equal(new FoldingRange(0, 0), ranges[0]);
    }

    [Fact]
    public void OtherLinesAreNotFolded ()
    {
        Assert.Empty(GetRanges("generic\n# label\n\n"));
    }

    [Fact]
    public void FoldingRangesAreCorrect ()
    {
        var ranges = GetRanges(";\n@c\n#l\n\n;");
        Assert.Equal(2, ranges.Length);
        Assert.Equal(new FoldingRange(0, 1), ranges[0]);
        Assert.Equal(new FoldingRange(4, 4), ranges[1]);
    }

    private FoldingRange[] GetRanges (string documentText)
    {
        var registry = new DocumentRegistry(new());
        var handler = new FoldingHandler(registry);
        new DocumentHandler(registry, new MockDiagnoser()).Open(new("@", documentText));
        return handler.GetFoldingRanges("@");
    }
}
