using Naninovel.Parsing;

namespace Naninovel.Language;

public class FoldingHandler (IDocumentRegistry docs) : IFoldingHandler
{
    private enum FoldType
    {
        Comment,
        Label,
        Nested
    }

    private readonly Dictionary<string, int> regionToIndex = [];
    private readonly Dictionary<FoldType, int> foldToIndex = new();
    private readonly List<FoldingRange> closed = [];

    private int lineIndex;

    public IReadOnlyList<FoldingRange> GetFoldingRanges (string documentUri)
    {
        documentUri = Uri.UnescapeDataString(documentUri);
        var doc = docs.Get(documentUri);
        ResetState(documentUri);
        for (; lineIndex < doc.LineCount; lineIndex++)
            VisitLine(doc[lineIndex].Script);
        CloseAll();
        return closed.ToArray();
    }

    private void ResetState (string documentUri)
    {
        closed.Clear();
        foldToIndex[FoldType.Comment] = -1;
        foldToIndex[FoldType.Label] = -1;
        foldToIndex[FoldType.Nested] = -1;
        regionToIndex.Clear();
        lineIndex = 0;
    }

    private void VisitLine (IScriptLine line)
    {
        if (line is CommentLine comment) VisitComment(comment);
        else if (line is LabelLine) VisitLabel();
        else if (line is CommandLine) VisitCommand();
        else VisitGeneric();
        VisitNested(line);
    }

    private void VisitComment (CommentLine line)
    {
        Open(FoldType.Comment);

        if (TryCloseRegion(line.Comment, out var region) &&
            regionToIndex.TryGetValue(region, out var openIndex))
        {
            closed.Add(new(openIndex, lineIndex));
            regionToIndex.Remove(region);
        }
        else if (TryOpenRegion(line.Comment, out region))
            regionToIndex[region] = lineIndex;
    }

    private void VisitLabel ()
    {
        Close(FoldType.Comment);
        Close(FoldType.Label);
        Open(FoldType.Label);
    }

    private void VisitCommand ()
    {
        Close(FoldType.Comment);
    }

    private void VisitGeneric ()
    {
        Close(FoldType.Comment);
    }

    private void VisitNested (IScriptLine line)
    {
        if (line.Indent == 0) Close(FoldType.Nested);
        if (line.Indent > 0 && foldToIndex[FoldType.Nested] < 0)
            foldToIndex[FoldType.Nested] = lineIndex - 1;
    }

    private void Open (FoldType type)
    {
        if (foldToIndex[type] < 0)
            foldToIndex[type] = lineIndex;
    }

    private void Close (FoldType type)
    {
        if (foldToIndex[type] < 0) return;
        closed.Add(new(foldToIndex[type], lineIndex - 1));
        foldToIndex[type] = -1;
    }

    private void CloseAll ()
    {
        Close(FoldType.Comment);
        Close(FoldType.Label);
        Close(FoldType.Nested);
    }

    private bool TryOpenRegion (string comment, out string region)
    {
        region = comment.GetAfterFirst(">").Trim();
        return comment.TrimStart().StartsWith('>');
    }

    private bool TryCloseRegion (string comment, out string region)
    {
        region = comment.GetAfterFirst("<").Trim();
        return comment.TrimStart().StartsWith('<');
    }
}
