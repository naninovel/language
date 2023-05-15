using System;
using System.Collections.Generic;
using Moq;
using Naninovel.Parsing;
using Xunit;

namespace Naninovel.Language.Test;

public class DocumentTest
{
    private readonly Mock<IDiagnoser> diagnoser = new();
    private readonly DocumentRegistry registry = new();
    private readonly DocumentHandler handler;

    public DocumentTest ()
    {
        handler = new DocumentHandler(registry, diagnoser.Object);
    }

    [Fact]
    public void WhenUpsertDocumentIsUpsertToRegistry ()
    {
        handler.UpsertDocuments(ToInfos("foo", ""));
        Assert.True(registry.Contains("foo"));
    }

    [Fact]
    public void WhenRemovedDocumentIsRemovedFromRegistry ()
    {
        handler.UpsertDocuments(ToInfos("foo", ""));
        handler.RemoveDocument("foo");
        Assert.False(registry.Contains("foo"));
    }

    [Fact]
    public void WhenDocumentWithExistingKeyUpsertItsReplaced ()
    {
        registry.Upsert("foo", "1");
        registry.Upsert("foo", "2");
        Assert.Equal("2", registry.Get("foo")[0].Text);
    }

    [Fact]
    public void WhenDocumentNotFoundExceptionIsThrown ()
    {
        Assert.Contains("not found", Assert.Throws<Error>(() => registry.Get("foo")).Message);
    }

    [Fact]
    public void UpsertDocumentWithEmptyContentHasSingleEmptyLine ()
    {
        handler.UpsertDocuments(ToInfos("@", ""));
        Assert.Equal(1, registry.Get("@").LineCount);
        Assert.Empty(registry.Get("@")[0].Text);
    }

    [Fact]
    public void UpsertDocumentTextLinesArePreserved ()
    {
        handler.UpsertDocuments(ToInfos("@", "generic\n@command\n#label\n;comment"));
        var document = registry.Get("@");
        Assert.Equal("generic", document[0].Text);
        Assert.Equal("@command", document[1].Text);
        Assert.Equal("#label", document[2].Text);
        Assert.Equal(";comment", document[3].Text);
    }

    [Fact]
    public void UpsertDocumentTextIsParsed ()
    {
        handler.UpsertDocuments(ToInfos("@", "generic\n@command\n#label\n;comment"));
        var document = registry.Get("@");
        Assert.IsType<GenericLine>(document[0].Script);
        Assert.IsType<CommandLine>(document[1].Script);
        Assert.IsType<LabelLine>(document[2].Script);
        Assert.IsType<CommentLine>(document[3].Script);
    }

    [Fact]
    public void CanInsertNewCharacter ()
    {
        handler.UpsertDocuments(ToInfos("@", "@ba"));
        handler.ChangeDocument("@", new[] { new DocumentChange(new(new(0, 3), new(0, 3)), "r") });
        Assert.Equal("@bar", registry.Get("@")[0].Text);
    }

    [Fact]
    public void CanInsertEmptyNewLines ()
    {
        handler.UpsertDocuments(ToInfos("@", ""));
        handler.ChangeDocument("@", new[] { new DocumentChange(new(new(0, 0), new(0, 0)), "\n\n") });
        Assert.Equal(3, registry.Get("@").LineCount);
    }

    [Fact]
    public void CanModifyExistingCharacter ()
    {
        handler.UpsertDocuments(ToInfos("@", "@bar"));
        handler.ChangeDocument("@", new[] { new DocumentChange(new(new(0, 1), new(0, 2)), "f") });
        Assert.Equal("@far", registry.Get("@")[0].Text);
    }

    [Fact]
    public void CanRemoveExistingCharacter ()
    {
        handler.UpsertDocuments(ToInfos("@", "@cmd x {x}"));
        handler.ChangeDocument("@", new[] { new DocumentChange(new(new(0, 8), new(0, 9)), "") });
        Assert.Equal("@cmd x {}", registry.Get("@")[0].Text);
    }

    [Fact]
    public void CanRemoveEmptyNewLines ()
    {
        handler.UpsertDocuments(ToInfos("@", "\n\n"));
        handler.ChangeDocument("@", new[] { new DocumentChange(new(new(0, 0), new(2, 0)), "") });
        Assert.Equal(1, registry.Get("@").LineCount);
        Assert.Empty(registry.Get("@")[0].Text);
    }

