using System.Collections.Generic;
using Naninovel.Utilities;

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

    public void Change (string uri, IReadOnlyList<DocumentChange> changes)
    {
        EnsureDocumentAvailable(uri);
        var doc = map[uri];
        var range = GetChangedRange(changes, doc.LineCount);
        notifier.Notify(n => n.HandleDocumentChanging(uri, range));
        changer.ApplyChanges(doc.Lines, changes);
        notifier.Notify(n => n.HandleDocumentChanged(uri, range));
    }

    public void Rename (string oldUri, string newUri)
    {
        EnsureDocumentAvailable(oldUri);
        var doc = map[oldUri];
        Remove(oldUri);
        Upsert(newUri, doc);
    }

    public void Remove (string uri)
    {
        EnsureDocumentAvailable(uri);
        notifier.Notify(n => n.HandleDocumentRemoved(uri));
        map.Remove(uri);
    }

    private void EnsureDocumentAvailable (string uri)
    {
        if (!Contains(uri)) throw new Error($"Failed to get '{uri}' document: not found.");
    }

    private LineRange GetChangedRange (IReadOnlyList<DocumentChange> changes, int lineCount)
    {
        var firstChangedLine = int.MaxValue;
        var lastChangedLine = int.MinValue;
        var insertedOrDeletedLine = false;
        var totalInsertedBreaks = 0;
        foreach (var change in changes)
        {
            if (change.Range.Start.Line < firstChangedLine)
                firstChangedLine = change.Range.Start.Line;
            if (change.Range.End.Line > lastChangedLine)
                lastChangedLine = change.Range.End.Line;
            var insertedBreaks = change.Text.SplitLines().Length - 1;
            var removedBreaks = change.Range.End.Line - change.Range.Start.Line;
            totalInsertedBreaks += insertedBreaks;
            if (!insertedOrDeletedLine && removedBreaks != insertedBreaks)
                insertedOrDeletedLine = true;
        }
        var end = insertedOrDeletedLine ? (lineCount - 1 + totalInsertedBreaks) : lastChangedLine;
        return new LineRange(firstChangedLine, end);
    }
}
