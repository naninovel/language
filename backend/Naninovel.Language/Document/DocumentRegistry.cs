using System.Collections.Generic;
using Naninovel.Metadata;

namespace Naninovel.Language;

public class DocumentRegistry : IDocumentRegistry, IMetadataObserver
{
    private readonly Dictionary<string, Document> map = new();
    private readonly DocumentChanger changer = new();
    private readonly MetadataProvider metaProvider = new();
    private readonly IObserverNotifier<IDocumentObserver> notifier;
    private readonly EndpointRegistry endpoints;

    public DocumentRegistry (IObserverNotifier<IDocumentObserver> notifier)
    {
        this.notifier = notifier;
        endpoints = new(metaProvider);
    }

    public void HandleMetadataChanged (Project meta) => metaProvider.Update(meta);

    public IReadOnlyCollection<string> GetAllUris () => map.Keys;

    public bool Contains (string uri, string? label = null)
    {
        if (label is null) return map.ContainsKey(uri);
        return endpoints.Contains(uri, label);
    }

    public bool IsEndpointUsed (string name, string? label = null)
    {
        return endpoints.IsUsed(name, label);
    }

    public IDocument Get (string uri)
    {
        EnsureDocumentAvailable(uri);
        return map[uri];
    }

    public void Upsert (string uri, Document document)
    {
        var adding = !map.ContainsKey(uri);
        var range = new LineRange(0, document.LineCount - 1);
        map[uri] = document;
        if (adding) endpoints.HandleDocumentAdded(uri);
        else endpoints.HandleLinesRemoved(uri, range);
        endpoints.HandleLinesAdded(uri, document.Lines, range);
        if (adding) notifier.Notify(n => n.HandleDocumentAdded(uri));
        else notifier.Notify(n => n.HandleDocumentChanged(uri, range));
    }

    public void Remove (string uri)
    {
        EnsureDocumentAvailable(uri);
        endpoints.HandleLinesRemoved(uri, new(0, map[uri].LineCount - 1));
        endpoints.HandleDocumentRemoved(uri);
        notifier.Notify(n => n.HandleDocumentRemoved(uri));
        map.Remove(uri);
    }

    public void Change (string uri, IReadOnlyList<DocumentChange> changes)
    {
        EnsureDocumentAvailable(uri);
        var lines = map[uri].Lines;
        var changedRange = changer.ApplyChanges(lines, changes);
        foreach (var change in changes)
            endpoints.HandleLinesRemoved(uri, change.Range);
        endpoints.HandleLinesAdded(uri, lines, changedRange);
        notifier.Notify(n => n.HandleDocumentChanged(uri, changedRange));
    }

    private void EnsureDocumentAvailable (string uri)
    {
        if (!map.ContainsKey(uri))
            throw new Error($"Failed to get '{uri}' document: not found.");
    }
}
