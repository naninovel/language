using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

internal class SemanticDiagnoser (MetadataProvider meta, IDocumentRegistry docs,
    DiagnosticRegistry registry) : Diagnoser(docs, registry)
{
    public override DiagnosticContext Context => DiagnosticContext.Semantic;

    private readonly ValueValidator validator = new(meta.Preferences.Identifiers);

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
        DiagnoseNesting(command, commandMeta);
    }

    private void DiagnoseParameter (Parsing.Parameter param, Metadata.Command commandMeta)
    {
        var paramMeta = meta.FindParameter(commandMeta.Id, param.Identifier);
        if (paramMeta is null)
        {
            AddUnknownParameter(param, commandMeta);
            return;
        }
        if (IsPreventingPreload(param, paramMeta)) AddPreventingPreload(param.Value);
        if (param.Value.Count == 0 || param.Value.Dynamic || param.Value[0] is not PlainText value) return;
        if (!validator.Validate(value, paramMeta.ValueContainerType, paramMeta.ValueType)) AddInvalidValue(value, paramMeta);
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

    private void AddPreventingPreload (MixedValue value)
    {
        var range = Line.GetRange(value, LineIndex);
        AddInfo(range, "Expressions in this parameter prevent pre-loading associated resources.");
    }

    private static bool IsParameterDefined (Metadata.Parameter paramMeta, Parsing.Command command)
    {
        foreach (var param in command.Parameters)
            if (param.Nameless && paramMeta.Nameless) return true;
            else if (string.Equals(param.Identifier, paramMeta.Id, StringComparison.OrdinalIgnoreCase)) return true;
            else if (string.Equals(param.Identifier, paramMeta.Alias, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private bool IsPreventingPreload (Parsing.Parameter param, Metadata.Parameter paramMeta)
    {
        if (!param.Value.Dynamic || paramMeta.ValueContext is null) return false;
        foreach (var ctx in paramMeta.ValueContext)
            switch (ctx?.Type)
            {
                case ValueContextType.Resource:
                case ValueContextType.Actor:
                case ValueContextType.Appearance: return true;
                default: continue;
            }
        return false;
    }

    private void DiagnoseNesting (Parsing.Command command, Metadata.Command commandMeta)
    {
        var doc = Docs.Get(Uri);
        var nextLine = doc.LineCount == (LineIndex + 1) ? default : doc[LineIndex + 1];
        var hasNested = nextLine != default && nextLine.Script.Indent > Line.Script.Indent;

        if (!commandMeta.NestedHost && hasNested)
            AddWarning(Line.GetRange(command, LineIndex), "This command doesn't support nesting.");
        if (commandMeta.RequiresNested && !hasNested)
            AddError(Line.GetRange(command, LineIndex), "This command requires nested lines.");
    }
}
