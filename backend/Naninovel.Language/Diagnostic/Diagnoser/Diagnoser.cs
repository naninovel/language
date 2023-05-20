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

    protected void Diagnose (string uri)
    {
        Uri = uri;
        var document = Docs.Get(uri);
        for (LineIndex = 0; LineIndex < document.LineCount; LineIndex++)
            DiagnoseLine(Line = document[LineIndex]);
    }

    protected void Diagnose (in LineLocation location)
    {
        Uri = location.DocumentUri;
        LineIndex = location.LineIndex;
        DiagnoseLine(Line = Docs.Get(location.DocumentUri)[location.LineIndex]);
    }

    protected void Remove (string uri)
    {
        Registry.Remove(uri, i => i.Context == Context);
    }

    protected void Remove (LineLocation location)
    {
        Registry.Remove(location.DocumentUri, i => i.Context == Context && i.Line == location.LineIndex);
    }

    protected void Rediagnose (string uri, LineRange range)
    {
        Registry.Remove(uri, i => i.Context == Context && range.Contains(i.Line));
        Uri = uri;
        var document = Docs.Get(uri);
        for (LineIndex = range.Start; LineIndex <= range.End; LineIndex++)
            DiagnoseLine(Line = document[LineIndex]);
    }

    protected void Rediagnose (in LineLocation location)
    {
        Remove(location);
        Diagnose(location);
    }

    protected abstract void DiagnoseLine (in DocumentLine line);

    private void AddDiagnostic (in Diagnostic diagnostic)
    {
        Registry.Add(Uri, new(LineIndex, Context, diagnostic));
    }
}
