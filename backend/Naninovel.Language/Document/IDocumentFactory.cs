namespace Naninovel.Language;

public interface IDocumentFactory
{
    Document CreateDocument (string scriptText);
    DocumentLine CreateLine (string lineText);
}
