namespace Naninovel.Language;

public class Document (List<DocumentLine> lines) : IDocument
{
    public int LineCount => Lines.Count;
    public List<DocumentLine> Lines { get; } = lines;
    public DocumentLine this [Index index] => Lines[index];
}
