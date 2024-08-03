using System.Collections.Immutable;
using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

public class EndpointRegistry (IMetadata meta, IDocumentRegistry docs)
    : IEndpointRegistry, IDocumentObserver
{
    private readonly HashSet<string> scriptPaths = [];
    private readonly Dictionary<LineLocation, string> labels = [];
    private readonly Dictionary<QualifiedLabel, HashSet<LineLocation>> labelLocations = [];
    private readonly Dictionary<LineLocation, QualifiedEndpoint> navigators = [];
    private readonly Dictionary<QualifiedEndpoint, HashSet<LineLocation>> navigatorLocations = [];
    private readonly EndpointResolver resolver = new(meta);

    public void HandleDocumentAdded (string uri)
    {
        var path = docs.ResolvePath(uri);
        var doc = docs.Get(uri);
        scriptPaths.Add(path);
        HandleLinesAdded(uri, path, doc, new(0, doc.LineCount - 1));
    }

    public void HandleDocumentRemoved (string uri)
    {
        var path = docs.ResolvePath(uri);
        scriptPaths.Remove(path);
        HandleLinesRemoved(uri, path, new(0, docs.Get(uri).LineCount - 1));
    }

    public void HandleDocumentChanging (string uri, LineRange range)
    {
        HandleLinesRemoved(uri, docs.ResolvePath(uri), range);
    }

    public void HandleDocumentChanged (string uri, LineRange range)
    {
        HandleLinesAdded(uri, docs.ResolvePath(uri), docs.Get(uri), range);
    }

    public bool ScriptExist (string scriptPath)
    {
        return scriptPaths.Contains(scriptPath);
    }

    public bool LabelExist (in QualifiedLabel label)
    {
        return labelLocations.ContainsKey(label);
    }

    public bool NavigatorExist (in QualifiedEndpoint endpoint)
    {
        return navigatorLocations.ContainsKey(endpoint);
    }

    public IReadOnlySet<LineLocation> GetLabelLocations (in QualifiedLabel label)
    {
        return labelLocations.TryGetValue(label, out var locs) ? locs : ImmutableHashSet<LineLocation>.Empty;
    }

    public IReadOnlySet<LineLocation> GetNavigatorLocations (in QualifiedEndpoint endpoint)
    {
        return navigatorLocations.TryGetValue(endpoint, out var locs) ? locs : ImmutableHashSet<LineLocation>.Empty;
    }

    public IReadOnlySet<string> GetAllScriptPaths () => scriptPaths;

    public IReadOnlySet<string> GetLabelsInScript (string scriptPath)
    {
        var result = new HashSet<string>();
        foreach (var (loc, label) in labels)
            if (docs.ResolvePath(loc.DocumentUri).Equals(scriptPath, StringComparison.Ordinal))
                result.Add(label);
        return result;
    }

    private void HandleLinesAdded (string uri, string path, IDocument doc, in LineRange range)
    {
        for (var i = range.Start; i <= range.End; i++)
            if (doc[i].Script is LabelLine labelLine)
                HandleLabelAdded(i, labelLine.Label);
            else if (doc[i].Script is CommandLine commandLine)
                HandleCommandAdded(commandLine.Command, i);
            else if (doc[i].Script is GenericLine genericLine)
                foreach (var content in genericLine.Content)
                    if (content is InlinedCommand inlined)
                        HandleCommandAdded(inlined.Command, i);

        void HandleLabelAdded (int line, string label)
        {
            labels[new(uri, line)] = label;
            GetOrAddLabelLocations(new(path, label)).Add(new(uri, line));
        }

        void HandleCommandAdded (Parsing.Command command, int line)
        {
            if (resolver.TryResolve(command, out var point))
                HandleNavigatorAdded(line, new(point.ScriptPath ?? path, point.Label));
        }

        void HandleNavigatorAdded (int line, in QualifiedEndpoint endpoint)
        {
            navigators[new(uri, line)] = endpoint;
            GetOrAddNavigatorLocations(endpoint).Add(new(uri, line));
        }
    }

    private void HandleLinesRemoved (string uri, string path, in LineRange range)
    {
        for (int i = range.Start; i <= range.End; i++)
            if (labels.ContainsKey(new(uri, i))) HandleLabelRemoved(i);
            else if (navigators.ContainsKey(new(uri, i))) HandleNavigatorRemoved(i);

        void HandleLabelRemoved (int line)
        {
            var location = new LineLocation(uri, line);
            var label = labels[location];
            var key = new QualifiedLabel(path, label);
            var locations = GetOrAddLabelLocations(key);
            locations.Remove(location);
            if (locations.Count > 0) return;
            labelLocations.Remove(key);
            labels.Remove(location);
        }

        void HandleNavigatorRemoved (int line)
        {
            var location = new LineLocation(uri, line);
            var navigator = navigators[location];
            var locations = GetOrAddNavigatorLocations(navigator);
            locations.Remove(location);
            if (locations.Count > 0) return;
            navigatorLocations.Remove(navigator);
            navigators.Remove(location);
        }
    }

    private HashSet<LineLocation> GetOrAddLabelLocations (in QualifiedLabel key)
    {
        return labelLocations.TryGetValue(key, out var locs) ? locs : labelLocations[key] = new();
    }

    private HashSet<LineLocation> GetOrAddNavigatorLocations (in QualifiedEndpoint key)
    {
        return navigatorLocations.TryGetValue(key, out var locs) ? locs : navigatorLocations[key] = new();
    }
}
