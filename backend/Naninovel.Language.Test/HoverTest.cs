using Moq;
using Naninovel.Metadata;

namespace Naninovel.Language.Test;

public class HoverTest
{
    private readonly MetadataMock meta = new();
    private readonly Mock<IDocumentRegistry> docs = new();
    private readonly HoverHandler handler;

    public HoverTest ()
    {
        handler = new(meta, docs.Object);
    }

    [Fact]
    public void WhenCommandMetadataNotFoundNullIsReturned ()
    {
        Assert.Null(HoverNullable("@c", 1));
        Assert.Null(HoverNullable("[c]", 1));
    }

    [Fact]
    public void WhenParameterMetadataNotFoundNullIsReturned ()
    {
        meta.Commands = [new Command { Id = "c" }];
        Assert.Null(HoverNullable("@c p", 3));
        Assert.Null(HoverNullable("[c p]", 3));
    }

    [Fact]
    public void WhenOverNothingHoverableNullIsReturned ()
    {
        meta.Commands = [new Command { Id = "c" }];
        Assert.Null(HoverNullable("", 0));
        Assert.Null(HoverNullable("; comment", 5));
        Assert.Null(HoverNullable("# label", 0));
        Assert.Null(HoverNullable("generic text", 5));
        Assert.Null(HoverNullable("@c ", 3));
        Assert.Null(HoverNullable("[c]", 3));
        Assert.Null(HoverNullable("{expression}", 5));
    }

    [Fact]
    public void WhenOverCommandIdentifierHoverContainSummary ()
    {
        meta.Commands = [new Command { Id = "c", Summary = "foo" }];
        Assert.Contains("foo", Hover("@c p:v", 1).Contents.Value);
        Assert.Contains("foo", Hover("[c p:v]", 1).Contents.Value);
    }

    [Fact]
    public void WhenOverParameterHoverContentIsEqualToSummary ()
    {
        var parameters = new[] { new Parameter { Id = "p", Summary = "foo" } };
        meta.Commands = [new Command { Id = "c", Parameters = parameters }];
        Assert.Equal("foo", Hover("@c p:v", 4).Contents.Value);
        Assert.Equal("foo", Hover("[c p:v]", 4).Contents.Value);
    }

    [Fact]
    public void CommandIdentifierHoverRangeIsEqualToContentRange ()
    {
        meta.Commands = [new Command { Id = "c", Summary = "" }];
        Assert.Equal(new Range(new(0, 1), new(0, 2)), Hover("@c p:v", 1).Range);
    }

    [Fact]
    public void ParameterHoverRangeIsEqualToContentRange ()
    {
        var parameters = new[] { new Parameter { Id = "p", Summary = "foo" } };
        meta.Commands = [new Command { Id = "c", Parameters = parameters }];
        Assert.Equal(new Range(new(0, 3), new(0, 6)), Hover("@c p:v", 5).Range);
    }

    [Fact]
    public void ParameterWithTextIdHoverRangeIsEqualToContentRange ()
    {
        var parameters = new[] { new Parameter { Id = "p", Summary = "foo" } };
        meta.Commands = [new Command { Id = "c", Parameters = parameters }];
        Assert.Equal(new Range(new(0, 3), new(0, 10)), Hover("@c p:v|id|", 5).Range);
    }

    [Fact]
    public void TextIdHoverRangeIsEqualToParameterContentRange ()
    {
        var parameters = new[] { new Parameter { Id = "p", Summary = "foo" } };
        meta.Commands = [new Command { Id = "c", Parameters = parameters }];
        Assert.Equal(new Range(new(0, 3), new(0, 10)), Hover("@c p:v|id|", 9).Range);
    }

    [Fact]
    public void CommandSummaryHasCorrectMarkup ()
    {
        meta.Commands = [new Command { Id = "c", Summary = "foo" }];
        Assert.Contains("## Summary\nfoo", Hover("@c", 1).Contents.Value);
    }

    [Fact]
    public void CommandRemarksHaveCorrectMarkup ()
    {
        meta.Commands = [new Command { Id = "c", Remarks = "foo" }];
        Assert.Contains("## Remarks\nfoo", Hover("@c", 1).Contents.Value);
    }

    [Fact]
    public void CommandExamplesHaveCorrectMarkup ()
    {
        meta.Commands = [new Command { Id = "c", Examples = "foo" }];
        Assert.Contains("## Examples\n```nani\nfoo\n```", Hover("@c", 1).Contents.Value);
    }

