using System.Collections.Generic;
using Naninovel.Metadata;

namespace Naninovel.Language;

public class DocumentRegistry : IDocumentRegistry, IMetadataObserver
{
    private readonly Dictionary<string, Document> map = new();
    private readonly DocumentChanger changer = new();
    private readonly MetadataProvider metaProvider = new();
    private readonly IObserverNotifier<IDocumentObserver> notifier;
    private readonly WipEndpointRegistry endpoints;

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
        var changing = map.ContainsKey(uri);
        map[uri] = document;
        endpoints.Remove(uri);
        endpoints.Add(uri, document.Lines);
        if (changing) notifier.Notify(n => n.HandleDocumentChanged(uri, new(0, document.LineCount - 1)));
        else notifier.Notify(n => n.HandleDocumentAdded(uri));
    }

    public void Remove (string uri)
    {
        map.Remove(uri);
        endpoints.Remove(uri);
        notifier.Notify(n => n.HandleDocumentRemoved(uri));
    }

    public void Change (string uri, IReadOnlyList<DocumentChange> changes)
    {
        EnsureDocumentAvailable(uri);
        var lines = map[uri].Lines;
        var changedRange = changer.ApplyChanges(lines, changes);
        foreach (var change in changes)
            endpoints.Remove(uri, change.Range);
        endpoints.Add(uri, lines, changedRange);
        notifier.Notify(n => n.HandleDocumentChanged(uri, changedRange));
    }

    private void EnsureDocumentAvailable (string uri)
    {
        if (!map.ContainsKey(uri))
            throw new Error($"Failed to get '{uri}' document: not found.");
    }
}
