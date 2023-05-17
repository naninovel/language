using System;
using System.Collections.Generic;

namespace Naninovel.Language;

internal class DiagnosticRegistry
{
    private readonly Dictionary<string, List<DiagnosticRegistryItem>> uriToItems = new();
    private readonly List<DiagnosticRegistryItem> items = new();
    private readonly List<Diagnostic> diagnostics = new();

    public IReadOnlyList<Diagnostic> GetDiagnostics (string uri)
    {
        diagnostics.Clear();
        foreach (var item in GetOrAddItems(uri))
            diagnostics.Add(item.Diagnostic);
        return diagnostics.Count > 0 ? diagnostics.ToArray() : Array.Empty<Diagnostic>();
    }

    public void Clear ()
    {
        foreach (var (_, items) in uriToItems)
            items.Clear();
    }

    public void Add (string uri, DiagnosticRegistryItem item)
    {
        GetOrAddItems(uri).Add(item);
    }

    public void Remove (string uri, Predicate<DiagnosticRegistryItem>? predicate = null)
    {
        if (predicate is null) GetOrAddItems(uri).Clear();
        else GetOrAddItems(uri).RemoveAll(predicate);
    }

    public IReadOnlyList<DiagnosticRegistryItem> Find (string uri, Predicate<DiagnosticRegistryItem> predicate)
    {
        items.Clear();
        foreach (var item in GetOrAddItems(uri))
            if (predicate(item))
                items.Add(item);
        return items.Count > 0 ? items.ToArray() : Array.Empty<DiagnosticRegistryItem>();
    }

    private List<DiagnosticRegistryItem> GetOrAddItems (string uri)
    {
        return uriToItems.TryGetValue(uri, out var items) ? items : uriToItems[uri] = new();
    }
}