    [Fact]
    public void CommandParametersHaveCorrectMarkup ()
    {
        var parameters = new[] {
            new Parameter { Id = "Nameless", Nameless = true },
            new Parameter { Id = "Required", Required = true },
            new Parameter { Id = "NamelessAndRequired", Nameless = true, Required = true },
            new Parameter { Id = "WithSummary", Summary = "foo" },
            new Parameter { Id = "WithAlias", Alias = "Alias" }
        };
        meta.Commands = [new Command { Id = "c", Parameters = parameters }];
        var content = Hover("@c", 1).Contents.Value;
        Assert.Contains("## Parameters\nName | Type | Summary\n:--- | :--- | :---\n", content);
        Assert.Contains("~nameless~ | string | \n", content);
        Assert.Contains("**required** | string | \n", content);
        Assert.Contains("~**namelessAndRequired**~ | string | \n", content);
        Assert.Contains("withSummary | string | foo\n", content);
        Assert.Contains("alias | string | \n", content);
    }

    [Fact]
    public void DoesntHoverParametersWithEmptySummary ()
    {
        var parameters = new[] { new Parameter { Id = "p", Summary = "" } };
        meta.Commands = [new Command { Id = "c", Parameters = parameters }];
        Assert.Null(Hover("@c p:v|id|", 9).Contents.Value);
    }

    [Fact]
    public void CanHoverFunctionInsideCommandParameter ()
    {
        meta.Commands = [
            new Command { Id = "c", Parameters = [new() { Id = "p" }] }
        ];
        meta.Functions = [
            new Function {
                Name = "fn",
                Summary = "Function summary.",
                Remarks = "Function remarks.",
                Example = "Function examples.",
                Parameters = [
                    new() { Name = "p1", Type = Metadata.ValueType.String },
                    new() { Name = "p2", Type = Metadata.ValueType.Decimal }
                ]
            }
        ];
        var content = Hover("@c p:x{fn()}x", 7).Contents.Value;
        Assert.Contains(
            """
            ## Summary
            Function summary.
            ## Remarks
            Function remarks.
            ## Parameters
            Name | Type
            :--- | :---
            p1 | string | 
            p2 | decimal | 
            ## Examples
            ```nani
            Function examples.
            ```
            """, content);
    }

    [Fact]
    public void CanHoverFunctionInsideGenericText ()
    {
        meta.Functions = [new Function { Name = "fn", Summary = "foo" }];
        Assert.Contains("foo", Hover("{fn()}", 2).Contents.Value);
    }

    [Fact]
    public void CanHoverFunctionInsideParameterWithExpressionContext ()
    {
        meta.Commands = [
            new Command {
                Id = "if", Parameters = [
                    new() { Id = "@", Nameless = true, ValueContext = [new() { Type = ValueContextType.Expression }] }
                ]
            }
        ];
        meta.Functions = [new Function { Name = "fn", Summary = "foo" }];
        Assert.Contains("foo", Hover("@if fn()", 4).Contents.Value);
    }

    [Fact]
    public void CanHoverFunctionInsideParameterWithExpressionContextAndBraces ()
    {
        meta.Commands = [
            new Command {
                Id = "if", Parameters = [
                    new() { Id = "@", Nameless = true, ValueContext = [new() { Type = ValueContextType.Expression }] }
                ]
            }
        ];
        meta.Functions = [new Function { Name = "fn", Summary = "foo" }];
        Assert.Contains("foo", Hover("@if x{fn()}x", 6).Contents.Value);
    }

    [Fact]
    public void DoesntHoverUnhoveredFunctions ()
    {
        meta.Functions = [new Function { Name = "fn", Summary = "foo" }];
        Assert.Null(Hover("{1+1+fn()}", 2).Contents.Value);
    }

    [Fact]
    public void DoesntHoverUnknownFunctions ()
    {
        Assert.Null(Hover("{fn()}", 2).Contents.Value);
    }

    [Fact]
    public void DoesntHoverUnknownFunctionsWithEmptySummary ()
    {
        meta.Functions = [new Function { Name = "fn", Summary = "" }];
        Assert.Null(Hover("{fn()}", 2).Contents.Value);
    }

    private Hover Hover (string line, int charOffset)
    {
        return HoverNullable(line, charOffset) ?? default;
    }

    private Hover? HoverNullable (string line, int charOffset)
    {
        docs.SetupScript(meta, "@", line);
        return handler.Hover("@", new Position(0, charOffset));
    }
}
