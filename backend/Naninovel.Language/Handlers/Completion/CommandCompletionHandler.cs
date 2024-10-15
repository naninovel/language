using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

internal class CommandCompletionHandler
{
    private readonly record struct CommandContext (Parsing.Command Model, Metadata.Command Meta);
    private readonly record struct ParameterContext (Parsing.Parameter Model, Metadata.Parameter Meta);

    private readonly IMetadata meta;
    private readonly CompletionProvider completions;
    private readonly IEndpointRegistry endpoints;
    private readonly ExpressionCompletionHandler expHandler;
    private readonly NamedValueParser namedParser;
    private readonly ExpressionEvaluator expEval;
    private int cursor => position.Character;
    private char charBehindCursor;
    private Position position;
    private DocumentLine line;
    private CommandContext command;
    private ParameterContext param;
    private string scriptPath = string.Empty;

    public CommandCompletionHandler (IMetadata meta, CompletionProvider completions, IEndpointRegistry endpoints)
    {
        this.meta = meta;
        this.completions = completions;
        this.endpoints = endpoints;
        expHandler = new ExpressionCompletionHandler(meta, endpoints, completions);
        namedParser = new NamedValueParser(meta.Syntax);
        expEval = new(meta, () => scriptPath, GetParamValue);
    }

    public CompletionItem[] Handle (Parsing.Command command, in Position position,
        in DocumentLine line, string scriptPath, bool inline)
    {
        ResetState(position, line, scriptPath);
        if (ShouldCompleteCommandId(command))
            return inline ? completions.GetInlineCommands() : completions.GetLineCommands();
        if (!TryResolveCommandContext(command, out this.command)) return [];
        if (!TryResolveParameterContext(out param)) return GetParameters();
        return GetParameterValues();
    }

    private void ResetState (in Position position, in DocumentLine line, string scriptPath)
    {
        this.line = line;
        this.position = position;
        this.scriptPath = scriptPath;
        charBehindCursor = line.GetCharBehindCursor(position);
    }

    private CompletionItem[] GetParameters ()
    {
        return completions.GetParameters(command.Meta.Id)
            .Where(item => {
                var itemId = item.Label;
                var paramMeta = meta.FindParameter(command.Meta.Id, itemId);
                if (paramMeta != null && paramMeta.Nameless && command.Model.Parameters.Any(p => p.Nameless))
                    return false;
                return !command.Model.Parameters.Any(p => itemId == p.Identifier);
            }).ToArray();
    }

    private bool IsCursorOver (ILineComponent content) => line.IsCursorOver(content, position);

    private bool ShouldCompleteCommandId (Parsing.Command command)
    {
        return IsCursorOver(command.Identifier) || string.IsNullOrEmpty(command.Identifier) &&
            (charBehindCursor == meta.Syntax.CommandLine[0] ||
             charBehindCursor == meta.Syntax.InlinedOpen[0]);
    }

    private bool TryResolveCommandContext (Parsing.Command model, out CommandContext ctx)
    {
        ctx = default;
        if (meta.FindCommand(model.Identifier) is not { } commandMeta) return false;
        ctx = new(model, commandMeta);
        return true;
    }

    private bool TryResolveParameterContext (out ParameterContext ctx)
    {
        ctx = default;
        if (command.Model.Parameters.FirstOrDefault(IsCursorOver) is not { } paramModel)
            if (!ShouldCompleteNameless()) return false;
            else paramModel = new Parsing.Parameter(new MixedValue([]));
        if (meta.FindParameter(command.Meta.Id, paramModel.Identifier) is not { } paramMeta) return false;
        ctx = new ParameterContext(paramModel, paramMeta);
        return true;
    }

    private bool ShouldCompleteNameless ()
    {
        if (!command.Meta.Parameters.Any(p => p.Nameless)) return false;
        if (!line.TryResolve(command.Model.Identifier, out var idRange)) return false;
        return cursor == idRange.End + 2;
    }

    private CompletionItem[] GetParameterValues ()
    {
        if (ShouldCompleteExpressions(out var expression))
            return expHandler.Handle(expression.Body, position, line, scriptPath);
        if (param.Meta.ValueType == Metadata.ValueType.Boolean)
            return completions.GetBooleans();
        if (FindValueContext() is { } context)
            return GetContextValues(context, param.Model.Value.FirstOrDefault(IsCursorOver));
        return [];
    }

