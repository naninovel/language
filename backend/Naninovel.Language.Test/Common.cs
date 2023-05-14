using System;
using System.Linq;
using Moq;
using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language.Test;

internal static class Common
{
    public static void SetupScript (this Mock<IDocumentRegistry> docs, string uri, params string[] lines)
    {
        var document = new DocumentFactory().CreateDocument(string.Join('\n', lines));
        docs.Setup(d => d.Get(uri)).Returns(document);
        // ReSharper disable once ConstantNullCoalescingCondition
        var uris = (docs.Object.GetAllUris() ?? Array.Empty<string>()).Append(uri).ToArray();
        docs.Setup(d => d.GetAllUris()).Returns(uris);
        docs.Setup(d => d.Contains(It.Is<string>(s => s == uri), It.IsAny<string>()))
            .Returns((string uri, string label) =>
                string.IsNullOrEmpty(label) ||
                document.Lines.Any(l => l.Script is LabelLine ll && ll.Label == label));
    }

    public static void SetupCommandWithEndpoint (this Project meta, string commandId)
    {
        var context = new[] {
            new ValueContext(),
            new ValueContext { Type = ValueContextType.Constant, SubType = Constants.LabelExpression }
        };
        var parameter = new Metadata.Parameter {
            Id = "",
            Nameless = true,
            ValueType = Metadata.ValueType.String,
            ValueContainerType = ValueContainerType.Named,
            ValueContext = context
        };
        var command = new Metadata.Command { Id = commandId, Parameters = new[] { parameter } };
        meta.Commands = meta.Commands.Append(command).ToArray();
    }
}
