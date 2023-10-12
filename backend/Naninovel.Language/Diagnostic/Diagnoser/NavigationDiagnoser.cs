using Naninovel.Metadata;
using Naninovel.Parsing;
using static Naninovel.Language.Common;

namespace Naninovel.Language;

internal class NavigationDiagnoser(MetadataProvider meta, IDocumentRegistry docs,
    IEndpointRegistry endpoints, DiagnosticRegistry registry) : Diagnoser(docs, registry)
{
    public override DiagnosticContext Context => DiagnosticContext.Navigation;

    private readonly EndpointResolver resolver = new(meta);

    public override void HandleDocumentAdded (string uri)
    {
        foreach (var otherUri in Docs.GetAllUris())
        foreach (var item in Registry.Get(otherUri))
            if (item.Context == Context)
                Rediagnose(new(otherUri, item.Line));
        Diagnose(uri);
    }

    public override void HandleDocumentRemoved (string uri)
    {
        Remove(uri);
        var name = ToScriptName(uri);
        var doc = Docs.Get(uri);
        foreach (var location in endpoints.GetNavigatorLocations(new(name)))
            Rediagnose(location);
        for (int i = 0; i < doc.LineCount; i++)
            HandleLineRemoved(doc[i].Script, name);
    }

    public override void HandleDocumentChanging (string uri, LineRange range)
    {
        var name = ToScriptName(uri);
        var doc = Docs.Get(uri);
        Remove(uri, range);
        for (int i = range.Start; i <= range.End; i++)
            HandleLineRemoved(doc[i].Script, name);
    }

    public override void HandleDocumentChanged (string uri, LineRange range)
    {
        var name = ToScriptName(uri);
        var doc = Docs.Get(uri);
        for (int i = range.Start; i <= range.End; i++)
            HandleLineRemoved(doc[i].Script, name);
        Diagnose(uri, range);
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
        if (!endpoints.NavigatorExist(new(ToScriptName(Uri), line.Label)))
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
        return !endpoints.LabelExist(new(name, point.Label));
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

    private void HandleLineRemoved (IScriptLine line, string scriptName)
    {
        if (line is LabelLine labelLine)
            HandleLabelRemoved(labelLine);
        else if (line is CommandLine commandLine)
            HandleCommandRemoved(commandLine.Command);
        else if (line is GenericLine genericLine)
            foreach (var content in genericLine.Content)
                if (content is InlinedCommand inlined)
                    HandleCommandRemoved(inlined.Command);

        void HandleLabelRemoved (LabelLine labelLine)
        {
            foreach (var location in endpoints.GetNavigatorLocations(new(scriptName, labelLine.Label)))
                Rediagnose(location);
        }

        void HandleCommandRemoved (Parsing.Command command)
        {
            if (resolver.TryResolve(command, out var point))
                HandleNavigatorRemoved(new(point.Script ?? scriptName, point.Label));
        }

        void HandleNavigatorRemoved (in QualifiedEndpoint endpoint)
        {
            if (endpoint.Label is null) return;
            foreach (var location in endpoints.GetLabelLocations(new(endpoint.ScriptName, endpoint.Label)))
                Rediagnose(location);
        }
    }
}
