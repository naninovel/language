using Moq;
using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language.Test;

public class CompletionTest
{
    private readonly Project meta = new();
    private readonly Mock<IDocumentRegistry> docs = new();
    private readonly Mock<IEndpointRegistry> endpoints = new();
    private readonly CompletionHandler handler;

    public CompletionTest ()
    {
        handler = new(docs.Object, endpoints.Object);
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
        meta.Actors = [new Actor { Id = "Ai", Type = Constants.CharacterType }];
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
        meta.Actors = [new Actor { Id = "Ai", Type = Constants.CharacterType }];
        Assert.Equal("Ai", Complete("Ai: Hello.", 1)[0].Label);
        Assert.Equal("Ai", Complete("Ai.Happy: Hey!", 2)[0].Label);
    }

    [Fact]
    public void OnlyCharacterIdsAreReturnedOnAuthorCompletion ()
    {
        meta.Actors = [
            new Actor { Id = "Bubble", Type = "TextPrinters" },
            new Actor { Id = "Kohaku", Type = Constants.CharacterType }
        ];
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
        meta.Actors = [new Actor { Type = Constants.CharacterType, Description = "foo" }];
        Assert.Equal("foo", Complete("", 0)[0].Detail);
    }

    [Fact]
    public void AuthorCompletionHasValueKind ()
    {
        meta.Actors = [new Actor { Type = Constants.CharacterType }];
        Assert.Equal(CompletionItemKind.Value, Complete("", 0)[0].Kind);
    }

    [Fact]
    public void WhenOverAppearanceContentAppearancesAreReturned ()
    {
        meta.Actors = [new Actor { Id = "Ai", Appearances = ["Happy"], Type = Constants.CharacterType }];
        Assert.Equal("Happy", Complete("Ai.Happy: Hi!", 3)[0].Label);
    }

    [Fact]
    public void WhenAfterDotPrefixedByNonWhitespaceCharactersAppearancesAreReturned ()
    {
        meta.Actors = [new Actor { Id = "Ai", Appearances = ["Happy"], Type = Constants.CharacterType }];
        Assert.Equal("Happy", Complete("Ai.", 3)[0].Label);
    }

    [Fact]
    public void WhenAfterDotPrefixedByTextWithWhitespaceResultIsEmpty ()
    {
        meta.Actors = [new Actor { Id = "Ai", Appearances = ["Happy"], Type = Constants.CharacterType }];
        Assert.Empty(Complete("Ai .", 4));
    }

    [Fact]
    public void WhenNoAppearancesOnAppearanceCompletionResultIsEmpty ()
    {
        meta.Actors = [new Actor { Id = "Ai", Type = Constants.CharacterType }];
        Assert.Empty(Complete("Ai.", 3));
    }

    [Fact]
    public void WhenOverInlinedCommandOpenCommandsAreReturned ()
    {
        meta.Commands = [new Metadata.Command { Id = "cmd" }];
        Assert.Equal("cmd", Complete("[", 1)[0].Label);
    }

    [Fact]
    public void WhenOverInlinedCommandIdContentCommandsAreReturned ()
    {
        meta.Commands = [new Metadata.Command { Id = "cmd" }];
        Assert.Equal("cmd", Complete("[x", 2)[0].Label);
    }

    [Fact]
    public void WhenOverCommandLineIdCommandsAreReturned ()
    {
        meta.Commands = [new Metadata.Command { Id = "cmd" }];
        Assert.Equal("cmd", Complete("@", 1)[0].Label);
    }

    [Fact]
    public void WhenOverCommandIdContentCommandsAreReturned ()
    {
        meta.Commands = [new Metadata.Command { Id = "cmd" }];
        Assert.Equal("cmd", Complete("@x", 2)[0].Label);
    }

    [Fact]
    public void CommandSummaryIsAssignedToDocumentation ()
    {
        meta.Commands = [new Metadata.Command { Id = "cmd", Summary = "foo" }];
        Assert.Equal("foo", Complete("[", 1)[0].Documentation?.Value);
    }

