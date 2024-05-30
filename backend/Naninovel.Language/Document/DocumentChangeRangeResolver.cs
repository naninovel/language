namespace Naninovel.Language;

internal class DocumentChangeRangeResolver
{
    private int firstChangedLine;
    private int lastChangedLine;
    private int totalInsertedBreaks;
    private bool insertedOrDeletedLine;

    public LineRange Resolve (IReadOnlyList<DocumentChange> changes, int lineCount)
    {
        Reset();
        foreach (var change in changes)
            ProcessChange(change);
        return new(firstChangedLine, ResolveEnd(lineCount));
    }

    private void Reset ()
    {
        firstChangedLine = int.MaxValue;
        lastChangedLine = int.MinValue;
        insertedOrDeletedLine = false;
        totalInsertedBreaks = 0;
    }

    private void ProcessChange (in DocumentChange change)
    {
        if (change.Range.Start.Line < firstChangedLine)
            firstChangedLine = change.Range.Start.Line;
        if (change.Range.End.Line > lastChangedLine)
            lastChangedLine = change.Range.End.Line;
        var insertedBreaks = change.Text.SplitLines().Length - 1;
        var removedBreaks = change.Range.End.Line - change.Range.Start.Line;
        totalInsertedBreaks += insertedBreaks;
        if (!insertedOrDeletedLine && removedBreaks != insertedBreaks)
            insertedOrDeletedLine = true;
    }

    private int ResolveEnd (int lineCount)
    {
        var max = lineCount - 1;
        var last = insertedOrDeletedLine ? (max + totalInsertedBreaks) : lastChangedLine;
        return Math.Min(max, last);
    }
}
