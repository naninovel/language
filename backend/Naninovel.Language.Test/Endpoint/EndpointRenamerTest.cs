using System.Collections.Immutable;
using Moq;
using Naninovel.Utilities;

namespace Naninovel.Language.Test;

public class EndpointRenamerTest
{
    private readonly MetadataMock meta = new();
    private readonly Mock<IDocumentRegistry> docs = new();
    private readonly Mock<IEndpointRegistry> ends = new();
    private readonly EndpointRenamer renamer;

    public EndpointRenamerTest ()
    {
        meta.SetupNavigationCommands();
        docs.Setup(d => d.ResolvePath(It.IsAny<string>())).Returns((string path) => path.GetAfterFirst("/").GetBeforeLast(".nani"));
        ends.Setup(d => d.GetLabelLocations(It.Ref<QualifiedLabel>.IsAny)).Returns(ImmutableHashSet<LineLocation>.Empty);
        ends.Setup(e => e.GetNavigatorLocations(It.Ref<QualifiedEndpoint>.IsAny)).Returns(ImmutableHashSet<LineLocation>.Empty);
        ends.Setup(e => e.GetAllNavigators()).Returns(ImmutableHashSet<QualifiedEndpoint>.Empty);
        renamer = new(meta, docs.Object, ends.Object);
    }

    [Fact]
    public void CanRenameLabel ()
    {
        docs.SetupScript("/script1.nani",
            "# foo",
            "@goto .foo",
            "@goto script2.bar"
        );
        docs.SetupScript("/script2.nani",
            "x [goto script1.foo]",
            "x [goto .bar]",
            "# bar"
        );
        ends.Setup(d => d.GetLabelLocations(new("script1", "foo"))).Returns(new HashSet<LineLocation> { new("/script1.nani", 0) });
        ends.Setup(d => d.GetLabelLocations(new("script2", "bar"))).Returns(new HashSet<LineLocation> { new("/script2.nani", 2) });
        ends.Setup(d => d.GetAllNavigators()).Returns(new HashSet<QualifiedEndpoint> { new("script1", "foo"), new("script2", "bar") });
        ends.Setup(d => d.GetNavigatorLocations(new("script1", "foo"))).Returns(new HashSet<LineLocation> { new("/script1.nani", 1), new("/script2.nani", 0) });
        ends.Setup(d => d.GetNavigatorLocations(new("script2", "bar"))).Returns(new HashSet<LineLocation> { new("/script1.nani", 2), new("/script2.nani", 1) });
        var editFoo = renamer.RenameLabel("/script1.nani", "foo", "baz");
        var editBar = renamer.RenameLabel("/script2.nani", "bar", "nya");
        Assert.Equal(3, editFoo!.Value.DocumentChanges.Count);
        Assert.Equal([new(new(new(0, 2), new(0, 5)), "baz", "naniscript-rename-label")], editFoo!.Value.DocumentChanges[0].Edits);
        Assert.Equal([new(new(new(1, 6), new(1, 10)), ".baz", "naniscript-rename-label")], editFoo!.Value.DocumentChanges[1].Edits);
        Assert.Equal([new(new(new(0, 8), new(0, 19)), "script1.baz", "naniscript-rename-label")], editFoo!.Value.DocumentChanges[2].Edits);
        Assert.Equal(3, editBar!.Value.DocumentChanges.Count);
        Assert.Equal([new(new(new(2, 2), new(2, 5)), "nya", "naniscript-rename-label")], editBar!.Value.DocumentChanges[0].Edits);
        Assert.Equal([new(new(new(2, 6), new(2, 17)), "script2.nya", "naniscript-rename-label")], editBar!.Value.DocumentChanges[1].Edits);
        Assert.Equal([new(new(new(1, 8), new(1, 12)), ".nya", "naniscript-rename-label")], editBar!.Value.DocumentChanges[2].Edits);
    }

