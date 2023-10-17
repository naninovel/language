using Moq;
using Naninovel.Parsing;
using Naninovel.TestUtilities;
using static Naninovel.Language.Test.Common;

namespace Naninovel.Language.Test;

public class DocumentChangerTest
{
    private readonly DocumentRegistry registry = new(
        new Mock<IObserverRegistry<IDocumentObserver>>().Object,
        new NotifierMock<IDocumentObserver>());

    [Fact]
    public void CanInsertNewCharacter ()
    {
        registry.Upsert("@", CreateDocument("@ba"));
        registry.Change("@", new[] { new DocumentChange(new(new(0, 3), new(0, 3)), "r") });
        Assert.Equal("@bar", registry.Get("@")[0].Text);
    }

    [Fact]
    public void CanInsertEmptyNewLines ()
    {
        registry.Upsert("@", CreateDocument(""));
        registry.Change("@", new[] { new DocumentChange(new(new(0, 0), new(0, 0)), "\n\n") });
        Assert.Equal(3, registry.Get("@").LineCount);
    }

    [Fact]
    public void CanModifyExistingCharacter ()
    {
        registry.Upsert("@", CreateDocument("@bar"));
        registry.Change("@", new[] { new DocumentChange(new(new(0, 1), new(0, 2)), "f") });
        Assert.Equal("@far", registry.Get("@")[0].Text);
    }

    [Fact]
    public void CanRemoveExistingCharacter ()
    {
        registry.Upsert("@", CreateDocument("@cmd x {x}"));
        registry.Change("@", new[] { new DocumentChange(new(new(0, 8), new(0, 9)), "") });
        Assert.Equal("@cmd x {}", registry.Get("@")[0].Text);
    }

    [Fact]
    public void CanRemoveEmptyNewLines ()
    {
        registry.Upsert("@", CreateDocument("", "", ""));
        registry.Change("@", new[] { new DocumentChange(new(new(0, 0), new(2, 0)), "") });
        Assert.Equal(1, registry.Get("@").LineCount);
        Assert.Empty(registry.Get("@")[0].Text);
    }

    [Fact]
    public void CanRemoveLinesWithMixedLineBreaks ()
    {
        registry.Upsert("@", CreateDocument("a", "b\r", "c"));
        registry.Change("@", new[] { new DocumentChange(new(new(0, 0), new(2, 0)), "") });
        Assert.Equal(1, registry.Get("@").LineCount);
        Assert.Equal("c", registry.Get("@")[0].Text);
    }

    [Fact]
    public void ChangeAcrossMultipleLinesAppliedCorrectly ()
    {
        registry.Upsert("@", CreateDocument("a", "", "bc", "d"));
        registry.Change("@", new[] { new DocumentChange(new(new(0, 1), new(2, 1)), "e") });
        Assert.Equal(2, registry.Get("@").LineCount);
        Assert.Equal("aec", registry.Get("@")[0].Text);
        Assert.Equal("d", registry.Get("@")[1].Text);
    }

    [Fact]
    public void MultipleChangesAreAppliedInOrder ()
    {
        registry.Upsert("@", CreateDocument(""));
        registry.Change("@", new[] {
            new DocumentChange(new(new(0, 0), new(0, 0)), "a"),
            new DocumentChange(new(new(0, 1), new(0, 1)), "b"),
            new DocumentChange(new(new(0, 2), new(0, 2)), "c")
        });
        Assert.Equal("abc", registry.Get("@")[0].Text);
    }

    [Fact]
    public void WhenChangedLinesAreReParsed ()
    {
        registry.Upsert("@", CreateDocument("generic"));
        registry.Change("@", new[] { new DocumentChange(new(new(0, 0), new(0, 7)), "@bar") });
        Assert.Equal("bar", ((CommandLine)registry.Get("@")[0].Script).Command.Identifier);
    }

    [Fact]
    public void CanInsertMultipleLinesAndThenAppendOneMore ()
    {
        registry.Upsert("@", CreateDocument(""));
        registry.Change("@", new[] {
            new DocumentChange(new(new(0, 0), new(0, 0)), "a\nb\nc"),
            new DocumentChange(new(new(2, 1), new(2, 1)), "\n")
        });
        Assert.Equal(4, registry.Get("@").LineCount);
        Assert.Equal("a", registry.Get("@")[0].Text);
        Assert.Equal("b", registry.Get("@")[1].Text);
        Assert.Equal("c", registry.Get("@")[2].Text);
        Assert.Empty(registry.Get("@")[3].Text);
    }

    [Fact]
    public void CanInsertLineBreakWithLeadingContent ()
    {
        registry.Upsert("@", CreateDocument("foo", ""));
        registry.Change("@", new[] { new DocumentChange(new(new(0, 3), new(0, 3)), "\nbar") });
        var document = registry.Get("@");
        Assert.Equal(3, document.LineCount);
        Assert.Equal("foo", document[0].Text);
        Assert.Equal("bar", document[1].Text);
        Assert.Empty(document[2].Text);
    }
}
