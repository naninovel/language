using System;
using System.Collections.Generic;
using System.Text;
using Naninovel.Parsing;

namespace Naninovel.Language;

internal class DocumentChanger
{
    private readonly StringBuilder builder = new();
    private readonly DocumentFactory factory = new();
    private IList<DocumentLine> lines = null!;
    private int firstChangedLine, lastChangedLine;

    public LineRange ApplyChanges (IList<DocumentLine> lines, IReadOnlyList<DocumentChange> changes)
    {
        Reset(lines);
        foreach (var change in changes)
            ApplyChange(change);
        return new LineRange(firstChangedLine, lastChangedLine);
    }

    private void Reset (IList<DocumentLine> lines)
    {
        firstChangedLine = int.MaxValue;
        lastChangedLine = int.MinValue;
        this.lines = lines;
    }

    private void ApplyChange (in DocumentChange change)
    {
        var startLineIdx = change.Range.Start.Line;
        var endLineIdx = change.Range.End.Line;
        var changedLines = GetChangedLines(lines[startLineIdx].Text, lines[endLineIdx].Text, change);
        for (int i = endLineIdx; i >= startLineIdx; i--)
            if (i - startLineIdx >= changedLines.Length) RemoveLine(i);
            else ChangeLine(i, factory.CreateLine(changedLines[i - startLineIdx]));
        for (int i = endLineIdx - startLineIdx + 1; i < changedLines.Length; i++)
            InsertLine(startLineIdx + i, factory.CreateLine(changedLines[i]));
    }

    private string[] GetChangedLines (string startLineText, string endLineText, in DocumentChange change)
    {
        builder.Clear();
        builder.Append(startLineText.AsSpan(0, change.Range.Start.Character));
        builder.Append(change.Text);
        builder.Append(endLineText.AsSpan(change.Range.End.Character));
        return ScriptParser.SplitText(builder.ToString());
    }

    private void InsertLine (int index, DocumentLine line)
    {
        lines.Insert(index, line);
        UpdateChangedRange(index);
    }

    private void ChangeLine (int index, DocumentLine line)
    {
        lines[index] = line;
        UpdateChangedRange(index);
    }

    private void RemoveLine (int index)
    {
        lines.RemoveAt(index);
        UpdateChangedRange(index);
    }

    private void UpdateChangedRange (int changedIndex)
    {
        if (changedIndex < firstChangedLine) firstChangedLine = changedIndex;
        if (changedIndex > lastChangedLine) lastChangedLine = changedIndex;
        if (lastChangedLine >= lines.Count) lastChangedLine = lines.Count - 1;
    }
}