    [Fact]
    public void CanRemoveLinesWithMixedLineBreaks ()
    {
        handler.UpsertDocuments(ToInfos("@", "a\nb\r\nc"));
        handler.ChangeDocument("@", new[] { new DocumentChange(new(new(0, 0), new(2, 0)), "") });
        Assert.Equal(1, registry.Get("@").LineCount);
        Assert.Equal("c", registry.Get("@")[0].Text);
    }

    [Fact]
    public void ChangeAcrossMultipleLinesAppliedCorrectly ()
    {
        handler.UpsertDocuments(ToInfos("@", "a\n\nbc\nd"));
        handler.ChangeDocument("@", new[] { new DocumentChange(new(new(0, 1), new(2, 1)), "e") });
        Assert.Equal(2, registry.Get("@").LineCount);
        Assert.Equal("aec", registry.Get("@")[0].Text);
        Assert.Equal("d", registry.Get("@")[1].Text);
    }

    [Fact]
    public void MultipleChangesAreAppliedInOrder ()
    {
        handler.UpsertDocuments(ToInfos("@", ""));
        handler.ChangeDocument("@", new[] {
            new DocumentChange(new(new(0, 0), new(0, 0)), "a"),
            new DocumentChange(new(new(0, 1), new(0, 1)), "b"),
            new DocumentChange(new(new(0, 2), new(0, 2)), "c")
        });
        Assert.Equal("abc", registry.Get("@")[0].Text);
    }

    [Fact]
    public void WhenChangedLinesAreReParsed ()
    {
        handler.UpsertDocuments(ToInfos("@", "generic"));
        handler.ChangeDocument("@", new[] { new DocumentChange(new(new(0, 0), new(0, 7)), "@bar") });
        Assert.Equal("bar", ((CommandLine)registry.Get("@")[0].Script).Command.Identifier);
    }

    [Fact]
    public void CanInsertMultipleLinesAndThenAppendOneMore ()
    {
        handler.UpsertDocuments(ToInfos("@", ""));
        handler.ChangeDocument("@", new[] {
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
        handler.UpsertDocuments(ToInfos("@", "foo\n"));
        handler.ChangeDocument("@", new[] { new DocumentChange(new(new(0, 3), new(0, 3)), "\nbar") });
        var document = registry.Get("@");
        Assert.Equal(3, document.LineCount);
        Assert.Equal("foo", document[0].Text);
        Assert.Equal("bar", document[1].Text);
        Assert.Empty(document[2].Text);
    }

    [Fact]
    public void UpsertDocumentIsDiagnosed ()
    {
        handler.UpsertDocuments(ToInfos("foo", ""));
        diagnoser.Verify(d => d.Diagnose("foo", null), Times.Once);
    }

    [Fact]
    public void UpsertDocumentsAreDiagnosed ()
    {
        handler.UpsertDocuments(new DocumentInfo[] { new("foo", ""), new("bar", "") });
        diagnoser.Verify(d => d.Diagnose("foo", null), Times.Once);
        diagnoser.Verify(d => d.Diagnose("bar", null), Times.Once);
    }

    [Fact]
    public void ChangedDocumentIsDiagnosed ()
    {
        handler.UpsertDocuments(ToInfos("foo", "a"));
        handler.ChangeDocument("foo", new[] { new DocumentChange(new(new(0, 0), new(0, 1)), "b") });
        diagnoser.Verify(d => d.Diagnose("foo", new(0, 0)), Times.Once);
    }

    [Fact]
    public void WhenCantGetLineRangeReturnsEmpty ()
    {
        var line = new DocumentLine("", new LabelLine(""), Array.Empty<ParseError>(), new());
        Assert.Equal(new InlineRange(0, 0), line.GetLineRange(null));
        Assert.Equal(new InlineRange(0, 0), line.GetLineRange(new PlainText("")));
    }

    [Fact]
    public void WhenCantExtractTextReturnsEmpty ()
    {
        var line = new DocumentLine("", new LabelLine(""), Array.Empty<ParseError>(), new());
        Assert.Empty(line.Extract(null));
        Assert.Empty(line.Extract(new PlainText("")));
        Assert.Empty(line.Extract(new InlineRange(9, 1)));
    }

    private IReadOnlyList<DocumentInfo> ToInfos (string documentUri, string documentText)
    {
        return new[] { new DocumentInfo(documentUri, documentText) };
    }
}
