using System.Collections.Generic;

namespace Naninovel.Language;

public class DocumentRegistry
{
    private readonly Dictionary<string, Document> map = new();

    public void Add (string uri, Document document) => map.Add(uri, document);
    public bool Contains (string uri) => map.ContainsKey(uri);
    public Document Get (string uri) => map[uri];
    public void Remove (string uri) => map.Remove(uri);
}
