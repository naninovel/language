using Moq;

namespace Naninovel.Language.Test;

public class DocumentHandlerTest
{
    private readonly Mock<IDocumentRegistry> registry = new();
    private readonly Mock<IEndpointRenamer> renamer = new();
    private readonly Mock<IEditPublisher> editor = new();
    private readonly DocumentHandler handler;

    public DocumentHandlerTest ()
    {
        var factory = new DocumentFactory(new MetadataMock());
        handler = new DocumentHandler(registry.Object, factory, renamer.Object, editor.Object);
    }

    [Fact]
    public void UpsertToRegistryOnUpsert ()
    {
        handler.UpsertDocuments([new("foo.nani", ""), new("bar.nani", "")]);
        registry.Verify(r => r.Upsert("foo.nani", It.IsAny<Document>()));
        registry.Verify(r => r.Upsert("bar.nani", It.IsAny<Document>()));
    }

    [Fact]
    public void ChangesInRegistryOnChange ()
    {
        var changes = new DocumentChange[] { new(new(), "foo.nani") };
        handler.ChangeDocument("foo.nani", changes);
        registry.Verify(r => r.Change("foo.nani", changes));
    }

    [Fact]
    public void RenamesInRegistryOnRename ()
    {
        handler.RenameDocuments([new("foo.nani", "bar.nani")]);
        registry.Verify(r => r.Rename("foo.nani", "bar.nani"));
    }

    [Fact]
    public void RenamesInRegistryAllAffectedDocumentsOnDirectoryRename ()
    {
        registry.Setup(r => r.GetAllUris()).Returns(["/foo.nani", "/sub/bar.nani", "/sub/baz.nani"]);
        handler.RenameDocuments([new("/sub", "/bus")]);
        registry.Verify(r => r.Rename("/sub/bar.nani", "/bus/bar.nani"));
        registry.Verify(r => r.Rename("/sub/baz.nani", "/bus/baz.nani"));
        registry.Verify(r => r.GetAllUris());
        registry.VerifyNoOtherCalls();
    }

    [Fact]
    public void RemovesFromRegistryOnDelete ()
    {
        handler.DeleteDocuments([new("foo.nani")]);
        registry.Verify(r => r.Remove("foo.nani"));
    }

    [Fact]
    public void IgnoresNonScriptFiles ()
    {
        handler.UpsertDocuments([new("foo.txt", ""), new("bar.txt", "")]);
        handler.ChangeDocument("foo.txt", [new(new(), "foo.txt")]);
        handler.RenameDocuments([new("foo.txt", "bar.txt")]);
        handler.DeleteDocuments([new("foo.txt")]);
        registry.VerifyNoOtherCalls();
    }

    [Fact]
    public void UnEscapesDocumentUris ()
    {
        handler.UpsertDocuments([new("%D0%A1%D0%BA%D1%80%D0%B8%D0%BF%D1%82.nani", "")]);
        handler.ChangeDocument("%D0%A1%D0%BA%D1%80%D0%B8%D0%BF%D1%82.nani", []);
        handler.RenameDocuments([new("%D0%A1%D0%BA%D1%80%D0%B8%D0%BF%D1%82.nani", "%D0%A1%D0%BA%D1%80%D0%B8%D0%BF%D1%82.nani")]);
        handler.DeleteDocuments([new("%D0%A1%D0%BA%D1%80%D0%B8%D0%BF%D1%82.nani")]);

        registry.Verify(r => r.Upsert("Скрипт.nani", It.IsAny<Document>()));
        registry.Verify(r => r.Change("Скрипт.nani", It.IsAny<DocumentChange[]>()));
        registry.Verify(r => r.Rename("Скрипт.nani", "Скрипт.nani"));
        registry.Verify(r => r.Remove("Скрипт.nani"));
    }

    [Fact]
    public void AppliesRenameRefactorOnScriptRename ()
    {
        var expectedEdit = new WorkspaceEdit([]);
        registry.Setup(r => r.GetAllUris()).Returns(["foo.nani"]);
        renamer.Setup(r => r.RenameScript("foo.nani", "bar.nani")).Returns(expectedEdit);
        handler.RenameDocuments([new("foo.nani", "bar.nani")]);
        editor.Verify(e => e.PublishEdit("Rename endpoints 'foo.nani' -> 'bar.nani'", expectedEdit));
    }

    [Fact]
    public void DoesntApplyRenameRefactorOnScriptRenameWhenNothingToRefactor ()
    {
        renamer.Setup(r => r.RenameScript("foo.nani", "bar.nani")).Returns((WorkspaceEdit?)null);
        handler.RenameDocuments([new("foo.nani", "bar.nani")]);
        editor.VerifyNoOtherCalls();
    }

    [Fact]
    public void AppliesRenameRefactorOnDirectoryRename ()
    {
        var expectedEdit = new WorkspaceEdit([]);
        registry.Setup(r => r.GetAllUris()).Returns(["foo"]);
        renamer.Setup(r => r.RenameDirectory("foo", "bar")).Returns(expectedEdit);
        handler.RenameDocuments([new("foo", "bar")]);
        editor.Verify(e => e.PublishEdit("Rename endpoints 'foo' -> 'bar'", expectedEdit));
    }

    [Fact]
    public void DoesntApplyRenameRefactorOnDirectoryRenameWhenNothingToRefactor ()
    {
        registry.Setup(r => r.GetAllUris()).Returns(["foo"]);
        renamer.Setup(r => r.RenameDirectory("foo", "bar")).Returns((WorkspaceEdit?)null);
        handler.RenameDocuments([new("foo", "bar")]);
        editor.VerifyNoOtherCalls();
    }
}
