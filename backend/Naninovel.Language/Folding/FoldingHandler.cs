using System.Collections.Generic;
using Naninovel.Parsing;

namespace Naninovel.Language;

public class FoldingHandler : IFoldingHandler
{
    private readonly IDocumentRegistry registry;
    private readonly List<FoldingRange> closed = new();
    private readonly Dictionary<LineType, int> open = new();

    private int lineIndex;

    public FoldingHandler (IDocumentRegistry registry)
    {
        this.registry = registry;
    }

    public IReadOnlyList<FoldingRange> GetFoldingRanges (string documentUri)
    {
        ResetState();
        var doc = registry.Get(documentUri);
        for (; lineIndex < doc.LineCount; lineIndex++)
            VisitLine(doc[lineIndex].Script);
        CloseAll();
        return closed.ToArray();
    }

    private void ResetState ()
    {
        closed.Clear();
        open[LineType.Comment] = -1;
        open[LineType.Command] = -1;
        open[LineType.Label] = -1;
        lineIndex = 0;
    }

    private void VisitLine (IScriptLine line)
    {
        if (line is CommentLine) VisitComment();
        else if (line is CommandLine) VisitCommand();
        else if (line is LabelLine) VisitLabel();
        else VisitGeneric();
    }

    private void VisitComment ()
    {
        Close(LineType.Command);
        Open(LineType.Comment);
    }

    private void VisitCommand ()
    {
        Close(LineType.Comment);
        Open(LineType.Command);
    }

    private void VisitLabel ()
    {
        CloseAll();
        Open(LineType.Label);
    }

    private void VisitGeneric ()
    {
        Close(LineType.Comment);
        Close(LineType.Command);
    }

    private void Open (LineType type)
    {
        if (open[type] < 0)
            open[type] = lineIndex;
    }

    private void Close (LineType type)
    {
        if (open[type] < 0) return;
        closed.Add(new(open[type], lineIndex - 1));
        open[type] = -1;
    }

    private void CloseAll ()
    {
        Close(LineType.Comment);
        Close(LineType.Command);
        Close(LineType.Label);
    }
}
