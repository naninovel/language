using System.Collections.Generic;
using Naninovel.Metadata;
using Naninovel.Parsing;
using static Naninovel.Language.Common;

namespace Naninovel.Language;

internal class EndpointRegistry
{
    private readonly Dictionary<string, List<EndpointRegistryItem>> uriToExisting = new();
    private readonly Dictionary<string, List<EndpointRegistryItem>> nameToUsed = new();
    private readonly EndpointResolver resolver;

    public EndpointRegistry (MetadataProvider metaProvider)
    {
        resolver = new(metaProvider);
    }

    public bool Contains (string uri, string label)
    {
        foreach (var item in GetExisting(uri))
            if (item.Label == label)
                return true;
        return false;
    }

    public bool IsUsed (string name, string? label = null)
    {
        var match = label ?? "";
        foreach (var item in GetUsed(name))
            if (string.IsNullOrEmpty(label) || item.Label == match)
                return true;
        return false;
    }

    public void Add (string uri, IReadOnlyList<DocumentLine> lines, LineRange? range = null)
    {
        EnsureScriptExist(uri);
        var (start, end) = range.HasValue ? (range.Value.Start, range.Value.End) : (0, lines.Count - 1);
        for (var i = start; i <= end; i++)
            ProcessAddedLine(uri, lines[i].Script, i);
    }

    public void Remove (string uri, LineRange? range = null)
    {
        var existing = GetExisting(uri);
        if (!range.HasValue) existing.Clear();
        else
            for (var i = existing.Count - 1; i >= 0; i--)
                if (existing[i].LineIndex >= range.Value.Start && existing[i].LineIndex <= range.Value.End)
                    existing.RemoveAt(i);
    }

    private List<EndpointRegistryItem> GetExisting (string uri)
    {
        return uriToExisting.TryGetValue(uri, out var items) ? items : uriToExisting[uri] = new();
    }

    private List<EndpointRegistryItem> GetUsed (string uriOrName)
    {
        var name = ToEndpointName(uriOrName);
        return nameToUsed.TryGetValue(name, out var items) ? items : nameToUsed[name] = new();
    }

    private void ProcessAddedLine (string uri, IScriptLine line, int index)
    {
        if (line is LabelLine labelLine)
            GetExisting(uri).Add(new(labelLine.Label, index));
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
            GetUsed(point.Script ?? uri).Add(new(point.Label ?? "", lineIndex));
    }

    private void EnsureScriptExist (string uri)
    {
        var items = GetExisting(uri);
        if (items.Count == 0) items.Add(new("", -1));
    }
}
