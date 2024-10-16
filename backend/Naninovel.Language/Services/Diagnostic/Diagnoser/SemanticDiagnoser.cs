using Naninovel.Expression;
using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

internal class SemanticDiagnoser : Diagnoser
{
    public override DiagnosticContext Context => DiagnosticContext.Semantic;

    private readonly Parser expParser;
    private readonly FunctionResolver fnResolver;
    private readonly List<ParseDiagnostic> expErrors = [];
    private readonly ExpressionEvaluator expEval;
    private readonly ValueValidator valueValidator;
    private readonly IMetadata meta;

    public SemanticDiagnoser (IMetadata meta, IDocumentRegistry docs,
        DiagnosticRegistry registry) : base(docs, registry)
    {
        this.meta = meta;
        expParser = new Parser(new() {
            Syntax = meta.Syntax,
            HandleDiagnostic = expErrors.Add
        });
        fnResolver = new FunctionResolver(meta);
        expEval = new(meta);
        valueValidator = new(meta.Syntax);
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
            else if (content is MixedValue mixed)
                foreach (var cmp in mixed)
                    if (cmp is Parsing.Expression exp)
                        DiagnoseExpression(exp, false);
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
        if (IsPreventingPreload(param, paramMeta))
            AddPreventingPreload(param.Value);
        if (param.Value.Count == 0) return;

        var ctx = paramMeta.ValueContext?.FirstOrDefault();
        if (ctx?.Type == ValueContextType.Expression || param.Value.Dynamic)
            foreach (var value in param.Value)
                if (ctx?.Type == ValueContextType.Expression || value is Parsing.Expression)
                    DiagnoseExpression(value, ctx?.SubType == Constants.Assignment);

        if (param.Value.Dynamic || param.Value[0] is not PlainText plain) return;

        if (!valueValidator.Validate(plain, paramMeta.ValueContainerType, paramMeta.ValueType))
            AddInvalidValue(plain, paramMeta);
    }

    private void DiagnoseExpression (IValueComponent component, bool assignment)
    {
        expErrors.Clear();
        var body = component is Parsing.Expression exp ? exp.Body : (PlainText)component;
        if (assignment) _ = expParser.TryParseAssignments(body, []);
        else _ = expParser.TryParse(body, out _);
        foreach (var err in expErrors)
            AddExpressionError(body, err);
        foreach (var fn in fnResolver.Resolve(body, Line))
            DiagnoseFunction(fn);
    }

    private void DiagnoseFunction (ResolvedFunction fn)
    {
        var fnRange = Line.GetRange(fn.Range, LineIndex);

        if (fn.Meta is not { } fnMeta)
        {
            AddError(fnRange, "Unknown function.");
            return;
        }

        for (var i = 0; i < fnMeta.Parameters.Length; i++)
            if (fn.Parameters.ElementAtOrDefault(i) is not { Value.Length: > 0 } param)
                AddError(fnRange, $"Missing '{fnMeta.Parameters[i].Name}' parameter.");
            else DiagnoseFunctionParameter(fn, param);

        if (fnMeta.Parameters.LastOrDefault() is not { Variadic: true })
            for (var i = fn.Parameters.Count - 1; i >= fnMeta.Parameters.Length; i--)
                AddError(Line.GetRange(fn.Parameters[i].Range, LineIndex), "Unknown parameter.");
    }

    private void DiagnoseFunctionParameter (ResolvedFunction fn, ResolvedFunctionParameter param)
    {
        if (!param.IsOperand || param.Meta is not { } paramMeta) return;
        var paramRange = Line.GetRange(param.Range, LineIndex);

        var names = default(List<string>);
        if (paramMeta.Context is { Type: ValueContextType.Constant } ctx)
        {
            _ = ListPool<string>.Rent(out names);
            expEval.Evaluate(ctx.SubType ?? "", names, new() { Function = fn.Function });
        }
        if (names?.Count > 0 && !meta.Constants.Where(c => names.Contains(c.Name)).Any(c => c.Values.Contains(param.Value, StringComparer.OrdinalIgnoreCase)))
            AddError(paramRange, $"Invalid constant value. Expected to be one of '{string.Join(", ", names)}'.");
        else if (!valueValidator.Validate(param.Value, ValueContainerType.Single, paramMeta.Type))
            AddError(paramRange, $"Invalid value: '{param.Value}' is not a {paramMeta.Type.ToString().FirstToLower()}.");
        if (names is not null) ListPool<string>.Return(names);
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
        var msg = $"Invalid value: '{value}' is not a {paramMeta.TypeLabel}.";
        if (paramMeta.ValueType == Metadata.ValueType.Boolean)
            msg += $" Expected '{meta.Syntax.True}' or '{meta.Syntax.False}'.";
        AddError(range, msg);
    }

    private void AddPreventingPreload (MixedValue value)
    {
        var range = Line.GetRange(value, LineIndex);
        const string msg =
            "Expression in this parameter value prevents resolving associated resources ahead of time, " +
            "which may result in a degraded runtime performance and inefficient asset bundle packaging. " +
            "Consider using custom command instead.";
        AddWarning(range, msg);
    }

    private void AddExpressionError (PlainText text, ParseDiagnostic diagnostic)
    {
        var range = Line.GetRange(text, LineIndex);
        range = new(
            new(range.Start.Line, range.Start.Character + diagnostic.Index),
            new(range.End.Line, range.Start.Character + diagnostic.Index + diagnostic.Length));
        AddError(range, diagnostic.Message);
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

        if (commandMeta.Nest is null && hasNested)
            AddWarning(Line.GetRange(command, LineIndex), "This command doesn't support nesting.");
        if (commandMeta.Nest is { Required: true } && !hasNested)
            AddError(Line.GetRange(command, LineIndex), "This command requires nested lines.");
    }
}
