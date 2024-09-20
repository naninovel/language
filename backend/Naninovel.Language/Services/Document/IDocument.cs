using Naninovel.Parsing;

namespace Naninovel.Language;

public interface IDocument
{
    int LineCount { get; }
    DocumentLine this [Index index] { get; }

    IEnumerable<IScriptLine> EnumerateScript ();
    Range GetRange ();
}
