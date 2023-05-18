using System.Collections.Generic;
using Naninovel.Metadata;
using Naninovel.Parsing;
using static Naninovel.Language.Common;

namespace Naninovel.Language;

internal class EndpointRegistry
{
    private const string? noLabel = null;
    private readonly Dictionary<(string name, int line), string> labels = new();
    private readonly Dictionary<(string name, int line), (string name, string? label)> endpoints = new();
    private readonly Dictionary<(string name, string? label), int> labelCount = new();
    private readonly Dictionary<(string name, string? label), int> endpointCount = new();
    private readonly EndpointResolver resolver;

    public EndpointRegistry (MetadataProvider metaProvider)
    {
        resolver = new(metaProvider);
    }

    public bool Contains (string uriOrName, string? label = noLabel)
    {
        return labelCount.ContainsKey((ToScriptName(uriOrName), label));
    }

    public bool IsUsed (string uriOrName, string? label = noLabel)
    {
        return endpointCount.ContainsKey((ToScriptName(uriOrName), label));
    }

    public void HandleDocumentAdded (string uriOrName)
    {
        labelCount.TryAdd((ToScriptName(uriOrName), noLabel), 0);
    }

    public void HandleDocumentRemoved (string uriOrName)
    {
        var name = ToScriptName(uriOrName);
        labelCount.Remove((name, noLabel));
    }

    public void HandleLinesAdded (string uriOrName, IReadOnlyList<DocumentLine> lines, in LineRange range)
    {
        var name = ToScriptName(uriOrName);
        for (var i = range.Start; i <= range.End; i++)
            if (lines[i].Script is LabelLine labelLine)
                HandleLabelAdded(i, labelLine.Label);
            else if (lines[i].Script is CommandLine commandLine)
                HandleCommandAdded(commandLine.Command, i);
            else if (lines[i].Script is GenericLine genericLine)
                foreach (var content in genericLine.Content)
                    if (content is InlinedCommand inlined)
                        HandleCommandAdded(inlined.Command, i);

        void HandleLabelAdded (int line, string label)
        {
            labels[(name, line)] = label;
            if (!labelCount.TryAdd((name, label), 1))
                labelCount[(name, label)] += 1;
        }

        void HandleCommandAdded (Parsing.Command command, int line)
        {
            if (resolver.TryResolve(command, out var point))
                HandleEndpointAdded(line, (point.Script ?? name, point.Label));
        }

        void HandleEndpointAdded (int line, (string name, string? label) endpoint)
        {
            endpoints[(name, line)] = endpoint;
            if (!endpointCount.TryAdd(endpoint, 1))
                endpointCount[endpoint] += 1;
            if (!endpointCount.TryAdd((endpoint.name, noLabel), 1))
                endpointCount[(endpoint.name, noLabel)] += 1;
        }
    }

    public void HandleLinesRemoved (string uriOrName, in LineRange range)
    {
        var name = ToScriptName(uriOrName);
        for (int i = range.Start; i <= range.End; i++)
            if (labels.ContainsKey((name, i))) HandleLabelRemoved(i);
            else if (endpoints.ContainsKey((name, i))) HandleEndpointRemoved(i);

        void HandleLabelRemoved (int line)
        {
            var label = labels[(name, line)];
            if ((labelCount[(name, label)] -= 1) > 0) return;
            labelCount.Remove((name, label));
            labels.Remove((name, line));
        }

        void HandleEndpointRemoved (int line)
        {
            var endpoint = endpoints[(name, line)];
            if ((endpointCount[endpoint] -= 1) > 0) return;
            endpointCount.Remove(endpoint);
            endpoints.Remove((name, line));
            if ((endpointCount[(endpoint.name, noLabel)] -= 1) == 0)
                endpointCount.Remove((endpoint.name, noLabel));
        }
    }
}
