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

    public void ApplyChanges (IList<DocumentLine> lines, IReadOnlyList<DocumentChange> changes)
    {
        this.lines = lines;
        foreach (var change in changes)
            ApplyChange(change);
    }

    private void ApplyChange (in DocumentChange change)
    {
        var startLineIdx = change.Range.Start.Line;
        var endLineIdx = change.Range.End.Line;
        var changedLines = GetChangedLines(lines[startLineIdx].Text, lines[endLineIdx].Text, change);
        for (int i = endLineIdx; i >= startLineIdx; i--)
            if (i - startLineIdx >= changedLines.Length) lines.RemoveAt(i);
            else lines[i] = factory.CreateLine(changedLines[i - startLineIdx]);
        for (int i = endLineIdx - startLineIdx + 1; i < changedLines.Length; i++)
            lines.Insert(startLineIdx + i, factory.CreateLine(changedLines[i]));
    }

    private string[] GetChangedLines (string startLineText, string endLineText, in DocumentChange change)
    {
        builder.Clear();
        builder.Append(startLineText.AsSpan(0, change.Range.Start.Character));
        builder.Append(change.Text);
        builder.Append(endLineText.AsSpan(change.Range.End.Character));
        return ScriptParser.SplitText(builder.ToString());
    }
}
