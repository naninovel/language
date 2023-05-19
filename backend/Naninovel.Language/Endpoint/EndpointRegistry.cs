using System;
using System.Collections.Generic;
using Naninovel.Metadata;
using Naninovel.Parsing;
using static Naninovel.Language.Common;

namespace Naninovel.Language;

public class EndpointRegistry : IEndpointRegistry, IDocumentObserver, IMetadataObserver
{
    private const string? noLabel = null;
    private readonly Dictionary<(string name, int line), string> labels = new();
    private readonly Dictionary<(string name, int line), (string name, string? label)> endpoints = new();
    private readonly Dictionary<(string name, string? label), int> labelCount = new();
    private readonly Dictionary<(string name, string? label), int> endpointCount = new();
    private readonly MetadataProvider metaProvider = new();
    private readonly IDocumentRegistry docs;
    private readonly EndpointResolver resolver;

    public EndpointRegistry (IDocumentRegistry docs)
    {
        this.docs = docs;
        resolver = new(metaProvider);
    }

    public void HandleMetadataChanged (Project meta) => metaProvider.Update(meta);

    public void HandleDocumentAdded (string uri)
    {
        var name = ToScriptName(uri);
        var doc = docs.Get(uri);
        labelCount.TryAdd((name, noLabel), 0);
        HandleLinesAdded(name, doc, new(0, doc.LineCount - 1));
    }

    public void HandleDocumentRemoved (string uri)
    {
        var name = ToScriptName(uri);
        labelCount.Remove((name, noLabel));
        HandleLinesRemoved(name, new(0, docs.Get(uri).LineCount - 1));
    }

    public void HandleDocumentChanged (string uri, LineRange range)
    {
        var name = ToScriptName(uri);
        var doc = docs.Get(uri);
        HandleLinesRemoved(name, range);
        HandleLinesAdded(name, doc, new(range.Start, Math.Min(range.End, doc.LineCount - 1)));
    }

    public bool ScriptExist (string scriptName)
    {
        return labelCount.ContainsKey((scriptName, noLabel));
    }

    public bool LabelExist (string scriptName, string label)
    {
        return labelCount.ContainsKey((scriptName, label));
    }

    public bool ScriptUsed (string scriptName)
    {
        return endpointCount.ContainsKey((scriptName, noLabel));
    }

    public bool LabelUsed (string scriptName, string label)
    {
        return endpointCount.ContainsKey((scriptName, label));
    }

    private void HandleLinesAdded (string name, IDocument doc, in LineRange range)
    {
        for (var i = range.Start; i <= range.End; i++)
            if (doc[i].Script is LabelLine labelLine)
                HandleLabelAdded(i, labelLine.Label);
            else if (doc[i].Script is CommandLine commandLine)
                HandleCommandAdded(commandLine.Command, i);
            else if (doc[i].Script is GenericLine genericLine)
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

    private void HandleLinesRemoved (string name, in LineRange range)
    {
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
