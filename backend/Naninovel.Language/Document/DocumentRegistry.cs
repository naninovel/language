using System.Collections.Generic;
using Naninovel.Parsing;

namespace Naninovel.Language;

public class DocumentRegistry : IDocumentRegistry
{
    private readonly Dictionary<string, Document> map = new();
    private readonly DocumentFactory factory = new();
    private readonly DocumentChanger changer = new();
    private readonly EndpointRegistry endpoints = new();
    private readonly IDiagnoser diagnoser;

    public DocumentRegistry (IDiagnoser diagnoser)
    {
        this.diagnoser = diagnoser;
    }

    public IReadOnlyCollection<string> GetAllUris () => map.Keys;

    public bool Contains (string uri, string? label = null)
    {
        if (!map.TryGetValue(uri, out var doc)) return false;
        if (string.IsNullOrEmpty(label)) return true;
        foreach (var line in doc.Lines)
            if (line.Script is LabelLine labelLine && labelLine.Label == label)
                return true;
        return false;
    }

    public IDocument Get (string uri)
    {
        return map.TryGetValue(uri, out var document) ? document :
            throw new Error($"Failed to get '{uri}' document: not found.");
    }

    public void Upsert (IReadOnlyList<DocumentInfo> infos)
    {
        foreach (var info in infos)
            map[info.Uri] = factory.CreateDocument(info.Text);
        foreach (var info in infos)
            RegisterChange(info.Text);
        foreach (var info in infos)
            diagnoser.Diagnose(info.Uri);
    }

    public void Remove (string uri)
    {
        map.Remove(uri);
        RegisterChange(uri);
        foreach (var otherUri in GetAllUris())
            diagnoser.Diagnose(otherUri);
    }

    public void Change (string uri, IReadOnlyList<DocumentChange> changes)
    {
        changer.ApplyChanges(((Document)Get(uri)).Lines, changes);
        foreach (var change in changes)
            RegisterChange(uri, change.Range);
        foreach (var change in changes)
            diagnoser.Diagnose(uri, change.Range);
    }

    private void RegisterChange (string uri, Range? range = null)
    {
        endpoints.HandleChange(); // TODO: Update endpoint relations.
    }
}