    private bool ShouldCompleteExpressions ([NotNullWhen(true)] out Parsing.Expression? expression)
    {
        expression = param.Model.Value.OfType<Parsing.Expression>().FirstOrDefault(IsCursorOver);
        return expression != null && charBehindCursor != meta.Syntax.ExpressionClose[0];
    }

    private ValueContext? FindValueContext ()
    {
        if (param.Meta.ValueContext is null) return null;
        if (IsCursorOverNamed(out var overValue) && overValue)
            return param.Meta.ValueContext.ElementAtOrDefault(1);
        return param.Meta.ValueContext.ElementAtOrDefault(0);
    }

    private bool IsCursorOverNamed (out bool overNamedValue)
    {
        overNamedValue = false;
        if (param.Model.Value.Count == 0 || param.Meta.ValueContainerType != ValueContainerType.Named) return false;
        var lastDotIndex = line.GetLineRange(param.Model.Value).Start +
                           line.Extract(param.Model.Value).LastIndexOf(meta.Syntax.NamedDelimiter[0]);
        overNamedValue = lastDotIndex >= 0 && cursor > lastDotIndex;
        return true;
    }

    private CompletionItem[] GetContextValues (ValueContext ctx, IValueComponent? cmp) => ctx.Type switch {
        ValueContextType.Expression => expHandler.Handle(cmp as PlainText ?? (cmp as Parsing.Expression)?.Body, position, line, scriptPath),
        ValueContextType.Constant => GetConstantValues(ctx),
        ValueContextType.Endpoint => GetEndpointValues(ctx),
        ValueContextType.Resource => completions.GetResources(ctx.SubType ?? ""),
        ValueContextType.Actor => completions.GetActors(ctx.SubType ?? ""),
        ValueContextType.Appearance when FindActor() is { } actor => completions.GetAppearances(actor.Id, actor.Type),
        ValueContextType.Appearance when !string.IsNullOrEmpty(ctx.SubType) => completions.GetAppearances(ctx.SubType),
        _ => []
    };

    private (string Id, string? Type)? FindActor ()
    {
        foreach (var param in command.Model.Parameters)
            if (meta.FindParameter(command.Meta.Id, param.Identifier) is not { } paramMeta) continue;
            else if (paramMeta.ValueContext is null) continue;
            else if (paramMeta.ValueContext.ElementAtOrDefault(0)?.Type == ValueContextType.Actor)
                return paramMeta.ValueContainerType == ValueContainerType.Named
                    ? (GetNamedValue(param.Value, true)!, paramMeta.ValueContext[0]!.SubType)
                    : (line.Extract(param.Value), paramMeta.ValueContext[0]!.SubType);
            else if (paramMeta.ValueContext.ElementAtOrDefault(1)?.Type == ValueContextType.Actor)
                return (GetNamedValue(param.Value, false)!, paramMeta.ValueContext[1]!.SubType);
        return null;
    }

    private string? GetNamedValue (MixedValue value, bool name)
    {
        var valueText = line.Extract(value);
        var parsed = namedParser.Parse(valueText);
        return name ? parsed.Name : parsed.Value;
    }

    private CompletionItem[] GetConstantValues (ValueContext context)
    {
        using var _ = ListPool<string>.Rent(out var names);
        expEval.Evaluate(context.SubType ?? "", names);
        return names.SelectMany(completions.GetConstants).ToArray();
    }

    private CompletionItem[] GetEndpointValues (ValueContext context)
    {
        if (context.SubType == Constants.EndpointScript)
        {
            var labels = endpoints.GetLabelsInScript(scriptPath);
            return completions.GetScriptEndpoints(endpoints.GetAllScriptPaths(), labels.Count > 0);
        }
        var script = GetNamedValue(param.Model.Value, true) ?? scriptPath;
        return completions.GetLabelEndpoints(endpoints.GetLabelsInScript(script));
    }

    private string? GetParamValue (string id, int? index)
    {
        foreach (var param in command.Model.Parameters)
            if (meta.FindParameter(command.Meta.Id, param.Identifier)?.Id == id)
                return index.HasValue ? GetNamedValue(param.Value, index == 0) : line.Extract(param.Value);
        return null;
    }
}
