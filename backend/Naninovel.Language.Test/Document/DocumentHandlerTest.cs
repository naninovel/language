using Moq;

namespace Naninovel.Language.Test;

public class DocumentHandlerTest
{
    private readonly Mock<IDocumentRegistry> registry = new();
    private readonly DocumentHandler handler;

    public DocumentHandlerTest ()
    {
        handler = new DocumentHandler(registry.Object, new());
    }

    [Fact]
    public void UpsertToRegistryOnUpsert ()
    {
        handler.UpsertDocuments(new DocumentInfo[] { new("foo", ""), new("bar", "") });
        registry.Verify(r => r.Upsert("foo", It.IsAny<Document>()));
        registry.Verify(r => r.Upsert("bar", It.IsAny<Document>()));
    }

    [Fact]
    public void ChangesInRegistryOnChange ()
    {
        var changes = new DocumentChange[] { new(new(), "foo") };
        handler.ChangeDocument("foo", changes);
        registry.Verify(r => r.Change("foo", changes));
    }

    [Fact]
    public void RenamesInRegistryOnRename ()
    {
        handler.RenameDocuments(new DocumentRenameInfo[] { new("foo", "bar") });
        registry.Verify(r => r.Rename("foo", "bar"));
    }

    [Fact]
    public void RemovesFromRegistryOnDelete ()
    {
        handler.DeleteDocuments(new DocumentDeleteInfo[] { new("foo") });
        registry.Verify(r => r.Remove("foo"));
    }
}
