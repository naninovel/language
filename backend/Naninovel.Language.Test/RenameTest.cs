using Moq;

namespace Naninovel.Language.Test;

public class RenameTest
{
    private readonly MetadataMock meta = new();
    private readonly Mock<IDocumentRegistry> docs = new();
    private readonly Mock<IEndpointRenamer> renamer = new();
    private readonly RenameHandler handler;

    public RenameTest ()
    {
        handler = new(renamer.Object, docs.Object);
    }

    [Fact]
    public void PrepareRenameReturnsRangeOfTheLabelValue ()
    {
        var expectedRange = new Range(new(0, 2), new(0, 5));
        docs.SetupScript(meta, "script.nani", "# foo");
        Assert.Equal(expectedRange, handler.PrepareRename("script.nani", new(0, 0)));
        Assert.Equal(expectedRange, handler.PrepareRename("script.nani", new(0, 1)));
        Assert.Equal(expectedRange, handler.PrepareRename("script.nani", new(0, 2)));
        Assert.Equal(expectedRange, handler.PrepareRename("script.nani", new(0, 4)));
    }

    [Fact]
    public void PrepareRenameReturnsNullForOtherLineTypes ()
    {
        docs.SetupScript(meta, "script.nani",
            "@command",
            "# comment",
            "generic"
        );
        Assert.Null(handler.PrepareRename("script.nani", new(0, 0)));
        Assert.Null(handler.PrepareRename("script.nani", new(0, 0)));
        Assert.Null(handler.PrepareRename("script.nani", new(0, 0)));
    }

    [Fact]
    public void WhenRenamingOverLabelLineReturnsWorkspaceEdit ()
    {
        var expectedEdit = new WorkspaceEdit([]);
        docs.SetupScript(meta, "script.nani", "# foo");
        renamer.Setup(r => r.RenameLabel("script.nani", "foo", "bar")).Returns(expectedEdit);
        Assert.Equal(expectedEdit, handler.Rename("script.nani", new(0, 0), "bar"));
        Assert.Equal(expectedEdit, handler.Rename("script.nani", new(0, 1), "bar"));
        Assert.Equal(expectedEdit, handler.Rename("script.nani", new(0, 2), "bar"));
        Assert.Equal(expectedEdit, handler.Rename("script.nani", new(0, 4), "bar"));
    }

    [Fact]
    public void WhenRenamingOverOtherLineTypesReturnsNull ()
    {
        docs.SetupScript(meta, "script.nani",
            "@command",
            "# comment",
            "generic"
        );
        renamer.Setup(r => r.RenameLabel(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(new WorkspaceEdit([]));
        Assert.Null(handler.Rename("script.nani", new(0, 0), "foo"));
        Assert.Null(handler.Rename("script.nani", new(0, 0), "foo"));
        Assert.Null(handler.Rename("script.nani", new(0, 0), "foo"));
    }
}
