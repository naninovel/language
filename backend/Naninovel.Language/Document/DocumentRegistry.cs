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

    public bool Contains (string uri)
    {
        return map.ContainsKey(uri);
    }

    public void Upsert (string uri, Document document)
    {
        var adding = !map.ContainsKey(uri);
        var range = new LineRange(0, document.LineCount - 1);
        if (!adding) notifier.Notify(n => n.HandleDocumentChanging(uri, range));
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
        var range = GetChangedRange(changes);
        notifier.Notify(n => n.HandleDocumentChanging(uri, range));
        changer.ApplyChanges(map[uri].Lines, changes);
        notifier.Notify(n => n.HandleDocumentChanged(uri, range));
    }

    private void EnsureDocumentAvailable (string uri)
    {
        if (!Contains(uri)) throw new Error($"Failed to get '{uri}' document: not found.");
    }

    private LineRange GetChangedRange (IReadOnlyList<DocumentChange> changes)
    {
        var start = int.MaxValue;
        var end = int.MinValue;
        foreach (var change in changes)
        {
            if (change.Range.Start.Line < start) start = change.Range.Start.Line;
            if (change.Range.End.Line > end) end = change.Range.End.Line;
        }
        return new LineRange(start, end);
    }
}
