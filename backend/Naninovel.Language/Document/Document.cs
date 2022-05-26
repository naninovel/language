using System.Collections.Generic;

namespace Naninovel.Language;

public class Document
{
    public List<DocumentLine> Lines { get; } = new();

    public DocumentLine this [int index]
    {
        get => Lines[index];
        set => Lines[index] = value;
    }
}
