using Moq;
using Xunit;

namespace Naninovel.Language.Test;

public class DocumentHandlerTest
{
    private readonly Mock<IDocumentRegistry> registry = new();
    private readonly DocumentHandler handler;

    public DocumentHandlerTest ()
    {
        handler = new DocumentHandler(registry.Object);
    }

    [Fact]
    public void UpsertToRegistryOnUpsert ()
    {
        handler.UpsertDocuments(new DocumentInfo[] { new("foo", ""), new("bar", "") });
        registry.Verify(r => r.Upsert("foo", It.IsAny<Document>()));
        registry.Verify(r => r.Upsert("bar", It.IsAny<Document>()));
    }

    [Fact]
    public void RemovesFromRegistryOnRemove ()
    {
        handler.RemoveDocument("foo");
        registry.Verify(r => r.Remove("foo"));
    }

    [Fact]
    public void ChangesInRegistryOnChange ()
    {
        var changes = new DocumentChange[] { new(new(), "foo") };
        handler.ChangeDocument("foo", changes);
        registry.Verify(r => r.Change("foo", changes));
    }
}
