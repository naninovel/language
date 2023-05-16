using System;
using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

internal class SemanticDiagnoser : Diagnoser
{
    public override DiagnosticContext Context => DiagnosticContext.Semantic;

    private readonly ValueValidator validator = new();
    private readonly MetadataProvider meta;

    public SemanticDiagnoser (MetadataProvider meta, IDocumentRegistry docs, DiagnosticRegistry registry)
        : base(docs, registry)
    {
        this.meta = meta;
    }

    public override void HandleDocumentAdded (string uri)
    {
        Diagnose(uri);
    }

    public override void HandleDocumentRemoved (string uri)
    {
        Registry.Remove(uri, i => i.Context == Context);
    }

    public override void HandleDocumentChanged (string uri, LineRange range)
    {
        Registry.Remove(uri, i => i.Context == Context && range.Contains(i.Line));
        Diagnose(uri, range);
    }

    protected override void DiagnoseLine (in DocumentLine line)
    {
        if (line.Script is CommandLine commandLine)
            DiagnoseCommand(commandLine.Command);
        else if (line.Script is GenericLine genericLine)
            DiagnoseGenericLine(genericLine);
    }

    private void DiagnoseGenericLine (GenericLine line)
    {
        foreach (var content in line.Content)
            if (content is InlinedCommand inlined)
                DiagnoseCommand(inlined.Command);
    }

    private void DiagnoseCommand (Parsing.Command command)
    {
        if (string.IsNullOrEmpty(command.Identifier)) return;
        var commandMeta = meta.FindCommand(command.Identifier);
        if (commandMeta is null) AddUnknownCommand(command);
        else DiagnoseCommand(command, commandMeta);
    }

    private void DiagnoseCommand (Parsing.Command command, Metadata.Command commandMeta)
    {
        foreach (var paramMeta in commandMeta.Parameters)
            if (paramMeta.Required && !IsParameterDefined(paramMeta, command))
                AddMissingRequiredParameter(command, paramMeta);
        foreach (var param in command.Parameters)
            DiagnoseParameter(param, commandMeta);
    }

    private void DiagnoseParameter (Parsing.Parameter param, Metadata.Command commandMeta)
    {
        var paramMeta = meta.FindParameter(commandMeta.Id, param.Identifier);
        if (paramMeta is null) AddUnknownParameter(param, commandMeta);
        else if (param.Value.Count == 0 || param.Value.Dynamic || param.Value[0] is not PlainText value) return;
        else if (!validator.Validate(value, paramMeta.ValueContainerType, paramMeta.ValueType)) AddInvalidValue(value, paramMeta);
    }

    private void AddUnknownCommand (Parsing.Command command)
    {
        var range = Line.GetRange(command, LineIndex);
        AddError(range, $"Command '{command.Identifier}' is unknown.");
    }

    private void AddUnknownParameter (Parsing.Parameter param, Metadata.Command commandMeta)
    {
        var range = Line.GetRange(param, LineIndex);
        var message = param.Nameless
            ? $"Command '{commandMeta.Label}' doesn't have a nameless parameter."
            : $"Command '{commandMeta.Label}' doesn't have '{param.Identifier}' parameter.";
        AddError(range, message);
    }

    private void AddMissingRequiredParameter (Parsing.Command command, Metadata.Parameter missingParam)
    {
        var range = Line.GetRange(command, LineIndex);
        AddError(range, $"Required parameter '{missingParam.Label}' is missing.");
    }

    private void AddInvalidValue (PlainText value, Metadata.Parameter paramMeta)
    {
        var range = Line.GetRange(value, LineIndex);
        AddError(range, $"Invalid value: '{value}' is not a {paramMeta.TypeLabel}.");
    }

    private static bool IsParameterDefined (Metadata.Parameter paramMeta, Parsing.Command command)
    {
        foreach (var param in command.Parameters)
            if (param.Nameless && paramMeta.Nameless) return true;
            else if (string.Equals(param.Identifier, paramMeta.Id, StringComparison.OrdinalIgnoreCase)) return true;
            else if (string.Equals(param.Identifier, paramMeta.Alias, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
