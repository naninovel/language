using System;
using System.Collections.Generic;
using System.Linq;
using Naninovel.Metadata;

namespace Naninovel.Language;

internal class CompletionProvider
{
    private readonly CompletionItem[] booleans;
    private readonly CompletionItem[] commands;
    private readonly CompletionItem[] expressions;
    private readonly Dictionary<string, CompletionItem[]> actorsByType;
    private readonly Dictionary<string, CompletionItem[]> appearancesByActorId;
    private readonly Dictionary<string, CompletionItem[]> parametersByCommandId;
    private readonly Dictionary<string, CompletionItem[]> constantsByName;
    private readonly Dictionary<string, CompletionItem[]> resourcesByType;

    public CompletionProvider (MetadataProvider meta)
    {
        booleans = new[] { CreateBoolean("true"), CreateBoolean("false") };
        commands = meta.Commands.Select(CreateCommand).ToArray();
        expressions = meta.Variables.Select(CreateVariable).Concat(meta.Functions.Select(CreateFunction)).ToArray();
        actorsByType = Map(meta.Actors, a => a.Type, CreateActor);
        actorsByType[Constants.WildcardType] = actorsByType.SelectMany(kv => kv.Value).ToArray();
        appearancesByActorId = Map(meta.Actors, a => a.Id, a => a.Appearances.Select(CreateAppearance));
        parametersByCommandId = Map(meta.Commands, c => c.Id, c => c.Parameters.Select(CreateParameter));
        constantsByName = Map(meta.Constants, c => c.Name, c => c.Values.Select(CreateConstant));
        resourcesByType = Map(meta.Resources, r => r.Type, CreateResource);
    }

    public CompletionItem[] GetBooleans () => booleans;
    public CompletionItem[] GetCommands () => commands;
    public CompletionItem[] GetExpressions () => expressions;
    public CompletionItem[] GetActors (string type) => GetOrEmpty(actorsByType, type);
    public CompletionItem[] GetAppearances (string actorId) => GetOrEmpty(appearancesByActorId, actorId);
    public CompletionItem[] GetParameters (string commandId) => GetOrEmpty(parametersByCommandId, commandId);
    public CompletionItem[] GetConstants (string name) => GetOrEmpty(constantsByName, name);
    public CompletionItem[] GetResources (string type) => GetOrEmpty(resourcesByType, type);

    private static Dictionary<string, CompletionItem[]> Map<T> (IEnumerable<T> items,
        Func<T, string> getKey, Func<T, CompletionItem> getItem)
    {
        return items.GroupBy(getKey, getItem).ToDictionary(g => g.Key, g => g.ToArray());
    }

    private static Dictionary<string, CompletionItem[]> Map<T> (IEnumerable<T> items,
        Func<T, string> getKey, Func<T, IEnumerable<CompletionItem>> getItems)
    {
        return items.GroupBy(getKey, getItems).ToDictionary(g => g.Key, g => g.SelectMany(i => i).ToArray());
    }

    private static CompletionItem[] GetOrEmpty (Dictionary<string, CompletionItem[]> map, string key)
    {
        if (map.TryGetValue(key, out var items)) return items;
        return Array.Empty<CompletionItem>();
    }

    private static CompletionItem CreateBoolean (string value) => new() {
        Label = value,
        Kind = CompletionItemKind.EnumMember,
        CommitCharacters = new[] { " " }
    };

    private static CompletionItem CreateCommand (Command command) => new() {
        Label = command.Label,
        Kind = CompletionItemKind.Function,
        Documentation = new MarkupContent(command.Summary ?? ""),
        CommitCharacters = new[] { " " }
    };

    private static CompletionItem CreateParameter (Parameter param) => new() {
        Label = param.Label,
        Kind = CompletionItemKind.Field,
        Detail = string.IsNullOrEmpty(param.DefaultValue) ? "" : $"Default value: {param.DefaultValue}",
        Documentation = new MarkupContent(param.Summary ?? ""),
        CommitCharacters = new[] { ":" }
    };

    private static CompletionItem CreateActor (Actor actor) => new() {
        Label = actor.Id,
        Kind = CompletionItemKind.Value,
        CommitCharacters = new[] { " ", ".", ",", ":" },
        Detail = actor.Description
    };

    private static CompletionItem CreateAppearance (string appearance) => new() {
        Label = appearance,
        Kind = CompletionItemKind.Value,
        CommitCharacters = new[] { " ", ".", ",", ":" }
    };

    private static CompletionItem CreateResource (Resource resource) => new() {
        Label = resource.Path,
        Kind = CompletionItemKind.Value,
        CommitCharacters = new[] { " ", ".", "," }
    };

    private static CompletionItem CreateConstant (string name) => new() {
        Label = name,
        Kind = CompletionItemKind.EnumMember,
        CommitCharacters = new[] { " " }
    };

    private static CompletionItem CreateVariable (string var) => new() {
        Label = var,
        Kind = CompletionItemKind.Variable,
        CommitCharacters = new[] { " " }
    };

    private static CompletionItem CreateFunction (string func) => new() {
        Label = func,
        Kind = CompletionItemKind.Method,
        CommitCharacters = new[] { " " },
        InsertText = func + "()"
    };
}
