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
    private char charBehindCursor => position.GetCharBehindCursor(lineText);
    private Parsing.Command command = null!;
    private Position position = null!;
    private string lineText = string.Empty;
    private string scriptName = string.Empty;

    public CommandCompletionHandler (MetadataProvider meta, CompletionProvider provider)
    {
        this.meta = meta;
        this.provider = provider;
    }

    public CompletionItem[] Handle (Parsing.Command command, Position position, string lineText, string scriptName)
    {
        ResetState(command, position, lineText, scriptName);
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

    private void ResetState (Parsing.Command command, Position position, string lineText, string scriptName)
    {
        this.command = command;
        this.position = position;
        this.lineText = lineText;
        this.scriptName = scriptName;
    }

    private bool IsCursorOver (LineContent content) => position.IsCursorOver(content);

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
               cursor == command.Identifier.EndIndex + 2;
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
        return command.Parameters.Any(p => p.Value.Expressions.Any(IsCursorOver)) &&
               charBehindCursor != Identifiers.ExpressionClose[0];
    }

    private ValueContext? FindValueContext (Metadata.Command commandMeta, Metadata.Parameter paramMeta)
    {
        var value = command.Parameters.FirstOrDefault(IsCursorOver)?.Value;
        if (paramMeta.ValueContainerType != ValueContainerType.Named || string.IsNullOrEmpty(value))
            return paramMeta.ValueContext;
        var lastDotIndex = value.StartIndex + value.Text.LastIndexOf('.');
        if (lastDotIndex < 0 || cursor <= lastDotIndex) return paramMeta.ValueContext;
        return paramMeta.NamedValueContext;
    }

    public CompletionItem[] GetContextValues (ValueContext context, Metadata.Command commandMeta)
    {
        return context.Type switch {
            ValueContextType.Expression => provider.GetExpressions(),
            ValueContextType.Constant => GetConstantValues(context, commandMeta),
            ValueContextType.Resource => provider.GetResources(context.SubType),
            ValueContextType.Actor => provider.GetActors(context.SubType),
            ValueContextType.Appearance when FindActorId(commandMeta) is { } id => provider.GetAppearances(id),
            ValueContextType.Appearance when !string.IsNullOrEmpty(context.SubType) => provider.GetAppearances(context.SubType),
            _ => Array.Empty<CompletionItem>()
        };
    }

    private string? FindActorId (Metadata.Command commandMeta)
    {
        foreach (var param in command.Parameters)
            if (meta.FindParameter(commandMeta.Id, param.Identifier) is not { } paramMeta) continue;
            else if (paramMeta.ValueContext?.Type == ValueContextType.Actor)
                return paramMeta.ValueContainerType == ValueContainerType.Named ? GetNamedValue(param.Value, true) : param.Value;
            else if (paramMeta.NamedValueContext?.Type == ValueContextType.Actor)
                return GetNamedValue(param.Value, false);
        return null;
    }

    private static string? GetNamedValue (string value, bool name)
    {
        var dotIndex = value.LastIndexOf('.');
        if (dotIndex < 0 && name) return value;
        if (dotIndex < 0 && !name) return null;
        var result = name ? value[..dotIndex] : value[(dotIndex + 1)..];
        return string.IsNullOrEmpty(result) ? null : result;
    }

    private CompletionItem[] GetConstantValues (ValueContext context, Metadata.Command commandMeta)
    {
        var name = ConstantEvaluator.EvaluateName(context.SubType, scriptName, GetParamValue);
        return provider.GetConstants(name);

        string? GetParamValue (string id, int? index)
        {
            foreach (var param in command.Parameters)
                if (meta.FindParameter(commandMeta.Id, param.Identifier) is { } paramMeta && paramMeta.Id == id)
                    return index.HasValue ? GetNamedValue(param.Value, index == 0) : param.Value;
            return null;
        }
    }
}
