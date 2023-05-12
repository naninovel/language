using System.Collections.Generic;

namespace Naninovel.Language;

public class DocumentRegistry
{
    private readonly EndpointRegistry endpoints;
    private readonly Dictionary<string, Document> map = new();

    public DocumentRegistry (EndpointRegistry endpoints)
    {
        this.endpoints = endpoints;
    }

    public bool Contains (string uri) => map.ContainsKey(uri);
    public Document Get (string uri) => map[uri];
    public IReadOnlyCollection<string> GetAllUris () => map.Keys;

    public void Set (string uri, Document document)
    {
        map[uri] = document;
        RegisterChange(uri);
    }

    public void Remove (string uri)
    {
        map.Remove(uri);
        RegisterChange(uri);
    }

    public void RegisterChange (string uri, Range? range = null)
    {
        endpoints.Set(); // TODO: Update endpoint relations.
    }
}
