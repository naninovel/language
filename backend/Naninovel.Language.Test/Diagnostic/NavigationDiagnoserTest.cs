using Xunit;

namespace Naninovel.Language.Test;

public class NavigationDiagnoserTest : DiagnoserTest
{
    protected override Settings Settings { get; } = new() { DiagnoseNavigation = true };

    [Fact]
    public void WhenEmptyDocumentResultIsEmpty ()
    {
        Assert.Empty(Diagnose(""));
    }

    [Fact]
    public void WhenUnusedLabelWarningIsDiagnosed ()
    {
        Endpoints.Setup(d => d.LabelUsed("this", "label")).Returns(false);
        var diags = Diagnose("# label");
        Assert.Single(diags);
        Assert.Equal(new(new(0, 2), new(0, 7)), diags[0].Range);
        Assert.Equal(DiagnosticSeverity.Warning, diags[0].Severity);
        Assert.Equal("Unused label.", diags[0].Message);
        Assert.Equal(new[] { DiagnosticTag.Unnecessary }, diags[0].Tags);
    }

    [Fact]
    public void WhenLabelIsUsedWarningIsNotDiagnosed ()
    {
        Endpoints.Setup(d => d.LabelUsed("this", "label")).Returns(true);
        Assert.Empty(Diagnose("# label"));
    }

    [Fact]
    public void WhenUnknownEndpointScriptWarningIsDiagnosed ()
    {
        Meta.SetupCommandWithEndpoint("goto");
        Endpoints.Setup(d => d.ScriptExist("other")).Returns(false);
        var diags = Diagnose("@goto other");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 6), new(0, 11)), DiagnosticSeverity.Warning,
            "Unknown endpoint: other."), diags[0]);
    }

    [Fact]
    public void WhenUnknownEndpointLabelInCurrentScriptWarningIsDiagnosed ()
    {
        Meta.SetupCommandWithEndpoint("goto");
        Endpoints.Setup(d => d.ScriptExist("this")).Returns(true);
        Endpoints.Setup(d => d.LabelExist("this", "label")).Returns(false);
        var diags = Diagnose("@goto .label");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 6), new(0, 12)), DiagnosticSeverity.Warning,
            "Unknown endpoint: .label."), diags[0]);
    }

    [Fact]
    public void WhenUnknownEndpointLabelInOtherScriptWarningIsDiagnosed ()
    {
        Meta.SetupCommandWithEndpoint("goto");
        Endpoints.Setup(d => d.ScriptExist("other")).Returns(true);
        Endpoints.Setup(d => d.LabelExist("other", "label")).Returns(false);
        var diags = Diagnose("@goto other.label");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 6), new(0, 17)), DiagnosticSeverity.Warning,
            "Unknown endpoint: other.label."), diags[0]);
    }

    [Fact]
    public void WhenKnownEndpointWarningIsNotDiagnosed ()
    {
        Meta.SetupCommandWithEndpoint("goto");
        Docs.SetupScript("other.nani", "");
        Endpoints.Setup(d => d.ScriptExist("this")).Returns(true);
        Endpoints.Setup(d => d.LabelExist("this", "label")).Returns(true);
        Endpoints.Setup(d => d.ScriptExist("other")).Returns(true);
        Endpoints.Setup(d => d.LabelExist("other", "label")).Returns(true);
        Assert.Empty(Diagnose("@goto this"));
        Assert.Empty(Diagnose("@goto .label"));
        Assert.Empty(Diagnose("@goto this.label"));
        Assert.Empty(Diagnose("@goto other"));
        Assert.Empty(Diagnose("@goto other.label"));
    }

    [Fact]
    public void DiagnosticsAreClearedWhenCorrected ()
    {
        Meta.SetupCommandWithEndpoint("goto");
        Endpoints.Setup(d => d.LabelExist("this", "bar")).Returns(true);
        Assert.NotEmpty(Diagnose("@goto .foo"));
        Assert.Empty(Diagnose("@goto .bar"));
    }

    [Fact]
    public void DiagnosticsAreClearedWhenDocumentAdded ()
    {
        Meta.SetupCommandWithEndpoint("goto");
        Endpoints.Setup(d => d.LabelExist("other", "foo")).Returns(false);
        Assert.NotEmpty(Diagnose("@goto other.foo"));
        Docs.SetupScript("other.nani", "");
        Endpoints.Setup(d => d.LabelExist("other", "foo")).Returns(true);
        Handler.HandleDocumentAdded("other.nani");
        Assert.Empty(GetDiagnostics());
    }

    [Fact]
    public void DiagnosticsAreAddedWhenDocumentRemoved ()
    {
        Meta.SetupCommandWithEndpoint("goto");
        Docs.SetupScript("other.nani", "");
        Endpoints.Setup(d => d.LabelExist("other", "foo")).Returns(true);
        Assert.Empty(Diagnose("@goto other.foo"));
        Endpoints.Setup(d => d.LabelExist("other", "foo")).Returns(false);
        Handler.HandleDocumentRemoved("other.nani");
        Assert.Single(GetDiagnostics());
        Assert.Equal(new(new(new(0, 6), new(0, 15)), DiagnosticSeverity.Warning,
            "Unknown endpoint: other.foo."), GetDiagnostics()[0]);
    }

    [Fact]
    public void UnusedLabelIsDetectedAfterChange ()
    {
        SetupHandler(Meta.SetupCommandWithEndpoint("goto"));
        Docs.SetupScript("foo.nani", "@goto bar.label");
        Docs.SetupScript("bar.nani", "# label");
        Endpoints.Setup(d => d.LabelUsed("bar", "label")).Returns(true);
        Handler.HandleDocumentAdded("foo.nani");
        Handler.HandleDocumentAdded("bar.nani");
        Assert.Empty(GetDiagnostics("bar.nani"));
        Docs.SetupScript("foo.nani", "@goto bar.baz");
        Endpoints.Setup(d => d.LabelUsed("bar", "label")).Returns(false);
        Handler.HandleDocumentChanged("foo.nani", new(0, 0));
        Assert.Contains(GetDiagnostics("bar.nani"), d => d.Message == "Unused label.");
    }

    [Fact]
    public void UnknownEndpointIsDetectedAfterChange ()
    {
        SetupHandler(Meta.SetupCommandWithEndpoint("goto"));
        Docs.SetupScript("foo.nani", "@goto bar.label");
        Docs.SetupScript("bar.nani", "# label");
        Endpoints.Setup(d => d.LabelExist("bar", "label")).Returns(true);
        Handler.HandleDocumentAdded("foo.nani");
        Handler.HandleDocumentAdded("bar.nani");
        Assert.Empty(GetDiagnostics("foo.nani"));
        Docs.SetupScript("bar.nani", "# baz");
        Endpoints.Setup(d => d.LabelExist("bar", "label")).Returns(false);
        Handler.HandleDocumentChanged("bar.nani", new(0, 0));
        Assert.Contains(GetDiagnostics("foo.nani"), d => d.Message == "Unknown endpoint: bar.label.");
    }
}
