using System.IO;
using Naninovel.Metadata;
using Naninovel.Parsing;
using Naninovel.Utilities;

namespace Naninovel.Language;

internal class NavigationDiagnoser : Diagnoser
{
    public override DiagnosticContext Context => DiagnosticContext.Navigation;

    private readonly EndpointResolver endpoint;

    public NavigationDiagnoser (MetadataProvider meta, IDocumentRegistry docs, DiagnosticRegistry registry)
        : base(docs, registry)
    {
        endpoint = new(meta);
    }

    public override void HandleDocumentAdded (string uri) { }

    public override void HandleDocumentRemoved (string uri) { }

    public override void HandleDocumentChanged (string uri, in LineRange range) { }

    protected override void DiagnoseLine (in DocumentLine line) { }

    private void DiagnoseLabelLine (LabelLine labelLine)
    {
        if (!Docs.IsEndpointUsed(Path.GetFileNameWithoutExtension(Uri), labelLine.Label))
            AddUnusedLabel(labelLine.Label);
    }

    private void DiagnoseParameter (Parsing.Parameter param, Metadata.Command commandMeta)
    {
        if (endpoint.TryResolve(param, commandMeta.Id, out var point) && IsEndpointUnknown(point))
            AddUnknownEndpoint(param);
    }

    private bool IsEndpointUnknown (Endpoint point)
    {
        var uri = string.IsNullOrEmpty(point.Script) ? Uri : ResolveUriByScriptName(point.Script);
        return uri is null || !Docs.Contains(uri, point.Label);
    }

    private string? ResolveUriByScriptName (string name)
    {
        var nameWithExtensions = name + ".nani";
        foreach (var uri in Docs.GetAllUris())
            if (uri.EndsWithOrdinal(nameWithExtensions))
                return uri;
        return null;
    }

    private void AddUnknownEndpoint (Parsing.Parameter param)
    {
        var range = Line.GetRange(param.Value, LineIndex);
        AddWarning(range, $"Unknown endpoint: {param.Value}.");
    }

    private void AddUnusedLabel (PlainText label)
    {
        var range = Line.GetRange(label, LineIndex);
        AddUnnecessary(range, "Unused label.");
    }
}
