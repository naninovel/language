using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Naninovel.Metadata;
using Naninovel.Parsing;
using Xunit;

namespace Naninovel.Language.Test;

public class DiagnosticTest
{
    private const string defaultUri = "this.nani";
    private readonly Project meta = new();
    private readonly Mock<IDocumentRegistry> docs = new();
    private readonly Mock<IDiagnosticPublisher> publisher = new();
    private readonly Dictionary<string, IReadOnlyList<Diagnostic>> diagnostics = new();
    private readonly DiagnosticHandler handler;

    public DiagnosticTest ()
    {
        handler = new(docs.Object, publisher.Object);
        docs.Setup(d => d.GetAllUris()).Returns(Array.Empty<string>());
        publisher.Setup(p => p.PublishDiagnostics(It.IsAny<string>(), It.IsAny<IReadOnlyList<Diagnostic>>()))
            .Callback((string uri, IReadOnlyList<Diagnostic> diags) => diagnostics[uri] = diags);
    }

    [Fact]
    public void WhenEmptyDocumentResultIsEmpty ()
    {
        Assert.Empty(Diagnose(""));
    }

    [Fact]
    public void ParseErrorsAreDiagnosedAsErrors ()
    {
        Assert.Equal(new(new(new(0, 0), new(0, 1)), DiagnosticSeverity.Error,
            LexingErrors.GetFor(ErrorType.MissingCommandId)), Diagnose("@")[0]);
        Assert.Equal(new(new(new(0, 3), new(0, 4)), DiagnosticSeverity.Error,
            LexingErrors.GetFor(ErrorType.SpaceInLabel)), Diagnose("# l l")[0]);
        Assert.Equal(new(new(new(0, 0), new(0, 3)), DiagnosticSeverity.Error,
            LexingErrors.GetFor(ErrorType.MissingTextIdBody)), Diagnose("|#|")[0]);
    }

