using System.Collections.Generic;

namespace Naninovel.Language;

internal abstract class Diagnoser
{
    public abstract DiagnosticContext Context { get; }

    protected IDocumentRegistry Docs { get; }
    protected DiagnosticRegistry Registry { get; }
    protected string Uri { get; private set; } = "";
    protected DocumentLine Line { get; private set; }
    protected int LineIndex { get; private set; }

    private static readonly IReadOnlyList<DiagnosticTag> unnecessary = new[] { DiagnosticTag.Unnecessary };

    protected Diagnoser (IDocumentRegistry docs, DiagnosticRegistry registry)
    {
        Docs = docs;
        Registry = registry;
    }

    public abstract void HandleDocumentAdded (string uri);
    public abstract void HandleDocumentRemoved (string uri);
    public abstract void HandleDocumentChanged (string uri, LineRange range);

    protected void AddError (in Range range, string message) =>
        AddDiagnostic(new(range, DiagnosticSeverity.Error, message));

    protected void AddWarning (in Range range, string message) =>
        AddDiagnostic(new(range, DiagnosticSeverity.Warning, message));

    protected void AddUnnecessary (in Range range, string message) =>
        AddDiagnostic(new(range, DiagnosticSeverity.Warning, message, unnecessary));

    protected void Diagnose (string uri, LineRange? range = null)
    {
        Uri = uri;
        var document = Docs.Get(uri);
        Diagnose(document, range ?? new(0, document.LineCount - 1));
    }

    protected abstract void DiagnoseLine (in DocumentLine line);

    private void Diagnose (IDocument document, in LineRange range)
    {
        for (LineIndex = range.Start; LineIndex <= range.End; LineIndex++)
            DiagnoseLine(Line = document[LineIndex]);
    }

    private void AddDiagnostic (in Diagnostic diagnostic)
    {
        Registry.Add(Uri, new(LineIndex, Context, diagnostic));
    }
}
