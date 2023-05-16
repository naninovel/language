using System;
using System.Collections.Generic;

namespace Naninovel.Language;

internal class DiagnosticRegistry
{
    private readonly Dictionary<string, List<DiagnosticRegistryItem>> uriToItems = new();
    private readonly List<Diagnostic> diagnostics = new();
    private readonly List<DiagnosticRegistryItem> items = new();

    public void Publish (IDiagnosticPublisher publisher)
    {
        foreach (var (uri, items) in uriToItems)
        {
            diagnostics.Clear();
            foreach (var item in items)
                diagnostics.Add(item.Diagnostic);
            publisher.PublishDiagnostics(uri, diagnostics);
        }
    }

    public void Clear ()
    {
        foreach (var (_, items) in uriToItems)
            items.Clear();
    }

    public void Add (string uri, DiagnosticRegistryItem item)
    {
        GetItems(uri).Add(item);
    }

    public void Remove (string uri, Predicate<DiagnosticRegistryItem>? predicate = null)
    {
        if (predicate is null) GetItems(uri).Clear();
        else GetItems(uri).RemoveAll(predicate);
    }

    public IReadOnlyList<DiagnosticRegistryItem> Find (string uri, Predicate<DiagnosticRegistryItem> predicate)
    {
        items.Clear();
        foreach (var item in GetItems(uri))
            if (predicate(item))
                items.Add(item);
        return items.Count > 0 ? items.ToArray() : Array.Empty<DiagnosticRegistryItem>();
    }

    private List<DiagnosticRegistryItem> GetItems (string uri)
    {
        return uriToItems.TryGetValue(uri, out var items) ? items : uriToItems[uri] = new();
    }
}
