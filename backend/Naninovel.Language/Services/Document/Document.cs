using Naninovel.Parsing;

namespace Naninovel.Language;

public class Document (List<DocumentLine> lines) : IDocument
{
    public int LineCount => Lines.Count;
    public List<DocumentLine> Lines { get; } = lines;
    public DocumentLine this [Index index] => Lines[index];

    public IEnumerable<IScriptLine> EnumerateScript ()
    {
        return Lines.Select(l => l.Script);
    }

    public Range GetRange ()
    {
        if (LineCount == 0) return Range.Empty;
        var lastLine = LineCount - 1;
        var lastChar = Lines[lastLine].Range.End + 1;
        return new(new(0, 0), new(lastLine, lastChar));
    }
}
