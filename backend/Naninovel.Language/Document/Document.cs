using System;
using System.Collections.Generic;

namespace Naninovel.Language;

public class Document : IDocument
{
    public int LineCount => Lines.Count;
    public List<DocumentLine> Lines { get; }

    public Document (List<DocumentLine> lines)
    {
        Lines = lines;
    }

    public DocumentLine this [Index index] => Lines[index];
}
