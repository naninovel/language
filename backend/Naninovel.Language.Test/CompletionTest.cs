using System.Collections.Generic;
using System.Linq;
using Moq;
using Naninovel.Metadata;
using Xunit;

namespace Naninovel.Language.Test;

public class CompletionTest
{
    private readonly Project meta = new();
    private readonly Mock<IDocumentRegistry> docs = new();
    private readonly CompletionHandler handler;

    public CompletionTest ()
    {
        handler = new(docs.Object);
    }

    [Fact]
    public void WhenCommentOrLabelLineResultIsEmpty ()
    {
        Assert.Empty(Complete("; comment", 1));
        Assert.Empty(Complete("# label", 1));
    }

    [Fact]
    public void WhenEmptyLineCharacterIdsAreReturned ()
    {
        meta.Actors = new[] { new Actor { Id = "Ai", Type = "Characters" } };
        Assert.Equal("Ai", Complete("", 0)[0].Label);
    }

    [Fact]
    public void WhenOffsetIsOutOfRangeResultIsEmpty ()
    {
        Assert.Empty(Complete("foo", 4));
        Assert.Empty(Complete("foo", -1));
    }

    [Fact]
    public void WhenOverAuthorContentCharacterIdsAreReturned ()
    {
        meta.Actors = new[] { new Actor { Id = "Ai", Type = "Characters" } };
        Assert.Equal("Ai", Complete("Ai: Hello.", 1)[0].Label);
        Assert.Equal("Ai", Complete("Ai.Happy: Hey!", 2)[0].Label);
    }

    [Fact]
    public void OnlyCharacterIdsAreReturnedOnAuthorCompletion ()
    {
        meta.Actors = new[] {
            new Actor { Id = "Bubble", Type = "TextPrinters" },
            new Actor { Id = "Kohaku", Type = "Characters" }
        };
        var items = Complete("", 0);
        Assert.Single(items);
        Assert.Equal("Kohaku", items[0].Label);
    }

    [Fact]
    public void WhenNoCharactersOnAuthorCompletionResultIsEmpty ()
    {
        Assert.Empty(Complete("", 0));
    }

    [Fact]
    public void CharacterDescriptionIsAssignedToCompletionDetail ()
    {
        meta.Actors = new[] { new Actor { Type = "Characters", Description = "foo" } };
        Assert.Equal("foo", Complete("", 0)[0].Detail);
    }

    [Fact]
    public void AuthorCompletionHasValueKind ()
    {
        meta.Actors = new[] { new Actor { Type = "Characters" } };
        Assert.Equal(CompletionItemKind.Value, Complete("", 0)[0].Kind);
    }

    [Fact]
    public void WhenOverAppearanceContentAppearancesAreReturned ()
    {
        meta.Actors = new[] { new Actor { Id = "Ai", Appearances = new[] { "Happy" }, Type = "Characters" } };
        Assert.Equal("Happy", Complete("Ai.Happy: Hi!", 3)[0].Label);
    }

    [Fact]
    public void WhenAfterDotPrefixedByNonWhitespaceCharactersAppearancesAreReturned ()
    {
        meta.Actors = new[] { new Actor { Id = "Ai", Appearances = new[] { "Happy" }, Type = "Characters" } };
        Assert.Equal("Happy", Complete("Ai.", 3)[0].Label);
    }

    [Fact]
    public void WhenAfterDotPrefixedByTextWithWhitespaceResultIsEmpty ()
    {
        meta.Actors = new[] { new Actor { Id = "Ai", Appearances = new[] { "Happy" }, Type = "Characters" } };
        Assert.Empty(Complete("Ai .", 4));
    }

    [Fact]
    public void WhenNoAppearancesOnAppearanceCompletionResultIsEmpty ()
    {
        meta.Actors = new[] { new Actor { Id = "Ai", Type = "Characters" } };
        Assert.Empty(Complete("Ai.", 3));
    }

    [Fact]
    public void WhenOverInlinedCommandOpenCommandsAreReturned ()
    {
        meta.Commands = new[] { new Command { Id = "cmd" } };
        Assert.Equal("cmd", Complete("[", 1)[0].Label);
    }

    [Fact]
    public void WhenOverInlinedCommandIdContentCommandsAreReturned ()
    {
        meta.Commands = new[] { new Command { Id = "cmd" } };
        Assert.Equal("cmd", Complete("[x", 2)[0].Label);
    }

