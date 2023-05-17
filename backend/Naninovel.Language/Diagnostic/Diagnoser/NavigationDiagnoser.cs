using Naninovel.Metadata;
using Naninovel.Parsing;
using Naninovel.Utilities;
using static Naninovel.Language.Common;

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

    public override void HandleDocumentAdded (string uri)
    {
        foreach (var otherUri in Docs.GetAllUris())
            if (otherUri != uri)
                Rediagnose(otherUri);
        Diagnose(uri);
    }

    public override void HandleDocumentRemoved (string uri)
    {
        Remove(uri);
        foreach (var otherUri in Docs.GetAllUris())
            Rediagnose(otherUri);
    }

    public override void HandleDocumentChanged (string uri, LineRange range)
    {
        Rediagnose(uri, range);
    }

    protected override void DiagnoseLine (in DocumentLine line)
    {
        if (line.Script is LabelLine labelLine)
            DiagnoseLabelLine(labelLine);
        else if (line.Script is CommandLine commandLine)
            DiagnoseCommand(commandLine.Command);
        else if (line.Script is GenericLine genericLine)
            DiagnoseGenericLine(genericLine);
    }

    private void DiagnoseLabelLine (LabelLine line)
    {
        if (!Docs.IsEndpointUsed(ToScriptName(Uri), line.Label))
            AddUnusedLabel(line.Label);
    }

    private void DiagnoseGenericLine (GenericLine line)
    {
        foreach (var content in line.Content)
            if (content is InlinedCommand inlined)
                DiagnoseCommand(inlined.Command);
    }

    private void DiagnoseCommand (Parsing.Command command)
    {
        foreach (var parameter in command.Parameters)
            DiagnoseParameter(parameter, command.Identifier);
    }

    private void DiagnoseParameter (Parsing.Parameter param, string commandId)
    {
        if (endpoint.TryResolve(param, commandId, out var point) && IsEndpointUnknown(point))
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
