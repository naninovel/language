using System;
using System.Collections.Generic;
using Naninovel.Parsing;
using Xunit;

namespace Naninovel.Language.Test;

public class DocumentTest
{
    private readonly DocumentRegistry registry;
    private readonly MockDiagnoser diagnoser;
    private readonly DocumentHandler handler;

    public DocumentTest ()
    {
        registry = new DocumentRegistry(new());
        diagnoser = new MockDiagnoser();
        handler = new DocumentHandler(registry, diagnoser);
    }

    [Fact]
    public void WhenOpenedDocumentIsAddedToRegistry ()
    {
        handler.Open(new("foo", ""));
        Assert.True(registry.Contains("foo"));
    }

    [Fact]
    public void WhenClosedDocumentIsRemovedFromRegistry ()
    {
        handler.Open(new("foo", ""));
        handler.Close("foo");
        Assert.False(registry.Contains("foo"));
    }

    [Fact]
    public void WhenDocumentWithExistingKeySetItsReplaced ()
    {
        registry.Set("foo", new Document { Lines = { Capacity = 0 } });
        registry.Set("foo", new Document { Lines = { Capacity = 1 } });
        Assert.Equal(1, registry.Get("foo").Lines.Capacity);
    }

    [Fact]
    public void WhenDocumentNotFoundExceptionIsThrown ()
    {
        Assert.Throws<KeyNotFoundException>(() => registry.Get("foo"));
    }

    [Fact]
    public void OpenedDocumentWithEmptyContentHasSingleEmptyLine ()
    {
        handler.Open(new("@", ""));
        Assert.Single(registry.Get("@").Lines);
        Assert.Empty(registry.Get("@")[0].Text);
    }

    [Fact]
    public void OpenedDocumentTextLinesArePreserved ()
    {
        handler.Open(new("@", "generic\n@command\n#label\n;comment"));
        var document = registry.Get("@");
        Assert.Equal("generic", document[0].Text);
        Assert.Equal("@command", document[1].Text);
        Assert.Equal("#label", document[2].Text);
        Assert.Equal(";comment", document[3].Text);
    }

    [Fact]
    public void OpenedDocumentTextIsParsed ()
    {
        handler.Open(new("@", "generic\n@command\n#label\n;comment"));
        var document = registry.Get("@");
        Assert.IsType<GenericLine>(document[0].Script);
        Assert.IsType<CommandLine>(document[1].Script);
        Assert.IsType<LabelLine>(document[2].Script);
        Assert.IsType<CommentLine>(document[3].Script);
    }

    [Fact]
    public void CanInsertNewCharacter ()
    {
        handler.Open(new("@", "@ba"));
        handler.Change("@", new[] { new DocumentChange(new(new(0, 3), new(0, 3)), "r") });
        Assert.Equal("@bar", registry.Get("@")[0].Text);
    }

    [Fact]
    public void CanInsertEmptyNewLines ()
    {
        handler.Open(new("@", ""));
        handler.Change("@", new[] { new DocumentChange(new(new(0, 0), new(0, 0)), "\n\n") });
        Assert.Equal(3, registry.Get("@").Lines.Count);
    }

    [Fact]
    public void CanModifyExistingCharacter ()
    {
        handler.Open(new("@", "@bar"));
        handler.Change("@", new[] { new DocumentChange(new(new(0, 1), new(0, 2)), "f") });
        Assert.Equal("@far", registry.Get("@")[0].Text);
    }

    [Fact]
    public void CanRemoveExistingCharacter ()
    {
        handler.Open(new("@", "@cmd x {x}"));
        handler.Change("@", new[] { new DocumentChange(new(new(0, 8), new(0, 9)), "") });
        Assert.Equal("@cmd x {}", registry.Get("@")[0].Text);
    }

    [Fact]
    public void CanRemoveEmptyNewLines ()
    {
        handler.Open(new("@", "\n\n"));
        handler.Change("@", new[] { new DocumentChange(new(new(0, 0), new(2, 0)), "") });
        Assert.Single(registry.Get("@").Lines);
        Assert.Empty(registry.Get("@")[0].Text);
    }

    [Fact]
    public void CanRemoveLinesWithMixedLineBreaks ()
    {
        handler.Open(new("@", "a\nb\r\nc"));
        handler.Change("@", new[] { new DocumentChange(new(new(0, 0), new(2, 0)), "") });
        Assert.Single(registry.Get("@").Lines);
        Assert.Equal("c", registry.Get("@")[0].Text);
    }

