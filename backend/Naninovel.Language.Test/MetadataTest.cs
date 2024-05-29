using Moq;
using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language.Test;

public class MetadataTest
{
    private readonly NotifierMock<IMetadataObserver> notifier = new();
    private readonly MetadataHandler handler;

    public MetadataTest ()
    {
        handler = new(notifier);
    }

    [Fact]
    public void NotifiesOnMetadataUpdate ()
    {
        var meta = new Project();
        handler.UpdateMetadata(meta);
        notifier.Verify(n => n.HandleMetadataChanged(meta), Times.Once);
        notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public void ProviderUpdatesMetadata ()
    {
        var provider = new MetadataProvider();
        provider.HandleMetadataChanged(new() {
            Commands = [new() { Id = "cmd", Parameters = [new() { Id = "p" }] }],
            Actors = [new() { Id = "actor" }],
            Resources = [new() { Path = "res" }],
            Constants = [new() { Name = "const" }],
            Variables = ["var"],
            Functions = [new() { Name = "fn", Parameters = [new() { Name = "p" }] }],
            Syntax = new Syntax(commentLine: "%")
        });
        Assert.Equal("cmd", provider.Commands.First().Id);
        Assert.Equal("actor", provider.Actors.First().Id);
        Assert.Equal("res", provider.Resources.First().Path);
        Assert.Equal("const", provider.Constants.First().Name);
        Assert.Equal("var", provider.Variables.First());
        Assert.Equal("fn", provider.Functions.First().Name);
        Assert.Equal("%", provider.Syntax.CommentLine);
        Assert.Equal("cmd", provider.FindCommand("cmd")!.Id);
        Assert.Equal("p", provider.FindParameter("cmd", "p")!.Id);
        Assert.Equal("fn", provider.FindFunction("fn")!.Name);
        Assert.Equal("p", provider.FindFunctionParameter("fn", "p")!.Name);
        Assert.Equal("p", provider.FindFunctionParameter("fn", 0)!.Name);
    }
}
