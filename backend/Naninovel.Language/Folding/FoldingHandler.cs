using System.Collections.Generic;
using Naninovel.Parsing;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_foldingRange

public class FoldingHandler
{
    private readonly DocumentRegistry registry;
    private readonly List<FoldingRange> ranges = new();

    private int lineIndex;
    private FoldingRange? range;

    public FoldingHandler (DocumentRegistry registry)
    {
        this.registry = registry;
    }

    public FoldingRange[] GetFoldingRanges (string documentUri)
    {
        ResetState();
        var lines = registry.Get(documentUri).Lines;
        for (; lineIndex < lines.Count; lineIndex++)
            if (ShouldFold(lines[lineIndex].Script))
                FoldLine(lines[lineIndex].Script);
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
