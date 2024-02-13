using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

internal class CommandCompletionHandler (MetadataProvider meta, CompletionProvider provider, IEndpointRegistry endpoints)
{
    private readonly record struct CommandContext (Parsing.Command Model, Metadata.Command Meta);
    private readonly record struct ParameterContext (Parsing.Parameter Model, Metadata.Parameter Meta);

    private int cursor => position.Character;
    private char charBehindCursor;
    private Position position;
    private DocumentLine line;
    private CommandContext command;
    private ParameterContext param;
    private string scriptName = string.Empty;

    public CompletionItem[] Handle (Parsing.Command command, in Position position, in DocumentLine line, string scriptName)
    {
        ResetState(position, line, scriptName);
        if (ShouldCompleteCommandId(command)) return provider.GetCommands();
        if (!TryResolveCommandContext(command, out this.command)) return [];
        if (!TryResolveParameterContext(out param)) return provider.GetParameters(this.command.Meta.Id);
        return GetParameterValues();
    }

    private void ResetState (in Position position, in DocumentLine line, string scriptName)
    {
        this.line = line;
        this.position = position;
        this.scriptName = scriptName;
        charBehindCursor = line.GetCharBehindCursor(position);
    }

    private bool IsCursorOver (ILineComponent content) => line.IsCursorOver(content, position);

    private bool ShouldCompleteCommandId (Parsing.Command command)
    {
        return IsCursorOver(command.Identifier) ||
               charBehindCursor == Identifiers.CommandLine[0] ||
               charBehindCursor == Identifiers.InlinedOpen[0];
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
        if (ShouldCompleteExpressions())
            return provider.GetExpressions();
        if (param.Meta.ValueType == Metadata.ValueType.Boolean)
            return provider.GetBooleans();
        if (FindValueContext() is { } context)
            return GetContextValues(context);
        return [];
    }

    private bool ShouldCompleteExpressions ()
    {
        return param.Model.Value.OfType<Expression>().Any(IsCursorOver) &&
               charBehindCursor != Identifiers.ExpressionClose[0];
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
        var lastDotIndex = line.GetLineRange(param.Model.Value).Start + line.Extract(param.Model.Value).LastIndexOf('.');
        overNamedValue = lastDotIndex >= 0 && cursor > lastDotIndex;
        return true;
    }

    private CompletionItem[] GetContextValues (ValueContext ctx) => ctx.Type switch {
        ValueContextType.Expression => provider.GetExpressions(),
        ValueContextType.Constant => GetConstantValues(ctx),
        ValueContextType.Endpoint => GetEndpointValues(ctx),
        ValueContextType.Resource => provider.GetResources(ctx.SubType ?? ""),
        ValueContextType.Actor => provider.GetActors(ctx.SubType ?? ""),
        ValueContextType.Appearance when FindActorId() is { } id => provider.GetAppearances(id),
        ValueContextType.Appearance when !string.IsNullOrEmpty(ctx.SubType) => provider.GetAppearances(ctx.SubType),
        _ => Array.Empty<CompletionItem>()
    };

    private string? FindActorId ()
    {
        foreach (var param in command.Model.Parameters)
            if (meta.FindParameter(command.Meta.Id, param.Identifier) is not { } paramMeta) continue;
            else if (paramMeta.ValueContext is null) continue;
            else if (paramMeta.ValueContext.ElementAtOrDefault(0)?.Type == ValueContextType.Actor)
                return paramMeta.ValueContainerType == ValueContainerType.Named
                    ? GetNamedValue(param.Value, true)
                    : line.Extract(param.Value);
            else if (paramMeta.ValueContext.ElementAtOrDefault(1)?.Type == ValueContextType.Actor)
                return GetNamedValue(param.Value, false);
        return null;
    }

    private string? GetNamedValue (MixedValue value, bool name)
    {
        var valueText = line.Extract(value);
        var dotIndex = valueText.LastIndexOf('.');
        if (dotIndex < 0 && name) return valueText;
        if (dotIndex < 0 && !name) return null;
        var result = name ? valueText[..dotIndex] : valueText[(dotIndex + 1)..];
        return string.IsNullOrEmpty(result) ? null : result;
    }

    private CompletionItem[] GetConstantValues (ValueContext context)
    {
        var names = ConstantEvaluator.EvaluateNames(context.SubType ?? "", scriptName, GetParamValue);
        return names.SelectMany(provider.GetConstants).ToArray();

        string? GetParamValue (string id, int? index)
        {
            foreach (var param in command.Model.Parameters)
                if (meta.FindParameter(command.Meta.Id, param.Identifier)?.Id == id)
                    return index.HasValue ? GetNamedValue(param.Value, index == 0) : line.Extract(param.Value);
            return null;
        }
    }

    private CompletionItem[] GetEndpointValues (ValueContext context)
    {
        if (context.SubType == Constants.EndpointScript)
            return provider.GetScriptEndpoints(endpoints.GetAllScriptNames());
        var script = GetNamedValue(param.Model.Value, true) ?? scriptName;
        return provider.GetLabelEndpoints(endpoints.GetLabelsInScript(script));
    }
}
