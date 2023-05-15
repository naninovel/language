using System.Collections.Generic;

namespace Naninovel.Language;

public class DocumentHandler : IDocumentHandler
{
    private readonly IDocumentRegistry registry;
    private readonly IDiagnoser diagnoser;

    public DocumentHandler (IDocumentRegistry registry, IDiagnoser diagnoser)
    {
        this.registry = registry;
        this.diagnoser = diagnoser;
    }

    public void UpsertDocuments (IReadOnlyList<DocumentInfo> docs)
    {
        foreach (var doc in docs)
            registry.Upsert(doc);
        foreach (var doc in docs)
            diagnoser.Diagnose(doc.Uri);
    }

    public void RemoveDocument (string uri)
    {
        registry.Remove(uri);
        foreach (var otherUri in registry.GetAllUris())
            diagnoser.Diagnose(otherUri);
    }

    public void ChangeDocument (string uri, IReadOnlyList<DocumentChange> changes)
    {
        var changedRange = registry.Change(uri, changes);
        diagnoser.Diagnose(uri, changedRange);
    }
}
