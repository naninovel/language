using Moq;

namespace Naninovel.Language.Test;

public class FoldingTest
{
    private readonly Mock<IDocumentRegistry> docs = new();
    private readonly FoldingHandler handler;

    public FoldingTest ()
    {
        handler = new(docs.Object);
    }

    [Fact]
    public void WhenEmptyDocumentResultIsEmpty ()
    {
        Assert.Empty(GetRanges(""));
    }

    [Fact]
    public void CommentLineIsFolded ()
    {
        var ranges = GetRanges("; comment");
        Assert.Single(ranges);
        Assert.Equal(new FoldingRange(0, 0), ranges[0]);
    }

    [Fact]
    public void LabelLineIsFolded ()
    {
        var ranges = GetRanges("# label");
        Assert.Single(ranges);
        Assert.Equal(new FoldingRange(0, 0), ranges[0]);
    }

    [Fact]
    public void GenericLinesAreNotFolded ()
    {
        Assert.Empty(GetRanges("generic 1", "generic 2", ""));
    }

    [Fact]
    public void IntersectingRangesAreCorrect ()
    {
        Assert.Equal(
            new FoldingRange[] { new(0, 1), new(5, 5), new(3, 7), new(8, 8) },
            GetRanges(";", ";", "@c", "#l", "", ";", "@c", "@c", "#l"));
    }

    [Fact]
    public void IndentedLinesAreFolded ()
    {
        Assert.Contains(
            new FoldingRange(1, 6),
            GetRanges("x", "@c", "    x", "    ", "    ;", "    @c", "    #x", "x"));
    }

    private IReadOnlyList<FoldingRange> GetRanges (params string[] lines)
    {
        docs.SetupScript("@", lines);
        return handler.GetFoldingRanges("@");
    }
}
