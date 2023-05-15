using System.Collections.Generic;
using Naninovel.Parsing;

namespace Naninovel.Language;

internal class EndpointRegistry
{
    private readonly Dictionary<string, List<EndpointRegistryItem>> uriToItems = new();

    public bool Contains (string uri, string label)
    {
        foreach (var item in GetOrAddItems(uri))
            if (item.Label == label)
                return true;
        return false;
    }

    public bool IsUsed (string uri, string? label = null)
    {
        return false;
    }

    public void Add (string uri, IReadOnlyList<DocumentLine> lines, LineRange? range = null)
    {
        var items = GetOrAddItems(uri);
        var (start, end) = range.HasValue ? (range.Value.Start, range.Value.End) : (0, lines.Count - 1);
        for (var i = start; i <= end; i++)
            if (lines[i].Script is LabelLine labelLine)
                items.Add(new(labelLine.Label, i));
    }

    public void Remove (string uri, LineRange? range = null)
    {
        var items = GetOrAddItems(uri);
        if (!range.HasValue) items.Clear();
        else
            for (var i = items.Count - 1; i >= 0; i--)
                if (items[i].LineIndex >= range.Value.Start && items[i].LineIndex <= range.Value.End)
                    items.RemoveAt(i);
    }

    private List<EndpointRegistryItem> GetOrAddItems (string uri)
    {
        return uriToItems.TryGetValue(uri, out var items) ? items : uriToItems[uri] = new();
    }
}
