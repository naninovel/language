using System.Collections.Generic;
using System.IO;
using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

internal class EndpointRegistry
{
    private readonly Dictionary<string, List<EndpointRegistryItem>> existing = new();
    private readonly Dictionary<string, List<EndpointRegistryItem>> used = new();
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
            if (item.Label == match)
                return true;
        return false;
    }

    public void Add (string uri, IReadOnlyList<DocumentLine> lines, LineRange? range = null)
    {
        var (start, end) = range.HasValue ? (range.Value.Start, range.Value.End) : (0, lines.Count - 1);
        for (var i = start; i <= end; i++)
            ProcessAddedLine(uri, lines[i].Script, i);
    }

    public void Remove (string uri, LineRange? range = null)
    {
        var items = GetExisting(uri);
        if (!range.HasValue) items.Clear();
        else
            for (var i = items.Count - 1; i >= 0; i--)
                if (items[i].LineIndex >= range.Value.Start && items[i].LineIndex <= range.Value.End)
                    items.RemoveAt(i);
    }

    private List<EndpointRegistryItem> GetExisting (string uri)
    {
        return existing.TryGetValue(uri, out var items) ? items : existing[uri] = new();
    }

    private List<EndpointRegistryItem> GetUsed (string uri)
    {
        return used.TryGetValue(uri, out var items) ? items : used[uri] = new();
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
            GetUsed(point.Script ?? Path.GetFileNameWithoutExtension(uri)).Add(new(point.Label ?? "", lineIndex));
    }
}
