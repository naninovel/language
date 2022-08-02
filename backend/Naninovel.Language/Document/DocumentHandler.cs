using System;
using System.Text;
using Naninovel.Parsing;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#textDocument_synchronization

public class DocumentHandler
{
    private readonly DocumentRegistry registry;
    private readonly IDiagnoser diagnoser;
    private readonly ScriptParser parser;
    private readonly StringBuilder builder = new();
    private readonly ErrorCollector errors = new();
    private readonly RangeMapper mapper = new();

    public DocumentHandler (DocumentRegistry registry, IDiagnoser diagnoser)
    {
        this.registry = registry;
        this.diagnoser = diagnoser;
        parser = new ScriptParser(errors, mapper);
    }

    public void Open (string uri, string text)
    {
        var document = new Document();
        foreach (var lineText in ScriptParser.SplitText(text))
            document.Lines.Add(CreateLine(lineText));
        registry.Add(uri, document);
        diagnoser.Diagnose(uri, document);
    }

    public void Close (string uri)
    {
        registry.Remove(uri);
    }

    public void Change (string uri, DocumentChange[] changes)
    {
        var document = registry.Get(uri);
        foreach (var change in changes)
            ApplyChange(document, change);
        diagnoser.Diagnose(uri, document);
    }

    private DocumentLine CreateLine (string lineText)
    {
        var lineModel = parser.ParseLine(lineText);
        var lineErrors = CollectErrors();
        var lineMapper = MapComponents();
        return new DocumentLine(lineText, lineModel, lineErrors, lineMapper);
    }

    private ParseError[] CollectErrors ()
    {
        if (errors.Count == 0) return Array.Empty<ParseError>();
        var lineErrors = errors.ToArray();
        errors.Clear();
        return lineErrors;
    }

    private RangeMapper MapComponents ()
    {
        var lineMapper = new RangeMapper();
        foreach (var (component, range) in mapper)
            lineMapper.Associate(component, range);
        mapper.Clear();
        return lineMapper;
    }

    private void ApplyChange (Document document, DocumentChange change)
    {
        var startLineIdx = change.Range.Start.Line;
        var endLineIdx = change.Range.End.Line;
        var changedLines = GetChangedLines(document[startLineIdx].Text, document[endLineIdx].Text, change);
        for (int i = endLineIdx; i >= startLineIdx; i--)
            if (i - startLineIdx >= changedLines.Length) document.Lines.RemoveAt(i);
            else document[i] = CreateLine(changedLines[i - startLineIdx]);
        for (int i = endLineIdx - startLineIdx + 1; i < changedLines.Length; i++)
            document.Lines.Insert(startLineIdx + i, CreateLine(changedLines[i]));
    }

    private string[] GetChangedLines (string startLineText, string endLineText, DocumentChange change)
    {
        builder.Clear();
        builder.Append(startLineText.AsSpan(0, change.Range.Start.Character));
        builder.Append(change.Text);
        builder.Append(endLineText.AsSpan(change.Range.End.Character));
        return ScriptParser.SplitText(builder.ToString());
    }
}
