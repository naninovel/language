using System.Collections.Generic;
using Naninovel.Metadata;
using Naninovel.Parsing;
using static Naninovel.Language.Common;

namespace Naninovel.Language;

internal class WipEndpointRegistry
{
    private readonly Dictionary<string, HashSet<EndpointLabel>> nameToLabels = new();
    private readonly Dictionary<string, HashSet<EndpointNavigator>> nameToNavigators = new();
    private readonly EndpointResolver resolver;

    public WipEndpointRegistry (MetadataProvider metaProvider)
    {
        resolver = new(metaProvider);
    }

    public bool Contains (string uriOrName, string? label = null)
    {
        var labels = GetLabels(uriOrName);
        if (string.IsNullOrEmpty(label))
            return labels.Contains(EndpointLabel.CreateScriptKey());
        return labels.Contains(EndpointLabel.CreateLabelKey(label));
    }

    public bool IsUsed (string uriOrName, string? label = null)
    {
        var name = ToScriptName(uriOrName);
        foreach (var (_, navs) in nameToNavigators)
            if (IsUsed(navs, name, label))
                return true;
        return false;
    }

    public void Add (string uri, IReadOnlyList<DocumentLine> lines, LineRange? range = null)
    {
        GetLabels(uri).Add(EndpointLabel.CreateScriptKey());
        var (start, end) = range.HasValue ? (range.Value.Start, range.Value.End) : (0, lines.Count - 1);
        for (var i = start; i <= end; i++)
            ProcessAddedLine(uri, lines[i].Script, i);
    }

    public void Remove (string uri, LineRange? range = null)
    {
        var labels = GetLabels(uri);
        var navs = GetNavigators(uri);

        if (!range.HasValue)
        {
            labels.Clear();
            navs.Clear();
            return;
        }

        for (int i = range.Value.Start; i < range.Value.End; i++)
        {
            labels.Remove(EndpointLabel.CreateLineKey(i));
            navs.Remove(EndpointNavigator.CreateLineKey(i));
        }
    }

    private void ProcessAddedLine (string uri, IScriptLine line, int index)
    {
        if (line is LabelLine labelLine)
            GetLabels(uri).Add(new(index, labelLine.Label));
        else if (line is CommandLine commandLine)
            ProcessAddedCommand(commandLine.Command, uri, index);
        else if (line is GenericLine genericLine)
            foreach (var content in genericLine.Content)
                if (content is InlinedCommand inlined)
                    ProcessAddedCommand(inlined.Command, uri, index);
    }

    private void ProcessAddedCommand (Parsing.Command command, string uri, int lineIndex)
    {
        if (resolver.TryResolve(command, out var point))
            GetNavigators(uri).Add(new(lineIndex, point.Script ?? ToScriptName(uri), point.Label ?? ""));
    }

    private HashSet<EndpointLabel> GetLabels (string uriOrName)
    {
        var name = ToScriptName(uriOrName);
        return nameToLabels.TryGetValue(name, out var labels) ? labels : nameToLabels[name] = new();
    }

    private HashSet<EndpointNavigator> GetNavigators (string uriOrName)
    {
        var name = ToScriptName(uriOrName);
        return nameToNavigators.TryGetValue(name, out var labels) ? labels : nameToNavigators[name] = new();
    }

    private bool IsUsed (IReadOnlySet<EndpointNavigator> navs, string name, string? label)
    {
        if (label == null) return navs.Contains(EndpointNavigator.CreateScriptKey(name));
        return navs.Contains(EndpointNavigator.CreateScriptAndLabelKey(name, label));
    }
}
