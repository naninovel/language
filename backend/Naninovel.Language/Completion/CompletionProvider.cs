using Naninovel.Metadata;

namespace Naninovel.Language;

internal class CompletionProvider
{
    private readonly CompletionItem[] booleans = [CreateBoolean("true"), CreateBoolean("false")];
    private CompletionItem[] commands = [];
    private CompletionItem[] expressions = [];
    private Dictionary<string, CompletionItem[]> actorsByType = [];
    private Dictionary<string, CompletionItem[]> appearancesByActorId = [];
    private Dictionary<string, CompletionItem[]> parametersByCommandId = [];
    private Dictionary<string, CompletionItem[]> constantsByName = [];
    private Dictionary<string, CompletionItem[]> resourcesByType = [];

    public void Update (MetadataProvider meta)
    {
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

    public CompletionItem[] GetScriptEndpoints (IEnumerable<string> scriptNames) =>
        scriptNames.Select(CreateEndpointScript).ToArray();
    public CompletionItem[] GetLabelEndpoints (IEnumerable<string> labels) =>
        labels.Select(CreateEndpointLabel).ToArray();

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
        CommitCharacters = [" "]
    };

    private static CompletionItem CreateCommand (Command command) => new() {
        Label = command.Label,
        Kind = CompletionItemKind.Function,
        Documentation = new MarkupContent(command.Summary ?? ""),
        CommitCharacters = [" "]
    };

    private static CompletionItem CreateParameter (Parameter param) => new() {
        Label = param.Label,
        Kind = CompletionItemKind.Field,
        Detail = string.IsNullOrEmpty(param.DefaultValue) ? "" : $"Default value: {param.DefaultValue}",
        Documentation = new MarkupContent(param.Summary ?? ""),
        CommitCharacters = [":"]
    };

    private static CompletionItem CreateActor (Actor actor) => new() {
        Label = actor.Id,
        Kind = CompletionItemKind.Value,
        CommitCharacters = [" ", ".", ",", ":"],
        Detail = actor.Description
    };

    private static CompletionItem CreateAppearance (string appearance) => new() {
        Label = appearance,
        Kind = CompletionItemKind.Value,
        CommitCharacters = [" ", ".", ",", ":"]
    };

    private static CompletionItem CreateResource (Resource resource) => new() {
        Label = resource.Path,
        Kind = CompletionItemKind.Value,
        CommitCharacters = [" ", ".", ","]
    };

    private static CompletionItem CreateConstant (string name) => new() {
        Label = name,
        Kind = CompletionItemKind.EnumMember,
        CommitCharacters = [" "]
    };

    private static CompletionItem CreateVariable (string var) => new() {
        Label = var,
        Kind = CompletionItemKind.Variable,
        CommitCharacters = [" "]
    };

    private static CompletionItem CreateFunction (string func) => new() {
        Label = func,
        Kind = CompletionItemKind.Method,
        CommitCharacters = [" "],
        InsertText = func + "()"
    };

    private static CompletionItem CreateEndpointScript (string scriptName) => new() {
        Label = scriptName,
        Kind = CompletionItemKind.EnumMember,
        CommitCharacters = [" ", "."]
    };

    private static CompletionItem CreateEndpointLabel (string label) => new() {
        Label = label,
        Kind = CompletionItemKind.EnumMember,
        CommitCharacters = [" "]
    };
}
