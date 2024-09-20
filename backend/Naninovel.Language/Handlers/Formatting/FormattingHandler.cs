using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

public class FormattingHandler (IMetadata meta, IDocumentRegistry docs) : IFormattingHandler
{
    private readonly ScriptSerializer serde = new(meta.Syntax);

    public IReadOnlyList<TextEdit> Format (string documentUri)
    {
        documentUri = Uri.UnescapeDataString(documentUri);
        var doc = docs.Get(documentUri);
        var range = doc.GetRange();
        var text = serde.Serialize(doc.EnumerateScript());
        return [new(range, text)];
    }
}