    [Fact]
    public void WhenOverCommandLineIdCommandsAreReturned ()
    {
        meta.Commands = new[] { new Command { Id = "cmd" } };
        Assert.Equal("cmd", Complete("@", 1)[0].Label);
    }

    [Fact]
    public void WhenOverCommandIdContentCommandsAreReturned ()
    {
        meta.Commands = new[] { new Command { Id = "cmd" } };
        Assert.Equal("cmd", Complete("@x", 2)[0].Label);
    }

    [Fact]
    public void CommandSummaryIsAssignedToDocumentation ()
    {
        meta.Commands = new[] { new Command { Id = "cmd", Summary = "foo" } };
        Assert.Equal("foo", Complete("[", 1)[0].Documentation?.Value);
    }

    [Fact]
    public void WhenOverCommandContentParametersAreReturned ()
    {
        var param = new Parameter { Id = "foo" };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        Assert.Equal("foo", Complete("@cmd ", 5)[0].Label);
    }

    [Fact]
    public void WhenAfterCommandWithNamelessParameterValuesAreReturned ()
    {
        var param = new Parameter { Id = "foo", Nameless = true, ValueType = ValueType.Boolean };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        Assert.Equal("true", Complete("@cmd ", 5)[0].Label);
    }

    [Fact]
    public void WhenCommandMetaNotFoundParametersAreNotCompleted ()
    {
        Assert.Empty(Complete("@cmd ", 5));
    }

    [Fact]
    public void WhenParamMetaNotFoundItIsNotCompleted ()
    {
        meta.Commands = new[] { new Command { Id = "cmd" } };
        Assert.Empty(Complete("@cmd x", 6));
        Assert.Empty(Complete("@cmd x:y", 8));
    }

    [Fact]
    public void WhenOverNamelessParameterValuesAreReturned ()
    {
        var param = new Parameter { Id = "foo", Nameless = true, ValueType = ValueType.Boolean };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        Assert.Equal("true", Complete("@cmd x", 6)[0].Label);
    }

    [Fact]
    public void WhenOverNamelessParameterWithTextIdValuesAreReturned ()
    {
        var param = new Parameter { Id = "foo", Nameless = true, ValueType = ValueType.Boolean };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        Assert.Equal("true", Complete("@cmd x|x|", 6)[0].Label);
    }

    [Fact]
    public void WhenOverParameterAssignmentValuesAreReturned ()
    {
        var param = new Parameter { Id = "id", ValueType = ValueType.Boolean };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        Assert.Equal("true", Complete("@cmd id:", 8)[0].Label);
    }

    [Fact]
    public void ParameterSummaryIsAssignedToDocumentation ()
    {
        var param = new Parameter { Id = "foo", Summary = "bar" };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        Assert.Equal("bar", Complete("@cmd ", 5)[0].Documentation?.Value);
    }

