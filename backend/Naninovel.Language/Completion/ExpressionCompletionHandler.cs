using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

internal class ExpressionCompletionHandler (IMetadata meta, IEndpointRegistry endpoints, CompletionProvider completions)
{
    private readonly FunctionResolver fnResolver = new(meta);
    private readonly NamedValueParser namedParser = new(meta.Syntax);
    private readonly FunctionConstantEvaluator fnConstEval = new(meta.Syntax);
    private DocumentLine line;
    private Position position;
    private ResolvedFunction fn = null!;
    private string scriptName = null!;

    public CompletionItem[] Handle (PlainText? expBody, in Position position, in DocumentLine line, string scriptName)
    {
        if (expBody is null || GetFunctionOverCursor(expBody, position, line) is not { } fn)
            return completions.GetExpressions();
        Reset(fn, position, line, scriptName);
        for (var i = fn.Parameters.Count - 1; i >= 0; i--)
            if (fn.Parameters[i].Meta?.Context is { } ctx && IsParameterOverCursor(i))
                return [..GetForContext(ctx, fn.Parameters[i]).Select(AsParamCompletion), ..completions.GetExpressions()];
        return completions.GetExpressions();
    }

    private void Reset (ResolvedFunction fn, in Position position, in DocumentLine line, string scriptName)
    {
        this.fn = fn;
        this.position = position;
        this.line = line;
        this.scriptName = scriptName;
    }

    private ResolvedFunction? GetFunctionOverCursor (PlainText body, in Position pos, in DocumentLine line)
    {
        foreach (var fn in fnResolver.Resolve(body, line))
            if (line.IsCursorOver(fn.Range, pos))
                return fn;
        return null;
    }

    private bool IsParameterOverCursor (int index)
    {
        var param = fn.Parameters[index];
        if (line.IsCursorOver(param.Range, position)) return true;
        for (int i = position.Character - 1; i >= 0; i--)
            if (char.IsWhiteSpace(line.Text[i])) continue;
            else if (line.Text[i] == '(' || line.Text[i] == ',') return true;
            else break;
        return false;
    }

    private CompletionItem[] GetForContext (ValueContext ctx, ResolvedFunctionParameter param) => ctx.Type switch {
        ValueContextType.Constant => fnConstEval.EvaluateNames(scriptName, ctx, fn).SelectMany(completions.GetConstants).ToArray(),
        ValueContextType.Endpoint => GetEndpointValues(ctx, param.Value ?? ""),
        ValueContextType.Resource => completions.GetResources(ctx.SubType ?? ""),
        ValueContextType.Actor => completions.GetActors(ctx.SubType ?? ""),
        ValueContextType.Appearance when FindActor() is { } actor => completions.GetAppearances(actor.Id, actor.Type),
        ValueContextType.Appearance when !string.IsNullOrEmpty(ctx.SubType) => completions.GetAppearances(ctx.SubType),
        _ => []
    };

    private (string? Id, string? Type)? FindActor ()
    {
        foreach (var param in fn.Parameters)
            if (param.Meta is { Context.Type: ValueContextType.Actor })
                return (param.Value, param.Meta.Context.SubType);
        return null;
    }

    private CompletionItem[] GetEndpointValues (ValueContext context, string value)
    {
        if (context.SubType == Constants.EndpointScript)
        {
            var labels = endpoints.GetLabelsInScript(scriptName);
            return completions.GetScriptEndpoints(endpoints.GetAllScriptNames(), labels.Count > 0);
        }
        var parsed = namedParser.Parse(value);
        var script = parsed.Name ?? scriptName;
        return completions.GetLabelEndpoints(endpoints.GetLabelsInScript(script));
    }

    private CompletionItem AsParamCompletion (CompletionItem item) => item with {
        Label = $"\"{item.Label}\"",
        InsertText = item.InsertText is null ? null : $"\"{item.InsertText}\"",
        CommitCharacters = [","]
    };
}