    [Fact]
    public void WhenOverCommandContentParametersAreReturned ()
    {
        var param = new Metadata.Parameter { Id = "foo" };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        Assert.Equal("foo", Complete("@cmd ", 5)[0].Label);
    }

    [Fact]
    public void WhenAfterCommandWithNamelessParameterValuesAreReturned ()
    {
        var param = new Metadata.Parameter { Id = "foo", Nameless = true, ValueType = Metadata.ValueType.Boolean };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
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
        meta.Commands = [new Metadata.Command { Id = "cmd" }];
        Assert.Empty(Complete("@cmd x", 6));
        Assert.Empty(Complete("@cmd x:y", 8));
    }

    [Fact]
    public void WhenOverNamelessParameterValuesAreReturned ()
    {
        var param = new Metadata.Parameter { Id = "foo", Nameless = true, ValueType = Metadata.ValueType.Boolean };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        Assert.Equal("true", Complete("@cmd x", 6)[0].Label);
    }

    [Fact]
    public void WhenOverNamelessParameterWithTextIdValuesAreReturned ()
    {
        var param = new Metadata.Parameter { Id = "foo", Nameless = true, ValueType = Metadata.ValueType.Boolean };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        Assert.Equal("true", Complete("@cmd x|x|", 6)[0].Label);
    }

    [Fact]
    public void WhenOverParameterAssignmentValuesAreReturned ()
    {
        var param = new Metadata.Parameter { Id = "id", ValueType = Metadata.ValueType.Boolean };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        Assert.Equal("true", Complete("@cmd id:", 8)[0].Label);
    }

    [Fact]
    public void ParameterSummaryIsAssignedToDocumentation ()
    {
        var param = new Metadata.Parameter { Id = "foo", Summary = "bar" };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        Assert.Equal("bar", Complete("@cmd ", 5)[0].Documentation?.Value);
    }

