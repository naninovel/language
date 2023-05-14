using Moq;

namespace Naninovel.Language.Test;

internal static class Common
{
    public static void SetupScript (this Mock<IDocumentRegistry> docs, string uri, params string[] lines)
    {
        var document = new DocumentFactory().CreateDocument(string.Join('\n', lines));
        docs.Setup(d => d.Get(uri)).Returns(document);
        docs.Setup(d => d.GetAllUris()).Returns(new[] { uri });
        docs.Setup(d => d.Contains(It.Is<string>(s => s == uri))).Returns(true);
    }
}
