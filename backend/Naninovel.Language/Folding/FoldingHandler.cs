using System.Collections.Generic;
using Naninovel.Parsing;

namespace Naninovel.Language;

public class FoldingHandler : IFoldingHandler
{
    private readonly IDocumentRegistry registry;
    private readonly List<FoldingRange> ranges = new();

    private int lineIndex;
    private FoldingRange? range;

    public FoldingHandler (IDocumentRegistry registry)
    {
        this.registry = registry;
    }

    public IReadOnlyList<FoldingRange> GetFoldingRanges (string documentUri)
    {
        ResetState();
        var doc = registry.Get(documentUri);
        for (; lineIndex < doc.LineCount; lineIndex++)
            if (ShouldFold(doc[lineIndex].Script))
                FoldLine(doc[lineIndex].Script);
        if (range is not null) AddRange();
        return ranges.ToArray();
    }

    private void ResetState ()
    {
        ranges.Clear();
        lineIndex = 0;
        range = null;
    }

    private bool ShouldFold (IScriptLine line)
    {
        return line is CommentLine or CommandLine;
    }

    private void FoldLine (IScriptLine line)
    {
        if (!range.HasValue) range = new(lineIndex, lineIndex);
        else if (lineIndex > range.Value.EndLine + 1) AddRange();
        else range = new FoldingRange(range.Value.StartLine, lineIndex);
    }

    private void AddRange ()
    {
        ranges.Add(range!.Value);
        range = new(lineIndex, lineIndex);
    }
}
