using System.Collections.Generic;

namespace Naninovel.Language;

internal abstract class Diagnoser : IDiagnoser
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
        var specRange = range ?? new(0, document.LineCount - 1);
        for (LineIndex = specRange.Start; LineIndex <= specRange.End; LineIndex++)
            DiagnoseLine(Line = document[LineIndex]);
    }

    protected void Remove (string uri, LineRange? range = null)
    {
        Registry.Remove(uri, i => i.Context == Context && (!range.HasValue || range.Value.Contains(i.Line)));
    }

    protected void Rediagnose (string uri, LineRange? range = null)
    {
        Remove(uri, range);
        Diagnose(uri, range);
    }

    protected abstract void DiagnoseLine (in DocumentLine line);

    private void AddDiagnostic (in Diagnostic diagnostic)
    {
        Registry.Add(Uri, new(LineIndex, Context, diagnostic));
    }
}
