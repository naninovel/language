using Naninovel.Metadata;
using Naninovel.Parsing;
using static Naninovel.Language.Common;

namespace Naninovel.Language;

internal class NavigationDiagnoser : Diagnoser
{
    public override DiagnosticContext Context => DiagnosticContext.Navigation;

    private readonly IEndpointRegistry endpoints;
    private readonly EndpointResolver resolver;

    public NavigationDiagnoser (MetadataProvider meta, IDocumentRegistry docs,
        IEndpointRegistry endpoints, DiagnosticRegistry registry) : base(docs, registry)
    {
        this.endpoints = endpoints;
        resolver = new(meta);
    }

    public override void HandleDocumentAdded (string uri)
    {
        foreach (var otherUri in Docs.GetAllUris())
        foreach (var item in Registry.Get(otherUri))
            if (item.Context == Context)
                Rediagnose(otherUri, new(item.Line, item.Line));
        Diagnose(uri);
    }

    public override void HandleDocumentRemoved (string uri)
    {
        Remove(uri);
        // TODO: Don't do this. Instead get endpoint registry and find which lines should be re-diagnosed.
        foreach (var otherUri in Docs.GetAllUris())
            Rediagnose(otherUri);
    }

    public override void HandleDocumentChanged (string uri, LineRange range)
    {
        // TODO: Get endpoint registry and find which lines should be re-diagnosed.
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
        if (!endpoints.LabelUsed(ToScriptName(Uri), line.Label))
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
        if (resolver.TryResolve(param, commandId, out var point) && IsEndpointUnknown(point))
            AddUnknownEndpoint(param);
    }

    private bool IsEndpointUnknown (Endpoint point)
    {
        var name = point.Script ?? ToScriptName(Uri);
        if (point.Label is null) return !endpoints.ScriptExist(name);
        return !endpoints.LabelExist(name, point.Label);
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
