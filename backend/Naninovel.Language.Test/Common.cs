using Moq;
using Naninovel.Metadata;

namespace Naninovel.Language.Test;

internal static class Common
{
    public static Document CreateDocument (params string[] lines)
    {
        var factory = new DocumentFactory(new MetadataMock());
        return factory.CreateDocument(string.Join('\n', lines));
    }

    public static Document CreateDocument (IMetadata meta, params string[] lines)
    {
        var factory = new DocumentFactory(meta);
        return factory.CreateDocument(string.Join('\n', lines));
    }

    public static void SetupScript (this Mock<IDocumentRegistry> docs, string uri, params string[] lines)
    {
        docs.Setup(d => d.Get(uri)).Returns(CreateDocument(lines));
        // ReSharper disable once ConstantNullCoalescingCondition
        var uris = (docs.Object.GetAllUris() ?? []).Append(uri).ToHashSet();
        docs.Setup(d => d.GetAllUris()).Returns(uris);
    }

    public static void SetupScript (this Mock<IDocumentRegistry> docs, IMetadata meta, string uri, params string[] lines)
    {
        docs.Setup(d => d.Get(uri)).Returns(CreateDocument(meta, lines));
        // ReSharper disable once ConstantNullCoalescingCondition
        var uris = (docs.Object.GetAllUris() ?? []).Append(uri).ToHashSet();
        docs.Setup(d => d.GetAllUris()).Returns(uris);
    }

    public static void SetupCommandWithEndpoint (this MetadataMock meta, string commandId)
    {
        var parameter = new Parameter {
            Id = "Path",
            Nameless = true,
            ValueType = Metadata.ValueType.String,
            ValueContainerType = ValueContainerType.Named,
            ValueContext = [
                new ValueContext { Type = ValueContextType.Endpoint, SubType = Constants.EndpointScript },
                new ValueContext { Type = ValueContextType.Endpoint, SubType = Constants.EndpointLabel }
            ]
        };
        var command = new Command { Id = commandId, Parameters = [parameter] };
        meta.Commands = meta.Commands.Append(command).ToList();
    }
}
