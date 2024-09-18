using System.Collections.Immutable;
using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

public class EndpointRegistry (IMetadata meta, IDocumentRegistry docs)
    : IEndpointRegistry, IDocumentObserver
{
    private readonly HashSet<string> scriptPaths = [];
    private readonly Dictionary<LineLocation, string> labelByLoc = [];
    private readonly Dictionary<QualifiedLabel, HashSet<LineLocation>> locsByLabel = [];
    private readonly Dictionary<LineLocation, QualifiedEndpoint> navigatorByLoc = [];
    private readonly Dictionary<QualifiedEndpoint, HashSet<LineLocation>> locsByNavigator = [];
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
        return locsByLabel.ContainsKey(label);
    }

    public bool NavigatorExist (in QualifiedEndpoint endpoint)
    {
        return locsByNavigator.ContainsKey(endpoint);
    }

    public IReadOnlySet<LineLocation> GetLabelLocations (in QualifiedLabel label)
    {
        return locsByLabel.TryGetValue(label, out var locs) ? locs : ImmutableHashSet<LineLocation>.Empty;
    }

    public IReadOnlySet<LineLocation> GetNavigatorLocations (in QualifiedEndpoint endpoint)
    {
        return locsByNavigator.TryGetValue(endpoint, out var locs) ? locs : ImmutableHashSet<LineLocation>.Empty;
    }

    public IReadOnlyCollection<QualifiedEndpoint> GetAllNavigators ()
    {
        return locsByNavigator.Keys;
    }

    public IReadOnlySet<string> GetAllScriptPaths () => scriptPaths;

    public IReadOnlySet<string> GetLabelsInScript (string scriptPath)
    {
        var result = new HashSet<string>();
        foreach (var (loc, label) in labelByLoc)
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
            labelByLoc[new(uri, line)] = label;
            GetOrAddLabelLocations(new(path, label)).Add(new(uri, line));
        }

        void HandleCommandAdded (Parsing.Command command, int line)
        {
            if (resolver.TryResolve(command, out var point))
                HandleNavigatorAdded(line, new(point.ScriptPath ?? path, point.Label));
        }

        void HandleNavigatorAdded (int line, in QualifiedEndpoint endpoint)
        {
            navigatorByLoc[new(uri, line)] = endpoint;
            GetOrAddNavigatorLocations(endpoint).Add(new(uri, line));
        }
    }

    private void HandleLinesRemoved (string uri, string path, in LineRange range)
    {
        for (int i = range.Start; i <= range.End; i++)
            if (labelByLoc.ContainsKey(new(uri, i))) HandleLabelRemoved(i);
            else if (navigatorByLoc.ContainsKey(new(uri, i))) HandleNavigatorRemoved(i);

        void HandleLabelRemoved (int line)
        {
            var location = new LineLocation(uri, line);
            var label = labelByLoc[location];
            var key = new QualifiedLabel(path, label);
            var locations = GetOrAddLabelLocations(key);
            locations.Remove(location);
            if (locations.Count > 0) return;
            locsByLabel.Remove(key);
            labelByLoc.Remove(location);
        }

        void HandleNavigatorRemoved (int line)
        {
            var location = new LineLocation(uri, line);
            var navigator = navigatorByLoc[location];
            var locations = GetOrAddNavigatorLocations(navigator);
            locations.Remove(location);
            if (locations.Count > 0) return;
            locsByNavigator.Remove(navigator);
            navigatorByLoc.Remove(location);
        }
    }

    private HashSet<LineLocation> GetOrAddLabelLocations (in QualifiedLabel key)
    {
        return locsByLabel.TryGetValue(key, out var locs) ? locs : locsByLabel[key] = [];
    }

    private HashSet<LineLocation> GetOrAddNavigatorLocations (in QualifiedEndpoint key)
    {
        return locsByNavigator.TryGetValue(key, out var locs) ? locs : locsByNavigator[key] = [];
    }
}
