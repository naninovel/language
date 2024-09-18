using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

public class EndpointRenamer (IMetadata meta, IDocumentRegistry docs, IEndpointRegistry ends) : IEndpointRenamer
{
    private readonly record struct EndpointValue (Range Range, (string? Script, string? Label) Raw);

    private const string renameLabelId = "naniscript-rename-label";
    private const string renameScriptId = "naniscript-rename-script";
    private const string renameDirectoryId = "naniscript-rename-directory";

    private readonly EndpointResolver resolver = new(meta);
    private readonly NamedValueParser namedParser = new(meta.Syntax);
    private readonly NamedValueSerializer namedSerde = new(meta.Syntax);

    private readonly IReadOnlyDictionary<string, EditAnnotation> annotations = new Dictionary<string, EditAnnotation> {
        [renameLabelId] = CreateAnnotation("label"),
        [renameScriptId] = CreateAnnotation("script"),
        [renameDirectoryId] = CreateAnnotation("directory")
    };

    public WorkspaceEdit? RenameLabel (string documentUri, string oldLabel, string newLabel)
    {
        var edits = new List<DocumentEdit>();
        var scriptPath = docs.ResolvePath(documentUri);
        foreach (var loc in ends.GetLabelLocations(new(scriptPath, oldLabel)))
            edits.Add(new(loc.DocumentUri, [EditLabel(loc, newLabel)]));
        foreach (var loc in ends.GetNavigatorLocations(new(scriptPath, oldLabel)))
            if (GetEndpointValue(loc) is { } value)
                edits.Add(new(loc.DocumentUri, [EditEndpointLabel(value, newLabel)]));
        return edits.Count > 0 ? new(edits, annotations) : null;
    }

    public WorkspaceEdit? RenameScript (string oldDocumentUri, string newDocumentUri)
    {
        var edits = new List<DocumentEdit>();
        var oldScriptPath = docs.ResolvePath(oldDocumentUri);
        var newScriptPath = docs.ResolvePath(newDocumentUri);
        foreach (var nav in ends.GetAllNavigators())
            if (nav.ScriptPath == oldScriptPath)
                foreach (var loc in ends.GetNavigatorLocations(nav))
                    if (GetEndpointValue(loc) is { } value && value.Raw.Script is not null)
                        edits.Add(new(loc.DocumentUri, [EditEndpointScript(value, newScriptPath)]));
        return edits.Count > 0 ? new(edits, annotations) : null;
    }

    public WorkspaceEdit? RenameDirectory (string oldDirectoryUri, string newDirectoryUri)
    {
        var edits = new List<DocumentEdit>();
        var oldSubPath = docs.ResolvePath(oldDirectoryUri);
        var newSubPath = docs.ResolvePath(newDirectoryUri);
        foreach (var nav in ends.GetAllNavigators())
            if (nav.ScriptPath.StartsWith(oldSubPath))
                foreach (var loc in ends.GetNavigatorLocations(nav))
                    if (GetEndpointValue(loc) is { } value && value.Raw.Script is not null)
                        edits.Add(new(loc.DocumentUri, [EditEndpointDirectory(value, oldSubPath, newSubPath)]));
        return edits.Count > 0 ? new(edits, annotations) : null;
    }

    private static EditAnnotation CreateAnnotation (string subject) => new() {
        Label = "Update Navigation Endpoints",
        Description = $"This will edit existing navigation endpoints (such as @goto commands) to accomodate for the renamed {subject}."
    };

    private EndpointValue? GetEndpointValue (LineLocation loc)
    {
        var doc = docs.Get(loc.DocumentUri);
        var line = doc[loc.LineIndex];
        if (line.Script is CommandLine commandLine)
            return GetEndpointValue(commandLine.Command, line, loc.LineIndex);
        if (line.Script is GenericLine genericLine)
            foreach (var content in genericLine.Content)
                if (content is InlinedCommand inlined)
                    return GetEndpointValue(inlined.Command, line, loc.LineIndex);
        return null;
    }

    private EndpointValue? GetEndpointValue (Parsing.Command command, DocumentLine line, int lineIdx)
    {
        foreach (var param in command.Parameters)
            if (resolver.TryResolve(param, command.Identifier, out _))
                return new(line.GetRange(param.Value, lineIdx), namedParser.Parse(line.Extract(param.Value)));
        return null;
    }

    private TextEdit EditLabel (LineLocation loc, string newLabel)
    {
        var doc = docs.Get(loc.DocumentUri);
        var docLine = doc[loc.LineIndex];
        var labelLine = (LabelLine)docLine.Script;
        var labelRange = docLine.GetRange(labelLine.Label, loc.LineIndex);
        return new(labelRange, newLabel, renameLabelId);
    }

    private TextEdit EditEndpointLabel (EndpointValue endpointValue, string newLabel)
    {
        var (script, _) = endpointValue.Raw;
        var newValue = namedSerde.Serialize(script, newLabel);
        return new(endpointValue.Range, newValue, renameLabelId);
    }

    private TextEdit EditEndpointScript (EndpointValue endpointValue, string newScript)
    {
        var (_, label) = endpointValue.Raw;
        var newValue = namedSerde.Serialize(newScript, label);
        return new(endpointValue.Range, newValue, renameScriptId);
    }

    private TextEdit EditEndpointDirectory (EndpointValue endpointValue, string oldSubPath, string newSubPath)
    {
        var (oldScript, label) = endpointValue.Raw;
        var newScript = newSubPath + oldScript!.GetAfterFirst(oldSubPath);
        var newValue = namedSerde.Serialize(newScript, label);
        return new(endpointValue.Range, newValue, renameDirectoryId);
    }
}
