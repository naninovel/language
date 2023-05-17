using Moq;
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
        Docs.Setup(d => d.IsEndpointUsed("this", "label")).Returns(false);
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
        Docs.Setup(d => d.IsEndpointUsed("this", "label")).Returns(true);
        Assert.Empty(Diagnose("# label"));
    }

    [Fact]
    public void WhenUnknownEndpointScriptWarningIsDiagnosed ()
    {
        Meta.SetupCommandWithEndpoint("goto");
        Docs.Setup(d => d.Contains("other.nani", It.IsAny<string>())).Returns(false);
        var diags = Diagnose("@goto other");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 6), new(0, 11)), DiagnosticSeverity.Warning,
            "Unknown endpoint: other."), diags[0]);
    }

    [Fact]
    public void WhenUnknownEndpointLabelInCurrentScriptWarningIsDiagnosed ()
    {
        Meta.SetupCommandWithEndpoint("goto");
        Docs.Setup(d => d.Contains("this.nani", It.IsAny<string>())).Returns(true);
        Docs.Setup(d => d.Contains("this.nani", "label")).Returns(false);
        var diags = Diagnose("@goto .label");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 6), new(0, 12)), DiagnosticSeverity.Warning,
            "Unknown endpoint: .label."), diags[0]);
    }

    [Fact]
    public void WhenUnknownEndpointLabelInOtherScriptWarningIsDiagnosed ()
    {
        Meta.SetupCommandWithEndpoint("goto");
        Docs.Setup(d => d.Contains("other.nani", It.IsAny<string>())).Returns(true);
        Docs.Setup(d => d.Contains("other.nani", "label")).Returns(false);
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
        Docs.Setup(d => d.Contains("this.nani", null)).Returns(true);
        Docs.Setup(d => d.Contains("this.nani", "label")).Returns(true);
        Docs.Setup(d => d.Contains("other.nani", null)).Returns(true);
        Docs.Setup(d => d.Contains("other.nani", "label")).Returns(true);
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
        Docs.Setup(d => d.Contains("this.nani", "bar")).Returns(true);
        Assert.NotEmpty(Diagnose("@goto .foo"));
        Assert.Empty(Diagnose("@goto .bar"));
    }

    [Fact]
    public void DiagnosticsAreClearedWhenDocumentAdded ()
    {
        Meta.SetupCommandWithEndpoint("goto");
        Docs.Setup(d => d.Contains("other.nani", "foo")).Returns(false);
        Assert.NotEmpty(Diagnose("@goto other.foo"));
        Docs.SetupScript("other.nani", "");
        Docs.Setup(d => d.Contains("other.nani", "foo")).Returns(true);
        Handler.HandleDocumentAdded("other.nani");
        Assert.Empty(GetDiagnostics());
    }

    [Fact]
    public void DiagnosticsAreAddedWhenDocumentRemoved ()
    {
        Meta.SetupCommandWithEndpoint("goto");
        Docs.SetupScript("other.nani", "");
        Docs.Setup(d => d.Contains("other.nani", "foo")).Returns(true);
        Assert.Empty(Diagnose("@goto other.foo"));
        Docs.Setup(d => d.Contains("other.nani", "foo")).Returns(false);
        Handler.HandleDocumentRemoved("other.nani");
        Assert.Single(GetDiagnostics());
        Assert.Equal(new(new(new(0, 6), new(0, 15)), DiagnosticSeverity.Warning,
            "Unknown endpoint: other.foo."), GetDiagnostics()[0]);
    }
}
