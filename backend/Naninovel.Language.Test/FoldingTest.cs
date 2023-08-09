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
        var ranges = GetRanges(";", ";", "@c", "#l", "", ";", "@c", "@c");
        Assert.Equal(2, ranges.Count);
        Assert.Equal(new FoldingRange[] { new(0, 1), new(2, 2), new(3, 7), new(5, 5), new(6, 7) }, ranges);
    }

    private IReadOnlyList<FoldingRange> GetRanges (params string[] lines)
    {
        docs.SetupScript("@", lines);
        return handler.GetFoldingRanges("@");
    }
}