    [Fact]
    public void WhenCommandMetaNotFoundErrorIsDiagnosed ()
    {
        var diags = Diagnose("@c");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 1), new(0, 2)), DiagnosticSeverity.Error,
            "Command 'c' is unknown."), diags[0]);
    }

    [Fact]
    public void WhenParameterMetaNotFoundErrorIsDiagnosed ()
    {
        meta.Commands = new[] { new Metadata.Command { Id = "c" } };
        var diags = Diagnose("@c p:v");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 3), new(0, 6)), DiagnosticSeverity.Error,
            "Command 'c' doesn't have 'p' parameter."), diags[0]);
    }

    [Fact]
    public void WhenNamelessParameterMetaNotFoundErrorIsDiagnosed ()
    {
        meta.Commands = new[] { new Metadata.Command { Id = "c" } };
        var diags = Diagnose("[c n]");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 3), new(0, 4)), DiagnosticSeverity.Error,
            "Command 'c' doesn't have a nameless parameter."), diags[0]);
    }

    [Fact]
    public void WhenValueIsMissingErrorIsDiagnosed ()
    {
        var parameters = new[] { new Metadata.Parameter { Id = "p" } };
        meta.Commands = new[] { new Metadata.Command { Id = "c", Parameters = parameters } };
        var diags = Diagnose("@c p:");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 3), new(0, 5)), DiagnosticSeverity.Error,
            LexingErrors.GetFor(ErrorType.MissingParamValue)), diags[0]);
    }

    [Fact]
    public void WhenInvalidValueErrorIsDiagnosed ()
    {
        var parameters = new[] {
            new Metadata.Parameter { Id = "sb", ValueType = Metadata.ValueType.Boolean, ValueContainerType = ValueContainerType.Single },
            new Metadata.Parameter { Id = "nd", ValueType = Metadata.ValueType.Decimal, ValueContainerType = ValueContainerType.Named },
            new Metadata.Parameter { Id = "il", ValueType = Metadata.ValueType.Integer, ValueContainerType = ValueContainerType.List },
            new Metadata.Parameter { Id = "nbl", ValueType = Metadata.ValueType.Boolean, ValueContainerType = ValueContainerType.NamedList }
        };
        meta.Commands = new[] { new Metadata.Command { Id = "c", Parameters = parameters } };
        var diags = Diagnose("@c sb:- nd:x.- il:,1.0 nbl:x.,x,.,.-");
        Assert.Equal(4, diags.Count);
        Assert.Equal(new(new(new(0, 6), new(0, 7)), DiagnosticSeverity.Error,
            "Invalid value: '-' is not a boolean."), diags[0]);
        Assert.Equal(new(new(new(0, 11), new(0, 14)), DiagnosticSeverity.Error,
            "Invalid value: 'x.-' is not a named decimal."), diags[1]);
        Assert.Equal(new(new(new(0, 18), new(0, 22)), DiagnosticSeverity.Error,
            "Invalid value: ',1.0' is not a integer list."), diags[2]);
        Assert.Equal(new(new(new(0, 27), new(0, 36)), DiagnosticSeverity.Error,
            "Invalid value: 'x.,x,.,.-' is not a named boolean list."), diags[3]);
    }

    [Fact]
    public void WhenValueContainExpressionTypeValidityIsNotChecked ()
    {
        var parameters = new[] { new Metadata.Parameter { Id = "p", ValueType = Metadata.ValueType.Boolean } };
        meta.Commands = new[] { new Metadata.Command { Id = "c", Parameters = parameters } };
        Assert.Empty(Diagnose("@c p:{x}"));
    }

    [Fact]
    public void WhenMissingRequiredParameterErrorIsDiagnosed ()
    {
        var parameters = new[] { new Metadata.Parameter { Id = "p", Required = true } };
        meta.Commands = new[] { new Metadata.Command { Id = "c", Parameters = parameters } };
        var diags = Diagnose("@c");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 1), new(0, 2)), DiagnosticSeverity.Error,
            "Required parameter 'p' is missing."), diags[0]);
    }

    [Fact]
    public void WhenCommandIsValidNoErrorsAreDiagnosed ()
    {
        var parameters = new[] {
            new Metadata.Parameter { Id = "Foo", Alias = "f", Required = true },
            new Metadata.Parameter { Id = "Bar", Required = true }
        };
        meta.Commands = new[] { new Metadata.Command { Id = "c", Parameters = parameters } };
        Assert.Empty(Diagnose("@c f:x bar:x"));
    }

    [Fact]
    public void NamelessRequiredParametersAreResolved ()
    {
        var param = new Metadata.Parameter { Id = "*", Alias = "", Nameless = true, Required = true };
        meta.Commands = new[] { new Metadata.Command { Id = "c", Parameters = new[] { param } } };
        Assert.Empty(Diagnose("@c foo"));
    }

    [Fact]
    public void WhenUnusedLabelWarningIsDiagnosed ()
    {
        docs.Setup(d => d.IsEndpointUsed("this", "label")).Returns(false);
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
        docs.Setup(d => d.IsEndpointUsed("this", "label")).Returns(true);
        Assert.Empty(Diagnose("# label"));
    }

    [Fact]
    public void WhenUnknownEndpointScriptWarningIsDiagnosed ()
    {
        meta.SetupCommandWithEndpoint("goto");
        docs.Setup(d => d.Contains("other.nani", It.IsAny<string>())).Returns(false);
        var diags = Diagnose("@goto other");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 6), new(0, 11)), DiagnosticSeverity.Warning,
            "Unknown endpoint: other."), diags[0]);
    }

    [Fact]
    public void WhenUnknownEndpointLabelInCurrentScriptWarningIsDiagnosed ()
    {
        meta.SetupCommandWithEndpoint("goto");
        docs.Setup(d => d.Contains("this.nani", It.IsAny<string>())).Returns(true);
        docs.Setup(d => d.Contains("this.nani", "label")).Returns(false);
        var diags = Diagnose("@goto .label");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 6), new(0, 12)), DiagnosticSeverity.Warning,
            "Unknown endpoint: .label."), diags[0]);
    }

    [Fact]
    public void WhenUnknownEndpointLabelInOtherScriptWarningIsDiagnosed ()
    {
        meta.SetupCommandWithEndpoint("goto");
        docs.Setup(d => d.Contains("other.nani", It.IsAny<string>())).Returns(true);
        docs.Setup(d => d.Contains("other.nani", "label")).Returns(false);
        var diags = Diagnose("@goto other.label");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 6), new(0, 17)), DiagnosticSeverity.Warning,
            "Unknown endpoint: other.label."), diags[0]);
    }

    [Fact]
    public void WhenKnownEndpointWarningIsNotDiagnosed ()
    {
        meta.SetupCommandWithEndpoint("goto");
        docs.SetupScript("other.nani", "");
        docs.Setup(d => d.Contains("this.nani", null)).Returns(true);
        docs.Setup(d => d.Contains("this.nani", "label")).Returns(true);
        docs.Setup(d => d.Contains("other.nani", null)).Returns(true);
        docs.Setup(d => d.Contains("other.nani", "label")).Returns(true);
        Assert.Empty(Diagnose("@goto this"));
        Assert.Empty(Diagnose("@goto .label"));
        Assert.Empty(Diagnose("@goto this.label"));
        Assert.Empty(Diagnose("@goto other"));
        Assert.Empty(Diagnose("@goto other.label"));
    }

    [Fact]
    public void WhenRangeSpecifiedDiagnosesOnlyAffectedLines ()
    {
        var doc = new Mock<IDocument>();
        doc.SetupGet(d => d[It.IsAny<Index>()]).Returns(new DocumentFactory().CreateLine(""));
        docs.Setup(d => d.GetAllUris()).Returns(Array.Empty<string>());
        handler.HandleSettingsChanged(new(true, false, false));
        docs.Setup(d => d.Get("@")).Returns(doc.Object);
        handler.HandleDocumentChanged("@", new Range(new(1, 0), new(2, 0)));
        doc.VerifyGet(l => l[0], Times.Never);
        doc.VerifyGet(l => l[1], Times.Once);
        doc.VerifyGet(l => l[2], Times.Once);
        doc.VerifyGet(l => l[3], Times.Never);
    }

    [Fact]
    public void SyntaxDiagnosticsAreCleared ()
    {
        Assert.NotEmpty(Diagnose("#", true, false, false));
        Assert.Empty(Diagnose("# foo", true, false, false));
    }

    [Fact]
    public void SemanticDiagnosticsAreCleared ()
    {
        meta.Commands = new[] { new Metadata.Command { Id = "bar" } };
        Assert.NotEmpty(Diagnose("@foo", false, true, false));
        Assert.Empty(Diagnose("@bar", false, true, false));
    }

    [Fact]
    public void NavigationDiagnosticsAreCleared ()
    {
        meta.SetupCommandWithEndpoint("goto");
        docs.Setup(d => d.Contains("this.nani", "bar")).Returns(true);
        Assert.NotEmpty(Diagnose("@goto .foo", false, false));
        Assert.Empty(Diagnose("@goto .bar", false, false));
    }

    private IReadOnlyList<Diagnostic> Diagnose (string line, bool syntax = true, bool semantics = true, bool navigation = true)
    {
        handler.HandleMetadataChanged(meta);
        handler.HandleSettingsChanged(new(syntax, semantics, navigation));
        var changing = docs.Object.GetAllUris().Contains(defaultUri);
        docs.SetupScript(defaultUri, line);
        if (changing) handler.HandleDocumentChanged(defaultUri, new(0, 0));
        else handler.HandleDocumentAdded(defaultUri);
        return GetDiagnostics(defaultUri);
    }

    private IReadOnlyList<Diagnostic> GetDiagnostics (string uri)
    {
        return diagnostics.TryGetValue(uri, out var diags) ? diags : Array.Empty<Diagnostic>();
    }
}
