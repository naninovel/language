﻿using System.Text;
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
            ApplyChanges(change);
    }

    private void ApplyChanges (in DocumentChange documentChange)
    {
        var startLineIdx = documentChange.Range.Start.Line;
        var endLineIdx = documentChange.Range.End.Line;
        var changedLines = GetChangedLines(lines[startLineIdx].Text, lines[endLineIdx].Text, documentChange);
        for (int i = endLineIdx; i >= startLineIdx; i--)
            if (i - startLineIdx >= changedLines.Length) lines.RemoveAt(i);
            else lines[i] = factory.CreateLine(changedLines[i - startLineIdx]);
        for (int i = endLineIdx - startLineIdx + 1; i < changedLines.Length; i++)
            lines.Insert(startLineIdx + i, factory.CreateLine(changedLines[i]));
    }

    private string[] GetChangedLines (string startLineText, string endLineText, in DocumentChange documentChange)
    {
        builder.Clear();
        builder.Append(startLineText.AsSpan(0, documentChange.Range.Start.Character));
        builder.Append(documentChange.Text);
        builder.Append(endLineText.AsSpan(documentChange.Range.End.Character));
        return ScriptParser.SplitText(builder.ToString());
    }
}
