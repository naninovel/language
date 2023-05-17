using System;
using Moq;
using Naninovel.Parsing;
using Xunit;

namespace Naninovel.Language.Test;

public class SyntaxDiagnoserTest : DiagnoserTest
{
    protected override Settings Settings { get; } = new() { DiagnoseSyntax = true };

    [Fact]
    public void WhenEmptyDocumentResultIsEmpty ()
    {
        Assert.Empty(Diagnose(""));
    }

    [Fact]
    public void WhenMissingTextIdBodyErrorIsDiagnosed ()
    {
        Diagnose("|#|");
        Assert.Single(GetDiagnostics());
        Assert.Equal(new(new(new(0, 0), new(0, 3)), DiagnosticSeverity.Error,
            LexingErrors.GetFor(ErrorType.MissingTextIdBody)), GetDiagnostics()[0]);
    }

    [Fact]
    public void WhenSpaceInLabelErrorIsDiagnosed ()
    {
        Diagnose("# l l");
        Assert.Single(GetDiagnostics());
        Assert.Equal(new(new(new(0, 3), new(0, 4)), DiagnosticSeverity.Error,
            LexingErrors.GetFor(ErrorType.SpaceInLabel)), GetDiagnostics()[0]);
    }

    [Fact]
    public void WhenCommandIdIsMissingErrorIsDiagnosed ()
    {
        Diagnose("@");
        Assert.Single(GetDiagnostics());
        Assert.Equal(new(new(new(0, 0), new(0, 1)), DiagnosticSeverity.Error,
            LexingErrors.GetFor(ErrorType.MissingCommandId)), GetDiagnostics()[0]);
    }

    [Fact]
    public void WhenValueIsMissingErrorIsDiagnosed ()
    {
        Diagnose("@c p:");
        Assert.Single(GetDiagnostics());
        Assert.Equal(new(new(new(0, 3), new(0, 5)), DiagnosticSeverity.Error,
            LexingErrors.GetFor(ErrorType.MissingParamValue)), GetDiagnostics()[0]);
    }

    [Fact]
    public void WhenChangingDocumentDiagnosesOnlyChangedLines ()
    {
        var doc = new Mock<IDocument>();
        doc.Setup(d => d.LineCount).Returns(4);
        doc.SetupGet(d => d[It.IsAny<Index>()]).Returns(new DocumentFactory().CreateLine("@"));
        Docs.Setup(d => d.GetAllUris()).Returns(new[] { "foo.nani" });
        Docs.Setup(d => d.Get("foo.nani")).Returns(doc.Object);
        Handler.HandleSettingsChanged(new() { DiagnoseSyntax = true });
        Handler.HandleDocumentAdded("foo.nani");
        doc.Invocations.Clear();
        Handler.HandleDocumentChanged("foo.nani", new Range(new(1, 0), new(2, 0)));
        doc.VerifyGet(l => l[0], Times.Never);
        doc.VerifyGet(l => l[1], Times.Once);
        doc.VerifyGet(l => l[2], Times.Once);
        doc.VerifyGet(l => l[3], Times.Never);
    }

    [Fact]
    public void DiagnosticsAreClearedWhenCorrected ()
    {
        Assert.NotEmpty(Diagnose("#"));
        Assert.Empty(Diagnose("# foo"));
    }
}
