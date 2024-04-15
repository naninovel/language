using Naninovel.Parsing;

namespace Naninovel.Language;

public class FoldingHandler (IDocumentRegistry registry) : IFoldingHandler
{
    private enum Region
    {
        Comment,
        Label,
        Nested
    }

    private readonly List<FoldingRange> closed = [];
    private readonly Dictionary<Region, int> open = new();

    private IDocument doc = null!;
    private int lineIndex;

    public IReadOnlyList<FoldingRange> GetFoldingRanges (string documentUri)
    {
        ResetState(documentUri);
        for (; lineIndex < doc.LineCount; lineIndex++)
            VisitLine(doc[lineIndex].Script);
        CloseAll();
        return closed.ToArray();
    }

    private void ResetState (string documentUri)
    {
        closed.Clear();
        open[Region.Comment] = -1;
        open[Region.Label] = -1;
        open[Region.Nested] = -1;
        lineIndex = 0;
        doc = registry.Get(documentUri);
    }

    private void VisitLine (IScriptLine line)
    {
        if (line is CommentLine) VisitComment();
        else if (line is LabelLine) VisitLabel();
        else if (line is CommandLine) VisitCommand();
        else VisitGeneric();
        VisitNested(line);
    }

    private void VisitComment ()
    {
        Open(Region.Comment);
    }

    private void VisitLabel ()
    {
        Close(Region.Comment);
        Close(Region.Label);
        Open(Region.Label);
    }

    private void VisitCommand ()
    {
        Close(Region.Comment);
    }

    private void VisitGeneric ()
    {
        Close(Region.Comment);
    }

    private void VisitNested (IScriptLine line)
    {
        if (line.Indent == 0) Close(Region.Nested);
        if (line.Indent > 0 && open[Region.Nested] < 0)
            open[Region.Nested] = lineIndex - 1;
    }

    private void Open (Region type)
    {
        if (open[type] < 0)
            open[type] = lineIndex;
    }

    private void Close (Region type)
    {
        if (open[type] < 0) return;
        closed.Add(new(open[type], lineIndex - 1));
        open[type] = -1;
    }

    private void CloseAll ()
    {
        Close(Region.Comment);
        Close(Region.Label);
        Close(Region.Nested);
    }
}
