using Naninovel.Parsing;

namespace Naninovel.Language;

internal class SyntaxDiagnoser(IDocumentRegistry docs, DiagnosticRegistry registry)
    : Diagnoser(docs, registry)
{
    public override DiagnosticContext Context => DiagnosticContext.Syntax;

    protected override void DiagnoseLine (in DocumentLine line)
    {
        foreach (var error in line.Errors)
            AddParseError(error);
    }

    private void AddParseError (ParseError error)
    {
        var range = new Range(
            new(LineIndex, error.StartIndex),
            new(LineIndex, error.EndIndex + 1));
        AddError(range, error.Message);
    }
}
