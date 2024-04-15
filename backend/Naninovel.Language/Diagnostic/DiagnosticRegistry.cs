namespace Naninovel.Language;

internal class DiagnosticRegistry
{
    private readonly Dictionary<string, List<DiagnosticRegistryItem>> uriToItems = new();
    private readonly List<Diagnostic> diags = new();

    public IReadOnlyList<DiagnosticRegistryItem> Get (string uri)
    {
        return uriToItems.TryGetValue(uri, out var items)
            ? items.ToArray()
            : [];
    }

    public IReadOnlyList<Diagnostic> CollectDiagnostics (string uri)
    {
        diags.Clear();
        foreach (var item in GetOrAddItems(uri))
            diags.Add(item.Diagnostic);
        return diags.Count > 0 ? diags.ToArray() : [];
    }

    public void Clear ()
    {
        uriToItems.Clear();
    }

    public void Add (string uri, DiagnosticRegistryItem item)
    {
        GetOrAddItems(uri).Add(item);
    }

    public void Remove (string uri, Predicate<DiagnosticRegistryItem> predicate)
    {
        GetOrAddItems(uri).RemoveAll(predicate);
    }

    private List<DiagnosticRegistryItem> GetOrAddItems (string uri)
    {
        return uriToItems.TryGetValue(uri, out var items) ? items : uriToItems[uri] = new();
    }
}