    [Fact]
    public void ParameterDefaultValueIsAssignedToDetails ()
    {
        var foo = new Metadata.Parameter { Id = "foo", DefaultValue = "foo default" };
        var bar = new Metadata.Parameter { Id = "bar" };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [foo, bar] }];
        Assert.Equal("Default value: foo default", Complete("@cmd ", 5)[0].Detail);
        Assert.Empty(Complete("@cmd ", 5)[1].Detail);
    }

    [Fact]
    public void ParametersWithoutContextAreNotCompleted ()
    {
        var param = new Metadata.Parameter { Id = "x" };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        Assert.Empty(Complete("@cmd x:", 7));
    }

    [Fact]
    public void WhenOverParameterValueContentValuesAreReturned ()
    {
        var param = new Metadata.Parameter { Id = "id", ValueType = Metadata.ValueType.Boolean };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        Assert.Equal("true", Complete("@cmd id:x", 9)[0].Label);
    }

    [Fact]
    public void WhenOverUnknownContextTypeResultIsEmpty ()
    {
        var param = new Metadata.Parameter { Id = "x", ValueContext = [new() { Type = (ValueContextType)255 }] };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        Assert.Empty(Complete("@cmd x:", 7));
    }

    [Fact]
    public void WhenOverActorContextActorIdsAreReturned ()
    {
        var param = new Metadata.Parameter { Id = "id", ValueContext = [new() { Type = ValueContextType.Actor, SubType = "foo" }] };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        meta.Actors = [new Actor { Id = "1", Type = "foo" }];
        Assert.Equal("1", Complete("@cmd id:x", 9)[0].Label);
    }

    [Fact]
    public void WhenOverActorContextWithoutSubtypeEmptyIsReturned ()
    {
        var param = new Metadata.Parameter { Id = "id", ValueContext = [new() { Type = ValueContextType.Actor }] };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        meta.Actors = [new Actor { Id = "1", Type = "foo" }];
        Assert.Empty(Complete("@cmd id:x", 9));
    }

    [Fact]
    public void WhenWildcardSpecifiedForActorTypeAllActorsAreReturned ()
    {
        var param = new Metadata.Parameter { Id = "id", ValueContext = [new() { Type = ValueContextType.Actor, SubType = "*" }] };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        meta.Actors = [
            new Actor { Id = "1", Type = "foo" },
            new Actor { Id = "2", Type = "bar" }
        ];
        Assert.Equal("1", Complete("@cmd id:", 8)[0].Label);
        Assert.Equal("2", Complete("@cmd id:", 8)[1].Label);
    }

    [Fact]
    public void WhenOverAppearanceContextActorAppearancesAreReturned ()
    {
        var idParam = new Metadata.Parameter { Id = "id", ValueContext = [new() { Type = ValueContextType.Actor, SubType = "@" }] };
        var apParam = new Metadata.Parameter { Id = "ap", ValueContext = [new() { Type = ValueContextType.Appearance }] };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [idParam, apParam] }];
        meta.Actors = [new Actor { Id = "Ai", Type = "@", Appearances = ["Normal"] }];
        Assert.Equal("Normal", Complete("@cmd id:Ai ap:", 14)[0].Label);
    }

    [Fact]
    public void WhenOverAppearanceContextButParameterWithActorContextIsMissingResultIsEmpty ()
    {
        var idParam = new Metadata.Parameter { Id = "id" };
        var apParam = new Metadata.Parameter { Id = "ap", ValueContext = [new() { Type = ValueContextType.Appearance }] };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [idParam, apParam] }];
        meta.Actors = [new Actor { Id = "Ai", Type = "@", Appearances = ["Normal"] }];
        Assert.Empty(Complete("@cmd id:Ai ap:", 14));
    }

    [Fact]
    public void WhenOverAppearanceContextButParameterWithActorContextIsNotFoundResultIsEmpty ()
    {
        var apParam = new Metadata.Parameter { Id = "ap", ValueContext = [new() { Type = ValueContextType.Appearance }] };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [apParam] }];
        Assert.Empty(Complete("@cmd id:Ai ap:", 14));
    }

    [Fact]
    public void WhenOverNamedValueAppearanceContextActorAppearancesAreReturned ()
    {
        var param = new Metadata.Parameter {
            Id = "@", Nameless = true,
            ValueContainerType = ValueContainerType.Named,
            ValueContext = [
                new() { Type = ValueContextType.Actor, SubType = "@" },
                new() { Type = ValueContextType.Appearance }
            ]
        };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        meta.Actors = [new Actor { Id = "Ai", Type = "@", Appearances = ["Normal"] }];
        Assert.Equal("Normal", Complete("@cmd Ai.", 8)[0].Label);
    }

    [Fact]
    public void WhenOverNamedNameAppearanceContextActorAppearancesAreReturned ()
    {
        var param = new Metadata.Parameter {
            Id = "@", Nameless = true,
            ValueContainerType = ValueContainerType.Named,
            ValueContext = [
                new() { Type = ValueContextType.Appearance },
                new() { Type = ValueContextType.Actor, SubType = "@" }
            ]
        };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        meta.Actors = [new Actor { Id = "Ai", Type = "@", Appearances = ["Normal"] }];
        Assert.Equal("Normal", Complete("@cmd .Ai", 5)[0].Label);
    }

    [Fact]
    public void WhenOverAppearanceContextAndActorIsSpecifiedInNamedParameterAppearancesAreReturned ()
    {
        var apParam = new Metadata.Parameter { Id = "ap", ValueContext = [new() { Type = ValueContextType.Appearance }] };
        var idParam = new Metadata.Parameter {
            Id = "@", Nameless = true,
            ValueContainerType = ValueContainerType.Named,
            ValueContext = [new() { Type = ValueContextType.Actor, SubType = "@" }]
        };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [apParam, idParam] }];
        meta.Actors = [new Actor { Id = "Ai", Type = "@", Appearances = ["Normal"] }];
        Assert.Equal("Normal", Complete("@cmd Ai ap:", 11)[0].Label);
    }

    [Fact]
    public void WhenOverAppearanceContextButActorIdIsNotSpecifiedResultIsEmpty ()
    {
        var apParam = new Metadata.Parameter { Id = "ap", ValueContext = [new() { Type = ValueContextType.Appearance }] };
        var idParam = new Metadata.Parameter {
            Id = "@", Nameless = true,
            ValueContainerType = ValueContainerType.Named,
            ValueContext = [null, new() { Type = ValueContextType.Actor, SubType = "@" }]
        };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [apParam, idParam] }];
        meta.Actors = [new Actor { Id = "Ai", Type = "@", Appearances = ["Normal"] }];
        Assert.Empty(Complete("@cmd x ap:", 10));
    }

    [Fact]
    public void WhenActorIdIsNotSpecifiedButHasDefaultValueAppearancesAreReturned ()
    {
        var param = new Metadata.Parameter {
            Id = "@", Nameless = true,
            ValueContainerType = ValueContainerType.Named,
            ValueContext = [new() { Type = ValueContextType.Appearance, SubType = "MainBackground" }]
        };
        meta.Commands = [new Metadata.Command { Id = "back", Parameters = [param] }];
        meta.Actors = [
            new Actor { Id = "Another", Appearances = ["Other"], Type = Constants.BackgroundType },
            new Actor { Id = "MainBackground", Appearances = ["Snow"], Type = Constants.BackgroundType }
        ];
        Assert.Equal("Snow", Complete("@back ", 6)[0].Label);
    }

    [Fact]
    public void WhenOverBooleanContextTrueAndFalseAreReturned ()
    {
        var param = new Metadata.Parameter { Id = "id", ValueType = Metadata.ValueType.Boolean };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        var items = Complete("@cmd id:x", 9);
        Assert.Equal(2, items.Count);
        Assert.Equal("true", items[0].Label);
        Assert.Equal("false", items[1].Label);
    }

    [Fact]
    public void WhenOverExpressionContextVariablesAndFunctionsAreReturned ()
    {
        var param = new Metadata.Parameter { Id = "ex", ValueContext = [new() { Type = ValueContextType.Expression }] };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        meta.Variables = ["foo"];
        meta.Functions = ["bar"];
        var items = Complete("@cmd ex:", 8);
        Assert.Equal(2, items.Count);
        Assert.Equal("foo", items[0].Label);
        Assert.Equal("bar", items[1].Label);
    }

    [Fact]
    public void WhenOverConstantContextConstantValuesAreReturned ()
    {
        var param = new Metadata.Parameter { Id = "ct", ValueContext = [new() { Type = ValueContextType.Constant, SubType = "foo" }] };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        meta.Constants = [new Constant { Name = "foo", Values = ["bar"] }];
        Assert.Equal("bar", Complete("@cmd ct:", 8)[0].Label);
    }

    [Fact]
    public void ConstantExpressionInNamelessParamEvaluated ()
    {
        var param = new Metadata.Parameter {
            Id = "Path",
            Nameless = true,
            ValueContainerType = ValueContainerType.Named,
            ValueContext = [null, new() { Type = ValueContextType.Constant, SubType = "Labels/{:Path[0]??$Script}+Test" }]
        };
        meta.Commands = [new Metadata.Command { Id = "Goto", Parameters = [param] }];
        meta.Constants = [
            new Constant { Name = "Labels/Script001", Values = ["foo"] },
            new Constant { Name = "Labels/Script002", Values = ["bar"] },
            new Constant { Name = "Test", Values = ["test"] }
        ];
        Assert.Equal("foo", Complete("@goto .", 7, "root/Script001.nani")[0].Label);
        Assert.Equal("bar", Complete("@goto Script002.", 16)[0].Label);
        Assert.Equal("test", Complete("@goto Script002.", 16)[1].Label);
    }

    [Fact]
    public void ConstantExpressionInNamedParamEvaluated ()
    {
        var summaryParam = new Metadata.Parameter {
            Id = "Summary",
            Nameless = true
        };
        var gotoParam = new Metadata.Parameter {
            Id = "GotoPath",
            Alias = "goto",
            ValueContainerType = ValueContainerType.Named,
            ValueContext = [
                new() { Type = ValueContextType.Resource, SubType = "Scripts" },
                new() { Type = ValueContextType.Constant, SubType = "Labels/{:GotoPath[0]??$Script}+Test" }
            ]
        };
        meta.Commands = [new Metadata.Command { Id = "AddChoice", Alias = "choice", Parameters = [summaryParam, gotoParam] }];
        meta.Resources = [
            new Resource { Path = "Script001", Type = "Scripts" },
            new Resource { Path = "Script002", Type = "Scripts" }
        ];
        meta.Constants = [
            new Constant { Name = "Labels/Script001", Values = ["foo"] },
            new Constant { Name = "Labels/Script002", Values = ["bar"] },
            new Constant { Name = "Test", Values = ["test"] }
        ];
        Assert.Equal("Script001", Complete("@choice x goto:", 15)[0].Label);
        Assert.Equal("Script002", Complete("@choice x goto:", 15)[1].Label);
        Assert.Equal("foo", Complete("@choice x goto:.", 16, "root/Script001.nani")[0].Label);
        Assert.Equal("bar", Complete("@choice x goto:Script002.", 25)[0].Label);
        Assert.Equal("test", Complete("@choice x goto:Script002.", 25)[1].Label);
    }

    [Fact]
    public void CanResolveOtherParameterWhenEvaluatingExpression ()
    {
        var foo = new Metadata.Parameter {
            Id = "foo",
            ValueContext = [new() { Type = ValueContextType.Constant, SubType = "{:bar}" }]
        };
        var bar = new Metadata.Parameter { Id = "bar" };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [foo, bar] }];
        meta.Constants = [new Constant { Name = "Test", Values = ["test"] }];
        Assert.Equal("test", Complete("@cmd baz: foo: bar:Test", 14)[0].Label);
    }

    [Fact]
    public void WhenUnknownParameterInConstantExpressionResultIsEmpty ()
    {
        var param = new Metadata.Parameter { Id = "foo", ValueContext = [new() { Type = ValueContextType.Constant, SubType = "{:bar}" }] };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        Assert.Empty(Complete("@cmd foo:", 9));
    }

    [Fact]
    public void WhenOverConstantContextWithoutSubtypeEmptyIsReturned ()
    {
        var param = new Metadata.Parameter { Id = "re", ValueContext = [new() { Type = ValueContextType.Constant }] };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        Assert.Empty(Complete("@cmd re:", 8));
    }

    [Fact]
    public void WhenOverResourceContextResourcePathsAreReturned ()
    {
        var param = new Metadata.Parameter { Id = "re", ValueContext = [new() { Type = ValueContextType.Resource, SubType = "foo" }] };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        meta.Resources = [new Resource { Type = "foo", Path = "nyan/pass" }];
        Assert.Equal("nyan/pass", Complete("@cmd re:", 8)[0].Label);
    }

    [Fact]
    public void WhenOverResourceContextWithoutSubtypeEmptyIsReturned ()
    {
        var param = new Metadata.Parameter { Id = "re", ValueContext = [new() { Type = ValueContextType.Resource }] };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        meta.Resources = [new Resource { Type = "foo", Path = "nyan/pass" }];
        Assert.Empty(Complete("@cmd re:", 8));
    }

    [Fact]
    public void WhenInsideCommandExpressionVariablesAndFunctionsAreReturned ()
    {
        var param = new Metadata.Parameter { Id = "@", Nameless = true, ValueType = Metadata.ValueType.Boolean };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        meta.Variables = ["foo"];
        meta.Functions = ["bar"];
        var expected = new[] { "foo", "bar" };
        Assert.Equal(expected, Complete("@cmd {", 6).Select(i => i.Label));
        Assert.Equal(expected, Complete("@cmd {x", 7).Select(i => i.Label));
        Assert.Equal(expected, Complete("@cmd x{x}x", 8).Select(i => i.Label));
    }

    [Fact]
    public void WhenOverCommandExpressionContextValuesAreReturned ()
    {
        var param = new Metadata.Parameter { Id = "@", Nameless = true, ValueType = Metadata.ValueType.Boolean };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [param] }];
        meta.Variables = ["foo"];
        Assert.Equal("true", Complete("@cmd {x}", 8)[0].Label);
    }

    [Fact]
    public void WhenInsideGenericExpressionVariablesAndFunctionsAreReturned ()
    {
        meta.Variables = ["foo"];
        meta.Functions = ["bar"];
        var expected = new[] { "foo", "bar" };
        Assert.Equal(expected, Complete("{", 1).Select(i => i.Label));
        Assert.Equal(expected, Complete("{x", 2).Select(i => i.Label));
        Assert.Equal(expected, Complete("x{x}x", 3).Select(i => i.Label));
    }

    [Fact]
    public void WhenOverGenericExpressionResultIsEmpty ()
    {
        meta.Variables = ["foo"];
        Assert.Empty(Complete("{x}", 3));
    }

    [Fact]
    public void IdAndAliasAreNotCaseSensitive ()
    {
        var param = new Metadata.Parameter { Id = "Identifier", Alias = "id", ValueType = Metadata.ValueType.Boolean };
        meta.Commands = [new Metadata.Command { Id = "Command", Alias = "cmd", Parameters = [param] }];
        Assert.Equal("true", Complete("@CMD ID:", 8)[0].Label);
        Assert.Equal("true", Complete("@command identifier:", 20)[0].Label);
    }

    [Fact]
    public void WhenNoEndpointsCompletesEmpty ()
    {
        meta.SetupCommandWithEndpoint("goto");
        endpoints.Setup(e => e.GetAllScriptNames()).Returns(new HashSet<string>());
        Assert.Empty(Complete("@goto ", 6));
    }

    [Fact]
    public void CanCompleteScriptEndpoint ()
    {
        meta.SetupCommandWithEndpoint("goto");
        endpoints.Setup(e => e.GetAllScriptNames()).Returns(new HashSet<string> { "ScriptA", "ScriptB" });
        Assert.Equal("ScriptA", Complete("@goto ", 6)[0].Label);
        Assert.Equal("ScriptB", Complete("@goto ", 6)[1].Label);
    }

    [Fact]
    public void WhenNoLabelsInScriptCompletesEmptyForLabel ()
    {
        meta.SetupCommandWithEndpoint("goto");
        endpoints.Setup(e => e.GetLabelsInScript("ScriptA")).Returns(new HashSet<string>());
        Assert.Empty(Complete("@goto ScriptA.", 14));
    }

    [Fact]
    public void CanCompleteLabelEndpoints ()
    {
        meta.SetupCommandWithEndpoint("goto");
        endpoints.Setup(e => e.GetLabelsInScript("ScriptA")).Returns(new HashSet<string> { "LabelA", "LabelB" });
        Assert.Equal("LabelA", Complete("@goto ScriptA.", 14)[0].Label);
        Assert.Equal("LabelB", Complete("@goto ScriptA.", 14)[1].Label);
    }

    [Fact]
    public void CanCompleteLabelEndpointsForCurrentScript ()
    {
        meta.SetupCommandWithEndpoint("goto");
        endpoints.Setup(e => e.GetLabelsInScript("ScriptA")).Returns(new HashSet<string> { "LabelA", "LabelB" });
        Assert.Equal("LabelA", Complete("@goto .", 7, "ScriptA.nani")[0].Label);
        Assert.Equal("LabelB", Complete("@goto .", 7, "ScriptA.nani")[1].Label);
    }

    [Fact]
    public void DoesntCompleteWhenFailedToResolveNamelessRange ()
    {
        var foo = new Metadata.Parameter { Id = "foo", ValueType = Metadata.ValueType.Boolean, Nameless = true };
        var bar = new Metadata.Parameter { Id = "bar" };
        meta.Commands = [new Metadata.Command { Id = "cmd", Parameters = [foo, bar] }];
        handler.HandleMetadataChanged(meta);
        var mapper = new Mock<RangeMapper>();
        docs.Setup(d => d.Get("@")).Returns(new Document([new DocumentLine("@cmd ", new CommandLine(new("cmd")), [], mapper.Object)]));
        Assert.Equal("foo", handler.Complete("@", new Position(0, 5))[0].Label);
    }

    private IReadOnlyList<CompletionItem> Complete (string line, int charOffset, string uri = "@")
    {
        handler.HandleMetadataChanged(meta);
        docs.SetupScript(uri, line);
        return handler.Complete(uri, new Position(0, charOffset));
    }
}
