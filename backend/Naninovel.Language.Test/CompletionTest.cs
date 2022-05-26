using System.Linq;
using Naninovel.Metadata;
using Xunit;

namespace Naninovel.Language.Test;

public class CompletionTest
{
    private readonly Project meta = new();

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
        var param = new Parameter { Id = "x", ValueContext = new() { Type = (ValueContextType)255 } };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        Assert.Empty(Complete("@cmd x:", 7));
    }

    [Fact]
    public void WhenOverActorContextActorIdsAreReturned ()
    {
        var param = new Parameter { Id = "id", ValueContext = new() { Type = ValueContextType.Actor, SubType = "foo" } };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        meta.Actors = new[] { new Actor { Id = "1", Type = "foo" } };
        Assert.Equal("1", Complete("@cmd id:x", 9)[0].Label);
    }

    [Fact]
    public void WhenWildcardSpecifiedForActorTypeAllActorsAreReturned ()
    {
        var param = new Parameter { Id = "id", ValueContext = new() { Type = ValueContextType.Actor, SubType = "*" } };
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
        var idParam = new Parameter { Id = "id", ValueContext = new() { Type = ValueContextType.Actor, SubType = "@" } };
        var apParam = new Parameter { Id = "ap", ValueContext = new() { Type = ValueContextType.Appearance } };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { idParam, apParam } } };
        meta.Actors = new[] { new Actor { Id = "Ai", Type = "@", Appearances = new[] { "Normal" } } };
        Assert.Equal("Normal", Complete("@cmd id:Ai ap:", 14)[0].Label);
    }

    [Fact]
    public void WhenOverAppearanceContextButParameterWithActorContextIsNotFoundResultIsEmpty ()
    {
        var apParam = new Parameter { Id = "ap", ValueContext = new() { Type = ValueContextType.Appearance } };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { apParam } } };
        Assert.Empty(Complete("@cmd id:Ai ap:", 14));
    }

    [Fact]
    public void WhenOverNamedValueAppearanceContextActorAppearancesAreReturned ()
    {
        var param = new Parameter {
            Id = "@", Nameless = true,
            ValueContainerType = ValueContainerType.Named,
            ValueContext = new() { Type = ValueContextType.Actor, SubType = "@" },
            NamedValueContext = new() { Type = ValueContextType.Appearance }
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
            ValueContext = new() { Type = ValueContextType.Appearance },
            NamedValueContext = new() { Type = ValueContextType.Actor, SubType = "@" }
        };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        meta.Actors = new[] { new Actor { Id = "Ai", Type = "@", Appearances = new[] { "Normal" } } };
        Assert.Equal("Normal", Complete("@cmd .Ai", 5)[0].Label);
    }

    [Fact]
    public void WhenOverAppearanceContextAndActorIsSpecifiedInNamedParameterAppearancesAreReturned ()
    {
        var apParam = new Parameter { Id = "ap", ValueContext = new() { Type = ValueContextType.Appearance } };
        var idParam = new Parameter {
            Id = "@", Nameless = true,
            ValueContainerType = ValueContainerType.Named,
            ValueContext = new() { Type = ValueContextType.Actor, SubType = "@" }
        };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { apParam, idParam } } };
        meta.Actors = new[] { new Actor { Id = "Ai", Type = "@", Appearances = new[] { "Normal" } } };
        Assert.Equal("Normal", Complete("@cmd Ai ap:", 11)[0].Label);
    }

    [Fact]
    public void WhenOverAppearanceContextButActorIdIsNotSpecifiedResultIsEmpty ()
    {
        var apParam = new Parameter { Id = "ap", ValueContext = new() { Type = ValueContextType.Appearance } };
        var idParam = new Parameter {
            Id = "@", Nameless = true,
            ValueContainerType = ValueContainerType.Named,
            NamedValueContext = new() { Type = ValueContextType.Actor, SubType = "@" }
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
            ValueContext = new() { Type = ValueContextType.Appearance, SubType = "MainBackground" }
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
        Assert.Equal(2, items.Length);
        Assert.Equal("true", items[0].Label);
        Assert.Equal("false", items[1].Label);
    }

    [Fact]
    public void WhenOverExpressionContextVariablesAndFunctionsAreReturned ()
    {
        var param = new Parameter { Id = "ex", ValueContext = new() { Type = ValueContextType.Expression } };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        meta.Variables = new[] { "foo" };
        meta.Functions = new[] { "bar" };
        var items = Complete("@cmd ex:", 8);
        Assert.Equal(2, items.Length);
        Assert.Equal("foo", items[0].Label);
        Assert.Equal("bar", items[1].Label);
    }

    [Fact]
    public void WhenOverConstantContextConstantValuesAreReturned ()
    {
        var param = new Parameter { Id = "ct", ValueContext = new() { Type = ValueContextType.Constant, SubType = "foo" } };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        meta.Constants = new[] { new Constant { Name = "foo", Values = new[] { "bar" } } };
        Assert.Equal("bar", Complete("@cmd ct:", 8)[0].Label);
    }

    [Fact]
    public void WhenOverResourceContextResourcePathsAreReturned ()
    {
        var param = new Parameter { Id = "re", ValueContext = new() { Type = ValueContextType.Resource, SubType = "foo" } };
        meta.Commands = new[] { new Command { Id = "cmd", Parameters = new[] { param } } };
        meta.Resources = new[] { new Resource { Type = "foo", Path = "nyan/pass" } };
        Assert.Equal("nyan/pass", Complete("@cmd re:", 8)[0].Label);
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

    private CompletionItem[] Complete (string lineText, int charOffset)
    {
        var registry = new DocumentRegistry();
        var handler = new CompletionHandler(new MetadataProvider(meta), registry);
        new DocumentHandler(registry, new MockDiagnoser()).Open("@", lineText);
        return handler.Complete("@", new Position(0, charOffset));
    }
}
