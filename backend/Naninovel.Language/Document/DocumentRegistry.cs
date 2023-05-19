using System.Collections.Generic;

namespace Naninovel.Language;

public class DocumentRegistry : IDocumentRegistry
{
    private readonly Dictionary<string, Document> map = new();
    private readonly DocumentChanger changer = new();
    private readonly IObserverNotifier<IDocumentObserver> notifier;

    public DocumentRegistry (IObserverRegistry<IDocumentObserver> observers, IObserverNotifier<IDocumentObserver> notifier)
    {
        observers.Order(Comparer<IDocumentObserver>.Create((x, y) => x is IEndpointRegistry ? -1 : 1));
        this.notifier = notifier;
    }

    public IReadOnlyCollection<string> GetAllUris () => map.Keys;

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
        if (adding) notifier.Notify(n => n.HandleDocumentAdded(uri));
        else notifier.Notify(n => n.HandleDocumentChanged(uri, range));
    }

    public void Remove (string uri)
    {
        EnsureDocumentAvailable(uri);
        notifier.Notify(n => n.HandleDocumentRemoved(uri));
        map.Remove(uri);
    }

    public void Change (string uri, IReadOnlyList<DocumentChange> changes)
    {
        EnsureDocumentAvailable(uri);
        var lines = map[uri].Lines;
        var changedRange = changer.ApplyChanges(lines, changes);
        notifier.Notify(n => n.HandleDocumentChanged(uri, changedRange));
    }

    private void EnsureDocumentAvailable (string uri)
    {
        if (!map.ContainsKey(uri))
            throw new Error($"Failed to get '{uri}' document: not found.");
    }
}
