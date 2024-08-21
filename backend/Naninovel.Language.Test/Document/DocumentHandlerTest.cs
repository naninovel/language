using Moq;

namespace Naninovel.Language.Test;

public class DocumentHandlerTest
{
    private readonly Mock<IDocumentRegistry> registry = new();
    private readonly DocumentHandler handler;

    public DocumentHandlerTest ()
    {
        var factory = new DocumentFactory(new MetadataMock());
        handler = new DocumentHandler(registry.Object, factory);
    }

    [Fact]
    public void UpsertToRegistryOnUpsert ()
    {
        handler.UpsertDocuments([new("foo", ""), new("bar", "")]);
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
        handler.RenameDocuments([new("foo", "bar")]);
        registry.Verify(r => r.Rename("foo", "bar"));
    }

    [Fact]
    public void RemovesFromRegistryOnDelete ()
    {
        handler.DeleteDocuments([new("foo")]);
        registry.Verify(r => r.Remove("foo"));
    }

    [Fact]
    public void UnEscapesDocumentUris ()
    {
        handler.UpsertDocuments([new("%D0%A1%D0%BA%D1%80%D0%B8%D0%BF%D1%82", "")]);
        handler.ChangeDocument("%D0%A1%D0%BA%D1%80%D0%B8%D0%BF%D1%82", []);
        handler.RenameDocuments([new("%D0%A1%D0%BA%D1%80%D0%B8%D0%BF%D1%82", "%D0%A1%D0%BA%D1%80%D0%B8%D0%BF%D1%82")]);
        handler.DeleteDocuments([new("%D0%A1%D0%BA%D1%80%D0%B8%D0%BF%D1%82")]);

        registry.Verify(r => r.Upsert("Скрипт", It.IsAny<Document>()));
        registry.Verify(r => r.Change("Скрипт", It.IsAny<DocumentChange[]>()));
        registry.Verify(r => r.Rename("Скрипт", "Скрипт"));
        registry.Verify(r => r.Remove("Скрипт"));
    }
}