    [Fact]
    public void ChangeAcrossMultipleLinesAppliedCorrectly ()
    {
        handler.Open(new("@", "a\n\nbc\nd"));
        handler.Change("@", new[] { new DocumentChange(new(new(0, 1), new(2, 1)), "e") });
        Assert.Equal(2, registry.Get("@").Lines.Count);
        Assert.Equal("aec", registry.Get("@")[0].Text);
        Assert.Equal("d", registry.Get("@")[1].Text);
    }

    [Fact]
    public void MultipleChangesAreAppliedInOrder ()
    {
        handler.Open(new("@", ""));
        handler.Change("@", new[] {
            new DocumentChange(new(new(0, 0), new(0, 0)), "a"),
            new DocumentChange(new(new(0, 1), new(0, 1)), "b"),
            new DocumentChange(new(new(0, 2), new(0, 2)), "c")
        });
        Assert.Equal("abc", registry.Get("@")[0].Text);
    }

    [Fact]
    public void WhenChangedLinesAreReParsed ()
    {
        handler.Open(new("@", "generic"));
        handler.Change("@", new[] { new DocumentChange(new(new(0, 0), new(0, 7)), "@bar") });
        Assert.Equal("bar", ((CommandLine)registry.Get("@")[0].Script).Command.Identifier);
    }

    [Fact]
    public void CanInsertMultipleLinesAndThenAppendOneMore ()
    {
        handler.Open(new("@", ""));
        handler.Change("@", new[] {
            new DocumentChange(new(new(0, 0), new(0, 0)), "a\nb\nc"),
            new DocumentChange(new(new(2, 1), new(2, 1)), "\n")
        });
        Assert.Equal(4, registry.Get("@").Lines.Count);
        Assert.Equal("a", registry.Get("@")[0].Text);
        Assert.Equal("b", registry.Get("@")[1].Text);
        Assert.Equal("c", registry.Get("@")[2].Text);
        Assert.Empty(registry.Get("@")[3].Text);
    }

    [Fact]
    public void CanInsertLineBreakWithLeadingContent ()
    {
        handler.Open(new("@", "foo\n"));
        handler.Change("@", new[] { new DocumentChange(new(new(0, 3), new(0, 3)), "\nbar") });
        var document = registry.Get("@");
        Assert.Equal(3, document.Lines.Count);
        Assert.Equal("foo", document[0].Text);
        Assert.Equal("bar", document[1].Text);
        Assert.Empty(document[2].Text);
    }

    [Fact]
    public void OpenedDocumentIsDiagnosed ()
    {
        handler.Open(new("foo", ""));
        Assert.Single(diagnoser.DiagnoseRequests);
        Assert.Equal("foo", diagnoser.DiagnoseRequests[0]);
    }

    [Fact]
    public void ChangedDocumentIsDiagnosed ()
    {
        handler.Open(new("foo", ""));
        handler.Change("foo", Array.Empty<DocumentChange>());
        Assert.Equal(2, diagnoser.DiagnoseRequests.Count);
        Assert.Equal("foo", diagnoser.DiagnoseRequests[1]);
    }

    [Fact]
    public void WhenCantGetLineRangeReturnsEmpty ()
    {
        var line = new DocumentLine("", new LabelLine(""), Array.Empty<ParseError>(), new());
        Assert.Equal(new LineRange(0, 0), line.GetLineRange(null));
        Assert.Equal(new LineRange(0, 0), line.GetLineRange(new PlainText("")));
    }

    [Fact]
    public void WhenCantExtractTextReturnsEmpty ()
    {
        var line = new DocumentLine("", new LabelLine(""), Array.Empty<ParseError>(), new());
        Assert.Empty(line.Extract(null));
        Assert.Empty(line.Extract(new PlainText("")));
        Assert.Empty(line.Extract(new LineRange(9, 1)));
    }

    [Fact]
    public void WhenOpeningMultipleDocumentsTheyAreDiagnosedAfterRegistration ()
    {
        handler.Open(new("foo", ""));
        Assert.Single(diagnoser.DiagnoseRequests);
        Assert.Equal("foo", diagnoser.DiagnoseRequests[0]);
    }
}
