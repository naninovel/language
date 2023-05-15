using System;
using System.Linq;
using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

internal class CommandCompletionHandler
{
    private readonly MetadataProvider meta;
    private readonly CompletionProvider provider;

    private int cursor => position.Character;
    private char charBehindCursor => line.GetCharBehindCursor(position);
    private Parsing.Command command = null!;
    private Position position;
    private DocumentLine line;
    private string scriptName = string.Empty;

    public CommandCompletionHandler (MetadataProvider meta, CompletionProvider provider)
    {
        this.meta = meta;
        this.provider = provider;
    }

    public CompletionItem[] Handle (Parsing.Command command, in Position position, in DocumentLine line, string scriptName)
    {
        ResetState(command, position, line, scriptName);
        if (ShouldCompleteCommandId())
            return provider.GetCommands();
        if (meta.FindCommand(command.Identifier) is not { } commandMeta)
            return Array.Empty<CompletionItem>();
        if (ShouldCompleteNamelessValue(commandMeta))
            return GetNamelessValues(commandMeta);
        if (ShouldCompleteNamedValue())
            return GetNamedValues(commandMeta);
        return provider.GetParameters(commandMeta.Id);
    }

    private void ResetState (Parsing.Command command, in Position position, in DocumentLine line, string scriptName)
    {
        this.line = line;
        this.command = command;
        this.position = position;
        this.scriptName = scriptName;
    }

    private bool IsCursorOver (ILineComponent content) => line.IsCursorOver(content, position);

    private bool ShouldCompleteCommandId ()
    {
        return IsCursorOver(command.Identifier) ||
               charBehindCursor == Identifiers.CommandLine[0] ||
               charBehindCursor == Identifiers.InlinedOpen[0];
    }

    private bool ShouldCompleteNamelessValue (Metadata.Command commandMeta)
    {
        return command.Parameters.Any(p => p.Nameless && IsCursorOver(p)) ||
               commandMeta.Parameters.Any(p => p.Nameless) &&
               line.TryResolve(command.Identifier, out var idRange) &&
               cursor == idRange.End + 2;
    }

    private bool ShouldCompleteNamedValue ()
    {
        return command.Parameters.Any(p => IsCursorOver(p.Value)) ||
               charBehindCursor == Identifiers.ParameterAssign[0];
    }

    private CompletionItem[] GetNamelessValues (Metadata.Command commandMeta)
    {
        var paramMeta = commandMeta.Parameters.FirstOrDefault(p => p.Nameless);
        if (paramMeta is null) return Array.Empty<CompletionItem>();
        return GetParameterValues(commandMeta, paramMeta);
    }

    private CompletionItem[] GetNamedValues (Metadata.Command commandMeta)
    {
        var param = command.Parameters.First(IsCursorOver);
        var paramMeta = meta.FindParameter(commandMeta.Id, param.Identifier);
        if (paramMeta is null) return Array.Empty<CompletionItem>();
        return GetParameterValues(commandMeta, paramMeta);
    }

    private CompletionItem[] GetParameterValues (Metadata.Command commandMeta, Metadata.Parameter paramMeta)
    {
        if (ShouldCompleteExpressions())
            return provider.GetExpressions();
        if (paramMeta.ValueType == Metadata.ValueType.Boolean)
            return provider.GetBooleans();
        if (FindValueContext(commandMeta, paramMeta) is { } context)
            return GetContextValues(context, commandMeta);
        return Array.Empty<CompletionItem>();
    }

    private bool ShouldCompleteExpressions ()
    {
        return command.Parameters.Any(p => p.Value.OfType<Expression>().Any(IsCursorOver)) &&
               charBehindCursor != Identifiers.ExpressionClose[0];
    }

    private ValueContext? FindValueContext (Metadata.Command commandMeta, Metadata.Parameter paramMeta)
    {
        var value = command.Parameters.FirstOrDefault(IsCursorOver)?.Value;
        if (value is null || paramMeta.ValueContainerType != ValueContainerType.Named)
            return paramMeta.ValueContext?.ElementAtOrDefault(0);
        var lastDotIndex = line.GetLineRange(value).Start + line.Extract(value).LastIndexOf('.');
        if (lastDotIndex < 0 || cursor <= lastDotIndex) return paramMeta.ValueContext?.ElementAtOrDefault(0);
        return paramMeta.ValueContext?.ElementAtOrDefault(1);
    }

    public CompletionItem[] GetContextValues (ValueContext context, Metadata.Command commandMeta)
    {
        return context.Type switch {
            ValueContextType.Expression => provider.GetExpressions(),
            ValueContextType.Constant => GetConstantValues(context, commandMeta),
            ValueContextType.Resource => provider.GetResources(context.SubType ?? ""),
            ValueContextType.Actor => provider.GetActors(context.SubType ?? ""),
            ValueContextType.Appearance when FindActorId(commandMeta) is { } id => provider.GetAppearances(id),
            ValueContextType.Appearance when !string.IsNullOrEmpty(context.SubType) => provider.GetAppearances(context.SubType),
            _ => Array.Empty<CompletionItem>()
        };
    }

    private string? FindActorId (Metadata.Command commandMeta)
    {
        foreach (var param in command.Parameters)
            if (meta.FindParameter(commandMeta.Id, param.Identifier) is not { } paramMeta) continue;
            else if (paramMeta.ValueContext?.ElementAtOrDefault(0)?.Type == ValueContextType.Actor)
                return paramMeta.ValueContainerType == ValueContainerType.Named
                    ? GetNamedValue(param.Value, true)
                    : line.Extract(param.Value);
            else if (paramMeta.ValueContext?.ElementAtOrDefault(1)?.Type == ValueContextType.Actor)
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

    private CompletionItem[] GetConstantValues (ValueContext context, Metadata.Command commandMeta)
    {
        var names = ConstantEvaluator.EvaluateNames(context.SubType ?? "", scriptName, GetParamValue);
        return names.SelectMany(provider.GetConstants).ToArray();

        string? GetParamValue (string id, int? index)
        {
            foreach (var param in command.Parameters)
                if (meta.FindParameter(commandMeta.Id, param.Identifier) is { } paramMeta && paramMeta.Id == id)
                    return index.HasValue ? GetNamedValue(param.Value, index == 0) : line.Extract(param.Value);
            return null;
        }
    }
}
