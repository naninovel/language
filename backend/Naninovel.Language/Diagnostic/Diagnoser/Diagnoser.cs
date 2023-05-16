using System.Collections.Generic;

namespace Naninovel.Language;

internal abstract class Diagnoser
{
    public abstract DiagnosticContext Context { get; }
    public abstract void HandleDocumentAdded (string uri);
    public abstract void HandleDocumentRemoved (string uri);
    public abstract void HandleDocumentChanged (string uri, in LineRange range);

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

    protected void AddError (Range range, string message) =>
        AddDiagnostic(range, DiagnosticSeverity.Error, message);

    protected void AddWarning (Range range, string message) =>
        AddDiagnostic(range, DiagnosticSeverity.Warning, message);

    protected void AddUnnecessary (Range range, string message) =>
        AddDiagnostic(range, DiagnosticSeverity.Warning, message, unnecessary);

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

    private void AddDiagnostic (Range range, DiagnosticSeverity severity,
        string message, IReadOnlyList<DiagnosticTag>? tags = null)
    {
        Registry.Add(Uri, new(LineIndex, Context, new(range, severity, message, tags)));
    }
}
