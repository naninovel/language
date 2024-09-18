using Naninovel.Parsing;

namespace Naninovel.Language;

internal class SyntaxDiagnoser (IDocumentRegistry docs, DiagnosticRegistry registry)
    : Diagnoser(docs, registry)
{
    public override DiagnosticContext Context => DiagnosticContext.Syntax;

    protected override void DiagnoseLine (in DocumentLine line)
    {
        foreach (var error in line.Errors)
            AddParseError(error);
        CheckIndentation(line.Text);
    }

    private void AddParseError (ParseError error)
    {
        var range = new Range(
            new(LineIndex, error.StartIndex),
            new(LineIndex, error.EndIndex + 1));
        AddError(range, error.Message);
    }

    private void CheckIndentation (string lineText)
    {
        var spaceCount = 0;
        for (var i = 0; i < lineText.Length; i++)
        {
            var @char = lineText[i];
            if (@char == '\t')
                AddError(new Range(new(LineIndex, i), new(LineIndex, i + 1)),
                    "Tab indents are not supported. Use 4 spaces.");
            else if (@char == ' ') spaceCount++;
            else break;
        }
        if (spaceCount % 4 != 0)
            AddError(new Range(new(LineIndex, 0), new(LineIndex, spaceCount)),
                "Each indent level should be exactly 4 spaces.");
    }
}
