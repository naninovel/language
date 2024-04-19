using Naninovel.Parsing;

namespace Naninovel.Language.Test;

public class SyntaxDiagnoserTest : DiagnoserTest
{
    protected override Settings Settings { get; } = new() { DiagnoseSyntax = true };

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
    public void WhenTabIndentErrorIsDiagnosed ()
    {
        Diagnose("\tx");
        Assert.Single(GetDiagnostics());
        Assert.Equal(new(new(new(0, 0), new(0, 1)), DiagnosticSeverity.Error,
            "Tab indents are not supported. Use 4 spaces."), GetDiagnostics()[0]);
    }

    [Fact]
    public void WhenIncorrectIndentLengthErrorIsDiagnosed ()
    {
        Diagnose("  x");
        Assert.Single(GetDiagnostics());
        Assert.Equal(new(new(new(0, 0), new(0, 2)), DiagnosticSeverity.Error,
            "Each indent level should be exactly 4 spaces."), GetDiagnostics()[0]);
    }

    [Fact]
    public void DiagnosticsAreAddedAfterChange ()
    {
        Assert.Empty(Diagnose("# foo"));
        Assert.NotEmpty(Diagnose("#"));
    }

    [Fact]
    public void DiagnosticsAreClearedAfterChange ()
    {
        Assert.NotEmpty(Diagnose("#"));
        Assert.Empty(Diagnose("# foo"));
    }

    [Fact]
    public void DiagnosesWhenRemovingLines ()
    {
        SetupHandler();
        Docs.SetupScript("foo.nani", "# bar", "# baz");
        Handler.HandleDocumentAdded("foo.nani");
        Assert.Empty(GetDiagnostics("foo.nani"));
        Handler.HandleDocumentChanging("foo.nani", new(0, 1));
        Docs.SetupScript("foo.nani", "#");
        Handler.HandleDocumentChanged("foo.nani", new(0, 0));
        Assert.NotEmpty(GetDiagnostics("foo.nani"));
    }

    [Fact]
    public void DiagnosesWhenRemovingDocument ()
    {
        SetupHandler();
        Docs.SetupScript("foo.nani", "#");
        Handler.HandleDocumentAdded("foo.nani");
        Assert.NotEmpty(GetDiagnostics("foo.nani"));
        Handler.HandleDocumentRemoved("foo.nani");
        Assert.Empty(GetDiagnostics("foo.nani"));
    }
}
