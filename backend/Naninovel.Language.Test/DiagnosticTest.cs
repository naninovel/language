using Naninovel.Metadata;
using Naninovel.Parsing;
using Xunit;
using Command = Naninovel.Metadata.Command;
using Parameter = Naninovel.Metadata.Parameter;

namespace Naninovel.Language.Test;

public class DiagnosticTest
{
    private readonly Project meta = new();

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
        Assert.Equal(new(new(new(0, 0), new(0, 2)), DiagnosticSeverity.Error,
            LexingErrors.GetFor(ErrorType.MissingTextIdBody)), Diagnose("||")[0]);
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
        meta.Commands = new[] { new Command { Id = "c" } };
        var diags = Diagnose("@c p:v");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 3), new(0, 6)), DiagnosticSeverity.Error,
            "Command 'c' doesn't have 'p' parameter."), diags[0]);
    }

    [Fact]
    public void WhenNamelessParameterMetaNotFoundErrorIsDiagnosed ()
    {
        meta.Commands = new[] { new Command { Id = "c" } };
        var diags = Diagnose("[c n]");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 3), new(0, 4)), DiagnosticSeverity.Error,
            "Command 'c' doesn't have a nameless parameter."), diags[0]);
    }

    [Fact]
    public void WhenValueIsMissingErrorIsDiagnosed ()
    {
        var parameters = new[] { new Parameter { Id = "p" } };
        meta.Commands = new[] { new Command { Id = "c", Parameters = parameters } };
        var diags = Diagnose("@c p:");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 3), new(0, 5)), DiagnosticSeverity.Error,
            LexingErrors.GetFor(ErrorType.MissingParamValue)), diags[0]);
    }

    [Fact]
    public void WhenInvalidValueErrorIsDiagnosed ()
    {
        var parameters = new[] {
            new Parameter { Id = "sb", ValueType = ValueType.Boolean, ValueContainerType = ValueContainerType.Single },
            new Parameter { Id = "nd", ValueType = ValueType.Decimal, ValueContainerType = ValueContainerType.Named },
            new Parameter { Id = "il", ValueType = ValueType.Integer, ValueContainerType = ValueContainerType.List },
            new Parameter { Id = "nbl", ValueType = ValueType.Boolean, ValueContainerType = ValueContainerType.NamedList }
        };
        meta.Commands = new[] { new Command { Id = "c", Parameters = parameters } };
        var diags = Diagnose("@c sb:- nd:x.- il:,1.0 nbl:x.,x,.,.-");
        Assert.Equal(4, diags.Length);
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
        var parameters = new[] { new Parameter { Id = "p", ValueType = ValueType.Boolean } };
        meta.Commands = new[] { new Command { Id = "c", Parameters = parameters } };
        Assert.Empty(Diagnose("@c p:{x}"));
    }

    [Fact]
    public void WhenMissingRequiredParameterErrorIsDiagnosed ()
    {
        var parameters = new[] { new Parameter { Id = "p", Required = true } };
        meta.Commands = new[] { new Command { Id = "c", Parameters = parameters } };
        var diags = Diagnose("@c");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 1), new(0, 2)), DiagnosticSeverity.Error,
            "Required parameter 'p' is missing."), diags[0]);
    }

    [Fact]
    public void WhenCommandIsValidNoErrorsAreDiagnosed ()
    {
        var parameters = new[] {
            new Parameter { Id = "Foo", Alias = "f", Required = true },
            new Parameter { Id = "Bar", Required = true }
        };
        meta.Commands = new[] { new Command { Id = "c", Parameters = parameters } };
        Assert.Empty(Diagnose("@c f:x bar:x"));
    }

    [Fact]
    public void NamelessRequiredParametersAreResolved ()
    {
        var param = new Parameter { Id = "*", Alias = "", Nameless = true, Required = true };
        meta.Commands = new[] { new Command { Id = "c", Parameters = new[] { param } } };
        Assert.Empty(Diagnose("@c foo"));
    }

    private Diagnostic[] Diagnose (string lineText)
    {
        var result = default(Diagnostic[]);
        var diagnoser = new Diagnoser(new(meta), Publish);
        new DocumentHandler(new(), diagnoser).Open("@", lineText);
        return result;

        void Publish (string _, Diagnostic[] diags) => result = diags;
    }
}
