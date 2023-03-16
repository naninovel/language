using System.Collections.Generic;

namespace Naninovel.Language;

public class DocumentRegistry
{
    private readonly Dictionary<string, Document> map = new();

    public void Set (string uri, Document document) => map[uri] = document;
    public bool Contains (string uri) => map.ContainsKey(uri);
    public Document Get (string uri) => map[uri];
    public IReadOnlyCollection<string> GetAllUris () => map.Keys;
    public void Remove (string uri) => map.Remove(uri);
}
