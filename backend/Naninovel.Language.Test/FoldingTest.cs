using System.Collections.Generic;
using Moq;
using Xunit;

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
        Assert.Empty(GetRanges("generic", "# label", ""));
    }

    [Fact]
    public void FoldingRangesAreCorrect ()
    {
        var ranges = GetRanges(";", "@c", "#l", "", ";");
        Assert.Equal(2, ranges.Count);
        Assert.Equal(new FoldingRange(0, 1), ranges[0]);
        Assert.Equal(new FoldingRange(4, 4), ranges[1]);
    }

    private IReadOnlyList<FoldingRange> GetRanges (params string[] lines)
    {
        docs.SetupScript("@", lines);
        return handler.GetFoldingRanges("@");
    }
}
