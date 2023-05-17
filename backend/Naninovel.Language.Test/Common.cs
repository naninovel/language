using System;
using System.Linq;
using Moq;
using Naninovel.Metadata;

namespace Naninovel.Language.Test;

internal static class Common
{
    public static Document CreateDocument (params string[] lines)
    {
        return new DocumentFactory().CreateDocument(string.Join('\n', lines));
    }

    public static void SetupScript (this Mock<IDocumentRegistry> docs, string uri, params string[] lines)
    {
        docs.Setup(d => d.Get(uri)).Returns(CreateDocument(lines));
        // ReSharper disable once ConstantNullCoalescingCondition
        var uris = (docs.Object.GetAllUris() ?? Array.Empty<string>()).Append(uri).ToArray();
        docs.Setup(d => d.GetAllUris()).Returns(uris);
    }

    public static Project SetupCommandWithEndpoint (this Project meta, string commandId)
    {
        var context = new[] {
            new ValueContext(),
            new ValueContext { Type = ValueContextType.Constant, SubType = Constants.LabelExpression }
        };
        var parameter = new Parameter {
            Id = "",
            Nameless = true,
            ValueType = Metadata.ValueType.String,
            ValueContainerType = ValueContainerType.Named,
            ValueContext = context
        };
        var command = new Command { Id = commandId, Parameters = new[] { parameter } };
        meta.Commands = meta.Commands.Append(command).ToArray();
        return meta;
    }
}
