using Naninovel.Metadata;

namespace Naninovel.Language.Test;

public class SemanticDiagnoserTest : DiagnoserTest
{
    protected override Settings Settings { get; } = new() { DiagnoseSemantics = true };

    [Fact]
    public void WhenCommandMetaNotFoundErrorIsDiagnosed ()
    {
        var diags = Diagnose("@c");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 1), new(0, 2)), DiagnosticSeverity.Error,
            "Command 'c' is unknown."), diags[0]);
    }

    [Fact]
    public void WhenCommandMissingIdentifierNothingDiagnosed ()
    {
        Assert.Empty(Diagnose("@"));
    }

    [Fact]
    public void WhenParameterMetaNotFoundErrorIsDiagnosed ()
    {
        Meta.Commands = [new Command { Id = "c" }];
        var diags = Diagnose("@c p:v");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 3), new(0, 6)), DiagnosticSeverity.Error,
            "Command 'c' doesn't have 'p' parameter."), diags[0]);
    }

    [Fact]
    public void WhenNamelessParameterMetaNotFoundErrorIsDiagnosed ()
    {
        Meta.Commands = [new Command { Id = "c" }];
        var diags = Diagnose("[c n]");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 3), new(0, 4)), DiagnosticSeverity.Error,
            "Command 'c' doesn't have a nameless parameter."), diags[0]);
    }

    [Fact]
    public void DoesntDiagnoseMissingValue () // It's handled by syntax diagnoser.
    {
        var parameters = new[] { new Parameter { Id = "p", Required = true } };
        Meta.Commands = [new Command { Id = "c", Parameters = parameters }];
        Assert.Empty(Diagnose("@c p:"));
    }

    [Fact]
    public void WhenInvalidValueErrorIsDiagnosed ()
    {
        var parameters = new Parameter[] {
            new() { Id = "sb", ValueType = Metadata.ValueType.Boolean, ValueContainerType = ValueContainerType.Single },
            new() { Id = "nd", ValueType = Metadata.ValueType.Decimal, ValueContainerType = ValueContainerType.Named },
            new() { Id = "il", ValueType = Metadata.ValueType.Integer, ValueContainerType = ValueContainerType.List },
            new() { Id = "nbl", ValueType = Metadata.ValueType.Boolean, ValueContainerType = ValueContainerType.NamedList }
        };
        Meta.Commands = [new Command { Id = "c", Parameters = parameters }];
        var diags = Diagnose("@c sb:- nd:x.- il:,1.0 nbl:x.,x,.,.-");
        Assert.Equal(4, diags.Count);
        Assert.Equal(new(new(new(0, 6), new(0, 7)), DiagnosticSeverity.Error,
            "Invalid value: '-' is not a boolean. Expected 'true' or 'false'."), diags[0]);
        Assert.Equal(new(new(new(0, 11), new(0, 14)), DiagnosticSeverity.Error,
            "Invalid value: 'x.-' is not a named decimal."), diags[1]);
        Assert.Equal(new(new(new(0, 18), new(0, 22)), DiagnosticSeverity.Error,
            "Invalid value: ',1.0' is not a integer list."), diags[2]);
        Assert.Equal(new(new(new(0, 27), new(0, 36)), DiagnosticSeverity.Error,
            "Invalid value: 'x.,x,.,.-' is not a named boolean list. Expected 'true' or 'false'."), diags[3]);
    }

    [Fact]
    public void WhenValueContainExpressionTypeValidityIsNotChecked ()
    {
        var parameters = new[] { new Parameter { Id = "p", ValueType = Metadata.ValueType.Boolean } };
        Meta.Commands = [new Command { Id = "c", Parameters = parameters }];
        Assert.Empty(Diagnose("@c p:{x}"));
    }

    [Fact]
    public void WhenValueIsIdentifiedTextTypeValidityIsNotChecked ()
    {
        var parameters = new[] { new Parameter { Id = "p", ValueType = Metadata.ValueType.Boolean } };
        Meta.Commands = [new Command { Id = "c", Parameters = parameters }];
        Assert.Empty(Diagnose("@c p:x|#x|"));
    }

    [Fact]
    public void WhenMissingRequiredParameterErrorIsDiagnosed ()
    {
        var parameters = new[] { new Parameter { Id = "p", Required = true } };
        Meta.Commands = [new Command { Id = "c", Parameters = parameters }];
        var diags = Diagnose("@c");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 1), new(0, 2)), DiagnosticSeverity.Error,
            "Required parameter 'p' is missing."), diags[0]);
    }

    [Fact]
    public void WhenCommandIsValidNoErrorsAreDiagnosed ()
    {
        var parameters = new Parameter[] {
            new() { Id = "Foo", Alias = "f", Required = true },
            new() { Id = "Bar", Required = true }
        };
        Meta.Commands = [new Command { Id = "c", Parameters = parameters }];
        Assert.Empty(Diagnose("@c f:x bar:x"));
    }

    [Fact]
    public void NamelessRequiredParametersAreResolved ()
    {
        var param = new Parameter { Id = "*", Alias = "", Nameless = true, Required = true };
        Meta.Commands = [new Command { Id = "c", Parameters = [param] }];
        Assert.Empty(Diagnose("@c foo"));
    }

    [Fact]
    public void DiagnosticsAreClearedWhenCorrected ()
    {
        Meta.Commands = [new Command { Id = "bar" }];
        Assert.NotEmpty(Diagnose("@foo"));
        Assert.Empty(Diagnose("@bar"));
    }

    [Fact]
    public void WarnsOnDynamicParametersPreventingPreload ()
    {
        var parameters = new Parameter[] {
            new() { Id = "p1", ValueContext = [new() { Type = ValueContextType.Resource }] },
            new() { Id = "p2", ValueContext = [new() { Type = ValueContextType.Actor }] },
            new() { Id = "p3", ValueContext = [new() { Type = ValueContextType.Appearance }] },
            new() { Id = "p4", ValueContext = [new() { Type = ValueContextType.Color }, null] },
            new() { Id = "p5" }
        };
        Meta.Commands = [new Command { Id = "c", Parameters = parameters }];
        var diags = Diagnose("@c p1:{} p2:x{}x p3:\"x { x } x\" p4:{} p5:{}");
        Assert.Equal(3, diags.Count);
        Assert.Equal(new(new(new(0, 6), new(0, 8)), DiagnosticSeverity.Information,
            "Expressions in this parameter prevent pre-loading associated resources."), diags[0]);
        Assert.Equal(new(new(new(0, 12), new(0, 16)), DiagnosticSeverity.Information,
            "Expressions in this parameter prevent pre-loading associated resources."), diags[1]);
        Assert.Equal(new(new(new(0, 20), new(0, 31)), DiagnosticSeverity.Information,
            "Expressions in this parameter prevent pre-loading associated resources."), diags[2]);
    }

    [Fact]
    public void ErrsWhenMissingRequiredNested ()
    {
        Meta.Commands = [new Command { Id = "c", NestedHost = true, RequiresNested = true }];
        Assert.Equal(new(new(new(0, 1), new(0, 2)), DiagnosticSeverity.Error,
            "This command requires nested lines."), Diagnose("@c")[0]);
        Assert.Equal(new(new(new(0, 1), new(0, 2)), DiagnosticSeverity.Error,
            "This command requires nested lines."), Diagnose("@c", "...")[0]);
    }

    [Fact]
    public void DoesntErrWhenHasRequiredNested ()
    {
        Meta.Commands = [new Command { Id = "c", NestedHost = true, RequiresNested = true }];
        Assert.Empty(Diagnose("@c", "    ..."));
    }

    [Fact]
    public void DoesntErrWhenMissingNestedButNestedAreNotRequired ()
    {
        Meta.Commands = [new Command { Id = "c", NestedHost = true, RequiresNested = false }];
        Assert.Empty(Diagnose("@c"));
        Assert.Empty(Diagnose("@c", "..."));
    }

    [Fact]
    public void DoesntErrWhenHasNestedWhichAreNotRequired ()
    {
        Meta.Commands = [new Command { Id = "c", NestedHost = true, RequiresNested = false }];
        Assert.Empty(Diagnose("@c", "    ..."));
    }

    [Fact]
    public void WarnsWhenHasNestedWhileTheCommandIsNotNestedHost ()
    {
        Meta.Commands = [new Command { Id = "c", NestedHost = false }];
        Assert.Equal(new(new(new(0, 1), new(0, 2)), DiagnosticSeverity.Warning,
            "This command doesn't support nesting."), Diagnose("@c", "    ...")[0]);
    }

    [Fact]
    public void FunctionErrDiagnosed ()
    {
        var parameters = new Parameter[] {
            new() { Id = "@", Nameless = true, ValueContext = [new() { Type = ValueContextType.Expression }] }
        };
        Meta.Commands = [new Command { Id = "if", Parameters = parameters }];
        var diags = Diagnose("@if +");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 4), new(0, 5)), DiagnosticSeverity.Error,
            "Missing unary operand."), diags[0]);
    }

    [Fact]
    public void FunctionAssignmentErrDiagnosed ()
    {
        var parameters = new Parameter[] {
            new() { Id = "@", Nameless = true, ValueContext = [new() { Type = ValueContextType.Expression, SubType = "Assignment" }] },
        };
        Meta.Commands = [new Command { Id = "set", Parameters = parameters }];
        var diags = Diagnose("@set =x");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 5), new(0, 7)), DiagnosticSeverity.Error,
            "Missing assigned variable name."), diags[0]);
    }

    [Fact]
    public void SyntaxErrsInAssignmentExpressionAreDiagnosed ()
    {
        var parameters = new Parameter[] {
            new() { Id = "@", Nameless = true, ValueContext = [new() { Type = ValueContextType.Expression, SubType = "Assignment" }] },
            new() { Id = "p" }
        };
        Meta.Commands = [new Command { Id = "set", Parameters = parameters }];
        var diags = Diagnose("@set x=+ p:x|#x|");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 7), new(0, 8)), DiagnosticSeverity.Error,
            "Missing unary operand."), diags[0]);
    }

    [Fact]
    public void DoesntDiagnoseAssignmentInNonAssignmentExpressions ()
    {
        var parameters = new Parameter[] {
            new() { Id = "@", Nameless = true }
        };
        Meta.Commands = [new Command { Id = "if", Parameters = parameters }];
        Assert.Empty(Diagnose("@if {x}"));
    }

    [Fact]
    public void FunctionErrInDynamicDiagnosed ()
    {
        var parameters = new Parameter[] {
            new() { Id = "@", Nameless = true }
        };
        Meta.Commands = [new Command { Id = "c", Parameters = parameters }];
        var diags = Diagnose("@c x|#x|{\"}x");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 9), new(0, 10)), DiagnosticSeverity.Error,
            "Unclosed string."), diags[0]);
    }

    [Fact]
    public void CorrectDynamicValuesHaveNoErrors ()
    {
        var parameters = new Parameter[] {
            new() { Id = "@", Nameless = true }
        };
        Meta.Commands = [new Command { Id = "c", Parameters = parameters }];
        Assert.Empty(Diagnose("@c {x}.x"));
    }

    [Fact]
    public void RespectsCompilerLocalizationWhenDiagnosingBoolean ()
    {
        Meta.Syntax = new Parsing.Syntax(@true: "да", @false: "нет");
        var parameters = new Parameter[] {
            new() { Id = "p1", ValueType = Metadata.ValueType.Boolean },
            new() { Id = "p2", ValueType = Metadata.ValueType.Boolean },
            new() { Id = "p3", ValueType = Metadata.ValueType.Boolean },
            new() { Id = "p4", ValueType = Metadata.ValueType.Boolean }
        };
        Meta.Commands = [new Command { Id = "c", Parameters = parameters }];
        Assert.Empty(Diagnose("@c p1:да p2:нет p3! !p4"));
    }

    [Fact]
    public void ErrWhenUnknownFunction ()
    {
        var diags = Diagnose("{foo()}");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 1), new(0, 6)), DiagnosticSeverity.Error,
            "Unknown function."), diags[0]);
    }

    [Fact]
    public void ErrWhenMissingFunctionParameter ()
    {
        Meta.Functions = [new() { Name = "foo", Parameters = [new() { Name = "x" }] }];
        var diags = Diagnose("{foo()}");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 1), new(0, 6)), DiagnosticSeverity.Error,
            "Missing 'x' parameter."), diags[0]);
    }

    [Fact]
    public void ErrWhenExtraFunctionParameter ()
    {
        Meta.Functions = [new() { Name = "foo", Parameters = [new() { Name = "x" }] }];
        var diags = Diagnose("""{foo("x","y")}""");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 9), new(0, 12)), DiagnosticSeverity.Error,
            "Unknown parameter."), diags[0]);
    }

    [Fact]
    public void DoesntErrWhenExtraFunctionParameterIsVariadic ()
    {
        Meta.Functions = [new() { Name = "foo", Parameters = [new() { Name = "x", Variadic = true }] }];
        Assert.Empty(Diagnose("""{foo("x","y")}"""));
    }

    [Fact]
    public void ErrWhenFunctionParameterHasInvalidValue ()
    {
        Meta.Functions = [new() { Name = "foo", Parameters = [new() { Name = "x", Type = Metadata.ValueType.Integer }] }];
        var diags = Diagnose("{foo(0.1)}");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 5), new(0, 8)), DiagnosticSeverity.Error,
            "Invalid value: '0.1' is not a integer."), diags[0]);
    }

    [Fact]
    public void ErrWhenFunctionParameterHasInvalidConstantValue ()
    {
        var param = new FunctionParameter { Name = "x", Context = new() { Type = ValueContextType.Constant, SubType = "@" } };
        Meta.Functions = [new() { Name = "foo", Parameters = [param] }];
        Meta.Constants = [new() { Name = "@", Values = ["foo"] }];
        var diags = Diagnose("""{foo("bar")}""");
        Assert.Single(diags);
        Assert.Equal(new(new(new(0, 5), new(0, 10)), DiagnosticSeverity.Error,
            "Invalid constant value. Expected to be one of '@'."), diags[0]);
    }

    [Fact]
    public void DoesntErrWhenFunctionParameterHasValidConstantValue ()
    {
        var param = new FunctionParameter { Name = "x", Context = new() { Type = ValueContextType.Constant, SubType = "@" } };
        Meta.Functions = [new() { Name = "foo", Parameters = [param] }];
        Meta.Constants = [new() { Name = "@", Values = ["foo"] }];
        Assert.Empty(Diagnose("""{foo("foo")}"""));
    }

    [Fact]
    public void DoesntErrWhenFunctionParameterIsAnExpression ()
    {
        var param = new FunctionParameter { Name = "x", Context = new() { Type = ValueContextType.Constant, SubType = "@" } };
        Meta.Functions = [new() { Name = "foo", Parameters = [param] }];
        Meta.Constants = [new() { Name = "@", Values = ["foo"] }];
        Assert.Empty(Diagnose("{foo(x)}"));
        Assert.Empty(Diagnose("{foo(x+y)}"));
        Assert.Empty(Diagnose("""{foo(foo("foo"))}"""));
    }
}