    [Fact]
    public void ParameterDefaultValueIsAssignedToDetails ()
    {
        var foo = new Parameter { Id = "foo", DefaultValue = "foo default" };
        var bar = new Parameter { Id = "bar" };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { foo, bar } } };
        Assert.Equal("Default value: foo default", Complete("@cmd ", 5)[0].Detail);
        Assert.Empty(Complete("@cmd ", 5)[1].Detail);
    }

    [Fact]
    public void ParametersWithoutContextAreNotCompleted ()
    {
        var param = new Parameter { Id = "x" };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        Assert.Empty(Complete("@cmd x:", 7));
    }

    [Fact]
    public void WhenOverParameterValueContentValuesAreReturned ()
    {
        var param = new Parameter { Id = "id", ValueType = ValueType.Boolean };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        Assert.Equal("true", Complete("@cmd id:x", 9)[0].Label);
    }

    [Fact]
    public void WhenOverUnknownContextTypeResultIsEmpty ()
    {
        var param = new Parameter { Id = "x", ValueContext = new ValueContext[] { new() { Type = (ValueContextType)255 } } };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        Assert.Empty(Complete("@cmd x:", 7));
    }

    [Fact]
    public void WhenOverActorContextActorIdsAreReturned ()
    {
        var param = new Parameter { Id = "id", ValueContext = new ValueContext[] { new() { Type = ValueContextType.Actor, SubType = "foo" } } };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        meta.Actors = new[] { new Actor { Id = "1", Type = "foo" } };
        Assert.Equal("1", Complete("@cmd id:x", 9)[0].Label);
    }

    [Fact]
    public void WhenOverActorContextWithoutSubtypeEmptyIsReturned ()
    {
        var param = new Parameter { Id = "id", ValueContext = new ValueContext[] { new() { Type = ValueContextType.Actor } } };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        meta.Actors = new[] { new Actor { Id = "1", Type = "foo" } };
        Assert.Empty(Complete("@cmd id:x", 9));
    }

    [Fact]
    public void WhenWildcardSpecifiedForActorTypeAllActorsAreReturned ()
    {
        var param = new Parameter { Id = "id", ValueContext = new ValueContext[] { new() { Type = ValueContextType.Actor, SubType = "*" } } };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        meta.Actors = new[] {
            new Actor { Id = "1", Type = "foo" },
            new Actor { Id = "2", Type = "bar" }
        };
        Assert.Equal("1", Complete("@cmd id:", 8)[0].Label);
        Assert.Equal("2", Complete("@cmd id:", 8)[1].Label);
    }

    [Fact]
    public void WhenOverAppearanceContextActorAppearancesAreReturned ()
    {
        var idParam = new Parameter { Id = "id", ValueContext = new ValueContext[] { new() { Type = ValueContextType.Actor, SubType = "@" } } };
        var apParam = new Parameter { Id = "ap", ValueContext = new ValueContext[] { new() { Type = ValueContextType.Appearance } } };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { idParam, apParam } } };
        meta.Actors = new[] { new Actor { Id = "Ai", Type = "@", Appearances = new[] { "Normal" } } };
        Assert.Equal("Normal", Complete("@cmd id:Ai ap:", 14)[0].Label);
    }

    [Fact]
    public void WhenOverAppearanceContextButParameterWithActorContextIsMissingResultIsEmpty ()
    {
        var idParam = new Parameter { Id = "id" };
        var apParam = new Parameter { Id = "ap", ValueContext = new ValueContext[] { new() { Type = ValueContextType.Appearance } } };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { idParam, apParam } } };
        meta.Actors = new[] { new Actor { Id = "Ai", Type = "@", Appearances = new[] { "Normal" } } };
        Assert.Empty(Complete("@cmd id:Ai ap:", 14));
    }

    [Fact]
    public void WhenOverAppearanceContextButParameterWithActorContextIsNotFoundResultIsEmpty ()
    {
        var apParam = new Parameter { Id = "ap", ValueContext = new ValueContext[] { new() { Type = ValueContextType.Appearance } } };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { apParam } } };
        Assert.Empty(Complete("@cmd id:Ai ap:", 14));
    }

    [Fact]
    public void WhenOverNamedValueAppearanceContextActorAppearancesAreReturned ()
    {
        var param = new Parameter {
            Id = "@", Nameless = true,
            ValueContainerType = ValueContainerType.Named,
            ValueContext = new ValueContext[] {
                new() { Type = ValueContextType.Actor, SubType = "@" },
                new() { Type = ValueContextType.Appearance }
            }
        };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        meta.Actors = new[] { new Actor { Id = "Ai", Type = "@", Appearances = new[] { "Normal" } } };
        Assert.Equal("Normal", Complete("@cmd Ai.", 8)[0].Label);
    }

    [Fact]
    public void WhenOverNamedNameAppearanceContextActorAppearancesAreReturned ()
    {
        var param = new Parameter {
            Id = "@", Nameless = true,
            ValueContainerType = ValueContainerType.Named,
            ValueContext = new ValueContext[] {
                new() { Type = ValueContextType.Appearance },
                new() { Type = ValueContextType.Actor, SubType = "@" }
            }
        };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        meta.Actors = new[] { new Actor { Id = "Ai", Type = "@", Appearances = new[] { "Normal" } } };
        Assert.Equal("Normal", Complete("@cmd .Ai", 5)[0].Label);
    }

    [Fact]
    public void WhenOverAppearanceContextAndActorIsSpecifiedInNamedParameterAppearancesAreReturned ()
    {
        var apParam = new Parameter { Id = "ap", ValueContext = new ValueContext[] { new() { Type = ValueContextType.Appearance } } };
        var idParam = new Parameter {
            Id = "@", Nameless = true,
            ValueContainerType = ValueContainerType.Named,
            ValueContext = new ValueContext[] { new() { Type = ValueContextType.Actor, SubType = "@" } }
        };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { apParam, idParam } } };
        meta.Actors = new[] { new Actor { Id = "Ai", Type = "@", Appearances = new[] { "Normal" } } };
        Assert.Equal("Normal", Complete("@cmd Ai ap:", 11)[0].Label);
    }

    [Fact]
    public void WhenOverAppearanceContextButActorIdIsNotSpecifiedResultIsEmpty ()
    {
        var apParam = new Parameter { Id = "ap", ValueContext = new ValueContext[] { new() { Type = ValueContextType.Appearance } } };
        var idParam = new Parameter {
            Id = "@", Nameless = true,
            ValueContainerType = ValueContainerType.Named,
            ValueContext = new ValueContext[] { null, new() { Type = ValueContextType.Actor, SubType = "@" } }
        };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { apParam, idParam } } };
        meta.Actors = new[] { new Actor { Id = "Ai", Type = "@", Appearances = new[] { "Normal" } } };
        Assert.Empty(Complete("@cmd x ap:", 10));
    }

    [Fact]
    public void WhenActorIdIsNotSpecifiedButHasDefaultValueAppearancesAreReturned ()
    {
        var param = new Parameter {
            Id = "@", Nameless = true,
            ValueContainerType = ValueContainerType.Named,
            ValueContext = new ValueContext[] { new() { Type = ValueContextType.Appearance, SubType = "MainBackground" } }
        };
        meta.Commands = new[] { new Command { Id = "back", Parameters = new[] { param } } };
        meta.Actors = new[] {
            new Actor { Id = "Another", Appearances = new[] { "Other" } },
            new Actor { Id = "MainBackground", Appearances = new[] { "Snow" } }
        };
        Assert.Equal("Snow", Complete("@back ", 6)[0].Label);
    }

    [Fact]
    public void WhenOverBooleanContextTrueAndFalseAreReturned ()
    {
        var param = new Parameter { Id = "id", ValueType = ValueType.Boolean };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        var items = Complete("@cmd id:x", 9);
        Assert.Equal(2, items.Count);
        Assert.Equal("true", items[0].Label);
        Assert.Equal("false", items[1].Label);
    }

    [Fact]
    public void WhenOverExpressionContextVariablesAndFunctionsAreReturned ()
    {
        var param = new Parameter { Id = "ex", ValueContext = new ValueContext[] { new() { Type = ValueContextType.Expression } } };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        meta.Variables = new[] { "foo" };
        meta.Functions = new[] { "bar" };
        var items = Complete("@cmd ex:", 8);
        Assert.Equal(2, items.Count);
        Assert.Equal("foo", items[0].Label);
        Assert.Equal("bar", items[1].Label);
    }

    [Fact]
    public void WhenOverConstantContextConstantValuesAreReturned ()
    {
        var param = new Parameter { Id = "ct", ValueContext = new ValueContext[] { new() { Type = ValueContextType.Constant, SubType = "foo" } } };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        meta.Constants = new[] { new Constant { Name = "foo", Values = new[] { "bar" } } };
        Assert.Equal("bar", Complete("@cmd ct:", 8)[0].Label);
    }

    [Fact]
    public void ConstantExpressionIsEvaluated ()
    {
        var param = new Parameter {
            Id = "Path",
            Nameless = true,
            ValueContainerType = ValueContainerType.Named,
            ValueContext = new ValueContext[] { null, new() { Type = ValueContextType.Constant, SubType = "Labels/{:Path[0]??$Script}+Test" } }
        };
        meta.Commands = new[] { new Command { Id = "Goto", Parameters = new[] { param } } };
        meta.Constants = new[] {
            new Constant { Name = "Labels/Script001", Values = new[] { "foo" } },
            new Constant { Name = "Labels/Script002", Values = new[] { "bar" } },
            new Constant { Name = "Test", Values = new[] { "test" } }
        };
        Assert.Equal("foo", Complete("@goto .", 7, "root/Script001.nani")[0].Label);
        Assert.Equal("bar", Complete("@goto Script002.", 16)[0].Label);
        Assert.Equal("test", Complete("@goto Script002.", 16)[1].Label);
    }

    [Fact]
    public void CanResolveOtherParameterWhenEvaluatingExpression ()
    {
        var foo = new Parameter {
            Id = "foo",
            ValueContext = new ValueContext[] { new() { Type = ValueContextType.Constant, SubType = "{:bar}" } }
        };
        var bar = new Parameter { Id = "bar" };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { foo, bar } } };
        meta.Constants = new[] { new Constant { Name = "Test", Values = new[] { "test" } } };
        Assert.Equal("test", Complete("@cmd baz: foo: bar:Test", 14)[0].Label);
    }

    [Fact]
    public void WhenUnknownParameterInConstantExpressionResultIsEmpty ()
    {
        var param = new Parameter { Id = "foo", ValueContext = new ValueContext[] { new() { Type = ValueContextType.Constant, SubType = "{:bar}" } } };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        Assert.Empty(Complete("@cmd foo:", 9));
    }

    [Fact]
    public void WhenOverConstantContextWithoutSubtypeEmptyIsReturned ()
    {
        var param = new Parameter { Id = "re", ValueContext = new ValueContext[] { new() { Type = ValueContextType.Constant } } };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        Assert.Empty(Complete("@cmd re:", 8));
    }

    [Fact]
    public void WhenOverResourceContextResourcePathsAreReturned ()
    {
        var param = new Parameter { Id = "re", ValueContext = new ValueContext[] { new() { Type = ValueContextType.Resource, SubType = "foo" } } };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        meta.Resources = new[] { new Resource { Type = "foo", Path = "nyan/pass" } };
        Assert.Equal("nyan/pass", Complete("@cmd re:", 8)[0].Label);
    }

    [Fact]
    public void WhenOverResourceContextWithoutSubtypeEmptyIsReturned ()
    {
        var param = new Parameter { Id = "re", ValueContext = new ValueContext[] { new() { Type = ValueContextType.Resource } } };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        meta.Resources = new[] { new Resource { Type = "foo", Path = "nyan/pass" } };
        Assert.Empty(Complete("@cmd re:", 8));
    }

    [Fact]
    public void WhenInsideCommandExpressionVariablesAndFunctionsAreReturned ()
    {
        var param = new Parameter { Id = "@", Nameless = true, ValueType = ValueType.Boolean };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        meta.Variables = new[] { "foo" };
        meta.Functions = new[] { "bar" };
        var expected = new[] { "foo", "bar" };
        Assert.Equal(expected, Complete("@cmd {", 6).Select(i => i.Label));
        Assert.Equal(expected, Complete("@cmd {x", 7).Select(i => i.Label));
        Assert.Equal(expected, Complete("@cmd x{x}x", 8).Select(i => i.Label));
    }

    [Fact]
    public void WhenOverCommandExpressionContextValuesAreReturned ()
    {
        var param = new Parameter { Id = "@", Nameless = true, ValueType = ValueType.Boolean };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        meta.Variables = new[] { "foo" };
        Assert.Equal("true", Complete("@cmd {x}", 8)[0].Label);
    }

    [Fact]
    public void WhenInsideGenericExpressionVariablesAndFunctionsAreReturned ()
    {
        meta.Variables = new[] { "foo" };
        meta.Functions = new[] { "bar" };
        var expected = new[] { "foo", "bar" };
        Assert.Equal(expected, Complete("{", 1).Select(i => i.Label));
        Assert.Equal(expected, Complete("{x", 2).Select(i => i.Label));
        Assert.Equal(expected, Complete("x{x}x", 3).Select(i => i.Label));
    }

    [Fact]
    public void WhenOverGenericExpressionResultIsEmpty ()
    {
        meta.Variables = new[] { "foo" };
        Assert.Empty(Complete("{x}", 3));
    }

    [Fact]
    public void IdAndAliasAreNotCaseSensitive ()
    {
        var param = new Parameter { Id = "Identifier", Alias = "id", ValueType = ValueType.Boolean };
        meta.Commands = new[] { new Command { Id = "Command", Alias = "cmd", Parameters = new[] { param } } };
        Assert.Equal("true", Complete("@CMD ID:", 8)[0].Label);
        Assert.Equal("true", Complete("@command identifier:", 20)[0].Label);
    }

    private IReadOnlyList<CompletionItem> Complete (string line, int charOffset, string uri = "@")
    {
        handler.HandleMetadataChanged(meta);
        docs.SetupScript(uri, line);
        return handler.Complete(uri, new Position(0, charOffset));
    }
}
