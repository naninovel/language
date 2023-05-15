using System.Collections.Generic;

namespace Naninovel.Language;

public class DocumentRegistry : IDocumentRegistry
{
    private readonly Dictionary<string, Document> map = new();
    private readonly DocumentFactory factory = new();
    private readonly DocumentChanger changer = new();
    private readonly EndpointRegistry endpoints = new();

    public IReadOnlyCollection<string> GetAllUris () => map.Keys;

    public bool Contains (string uri, string? label = null)
    {
        if (label is null) return map.ContainsKey(uri);
        return endpoints.Contains(uri, label);
    }

    public bool IsUsed (string uri, string? label = null)
    {
        return endpoints.IsUsed(uri, label);
    }

    public IDocument Get (string uri)
    {
        EnsureDocumentAvailable(uri);
        return map[uri];
    }

    public void Upsert (DocumentInfo doc)
    {
        var document = factory.CreateDocument(doc.Text);
        map[doc.Uri] = document;
        endpoints.Remove(doc.Uri);
        endpoints.Add(doc.Uri, document.Lines);
    }

    public void Remove (string uri)
    {
        map.Remove(uri);
        endpoints.Remove(uri);
    }

    public LineRange Change (string uri, IReadOnlyList<DocumentChange> changes)
    {
        EnsureDocumentAvailable(uri);
        var lines = map[uri].Lines;
        var changedRange = changer.ApplyChanges(lines, changes);
        foreach (var change in changes)
            endpoints.Remove(uri, change.Range);
        endpoints.Add(uri, lines, changedRange);
        return changedRange;
    }

    private void EnsureDocumentAvailable (string uri)
    {
        if (!map.ContainsKey(uri))
            throw new Error($"Failed to get '{uri}' document: not found.");
    }
}