    [Fact]
    public void CanRenameScript ()
    {
        docs.SetupScript("/script1.nani",
            "# foo",
            "@goto .foo",
            "@goto script2.bar"
        );
        docs.SetupScript("/script2.nani",
            "x [goto script1.foo]",
            "x [goto .bar]",
            "# bar"
        );
        ends.Setup(d => d.GetAllNavigators()).Returns(new HashSet<QualifiedEndpoint> { new("script1", "foo"), new("script2", "bar") });
        ends.Setup(d => d.GetNavigatorLocations(new("script1", "foo"))).Returns(new HashSet<LineLocation> { new("/script1.nani", 1), new("/script2.nani", 0) });
        ends.Setup(d => d.GetNavigatorLocations(new("script2", "bar"))).Returns(new HashSet<LineLocation> { new("/script1.nani", 2), new("/script2.nani", 1) });
        var edit1 = renamer.RenameScript("/script1.nani", "/scriptOne.nani");
        var edit2 = renamer.RenameScript("/script2.nani", "/scriptTwo.nani");
        Assert.Single(edit1!.Value.DocumentChanges);
        Assert.Equal([new(new(new(0, 8), new(0, 19)), "scriptOne.foo", "naniscript-rename-script")], edit1!.Value.DocumentChanges[0].Edits);
        Assert.Single(edit2!.Value.DocumentChanges);
        Assert.Equal([new(new(new(2, 6), new(2, 17)), "scriptTwo.bar", "naniscript-rename-script")], edit2!.Value.DocumentChanges[0].Edits);
    }

    [Fact]
    public void CanRenameDirectory ()
    {
        docs.SetupScript("/1/script1.nani",
            "# foo",
            "@goto .foo",
            "@goto 2/script2.bar"
        );
        docs.SetupScript("/2/script2.nani",
            "x [goto 1/script1.foo]",
            "x [goto .bar]",
            "# bar"
        );
        ends.Setup(d => d.GetAllNavigators()).Returns(new HashSet<QualifiedEndpoint> { new("1/script1", "foo"), new("2/script2", "bar") });
        ends.Setup(d => d.GetNavigatorLocations(new("1/script1", "foo"))).Returns(new HashSet<LineLocation> { new("/1/script1.nani", 1), new("/2/script2.nani", 0) });
        ends.Setup(d => d.GetNavigatorLocations(new("2/script2", "bar"))).Returns(new HashSet<LineLocation> { new("/1/script1.nani", 2), new("/2/script2.nani", 1) });
        var edit1 = renamer.RenameDirectory("/1", "/one");
        var edit2 = renamer.RenameDirectory("/2", "/two");
        Assert.Single(edit1!.Value.DocumentChanges);
        Assert.Equal([new(new(new(0, 8), new(0, 21)), "one/script1.foo", "naniscript-rename-directory")], edit1!.Value.DocumentChanges[0].Edits);
        Assert.Single(edit2!.Value.DocumentChanges);
        Assert.Equal([new(new(new(2, 6), new(2, 19)), "two/script2.bar", "naniscript-rename-directory")], edit2!.Value.DocumentChanges[0].Edits);
    }

    [Fact]
    public void IgnoresLinesWithoutEndpoints ()
    {
        docs.SetupScript("/script1.nani",
            "# foo",
            "@cmd .foo",
            "goto script2.bar"
        );
        docs.SetupScript("/script2.nani",
            "x [cmd script1.foo]",
            "x goto .bar",
            "# bar"
        );
        ends.Setup(d => d.GetAllNavigators()).Returns(new HashSet<QualifiedEndpoint> { new("script1", "foo"), new("script2", "bar") });
        ends.Setup(d => d.GetNavigatorLocations(new("script1", "foo"))).Returns(new HashSet<LineLocation> { new("/script1.nani", 1), new("/script2.nani", 0) });
        ends.Setup(d => d.GetNavigatorLocations(new("script2", "bar"))).Returns(new HashSet<LineLocation> { new("/script1.nani", 2), new("/script2.nani", 1) });
        Assert.Null(renamer.RenameLabel("/script1.nani", "foo", "baz"));
        Assert.Null(renamer.RenameLabel("/script2.nani", "bar", "nya"));
    }
}
