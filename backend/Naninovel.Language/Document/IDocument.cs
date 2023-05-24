using System;

namespace Naninovel.Language;

public interface IDocument
{
    public int LineCount { get; }
    DocumentLine this [Index index] { get; }
}
