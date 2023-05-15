using System;
using System.Collections.Generic;
using Naninovel.Metadata;
using Naninovel.Parsing;
using Naninovel.Utilities;

namespace Naninovel.Language;

public class Diagnoser : IDiagnoser, IMetadataObserver
{
    private readonly MetadataProvider metaProvider = new();
    private readonly List<Diagnostic> diagnostics = new();
    private readonly ValueValidator validator = new();
    private readonly EndpointResolver endpoint;
    private readonly IDocumentRegistry docs;
    private readonly IDiagnosticPublisher publisher;

    private string documentUri = null!;
    private int lineIndex;
    private DocumentLine line;

    public Diagnoser (IDocumentRegistry docs, IDiagnosticPublisher publisher)
    {
        this.publisher = publisher;
        this.docs = docs;
        endpoint = new(metaProvider);
    }

    public void HandleMetadataChanged (Project meta)
    {
        metaProvider.Update(meta);
        foreach (var uri in docs.GetAllUris())
            Diagnose(uri);
    }

    public void Diagnose (string documentUri, LineRange? range = null)
    {
        ResetState(documentUri);
        var document = docs.Get(documentUri);
        Diagnose(document, range ?? new(0, document.LineCount - 1));
    }

    private void Diagnose (IDocument document, in LineRange range)
    {
        for (lineIndex = range.Start; lineIndex <= range.End; lineIndex++)
            DiagnoseLine(document[lineIndex]);
        Publish(documentUri);
    }

    private void ResetState (string documentUri)
    {
        diagnostics.Clear();
        this.documentUri = documentUri;
    }

    private void Publish (string documentUri)
    {
        if (diagnostics.Count == 0)
            publisher.PublishDiagnostics(documentUri, Array.Empty<Diagnostic>());
        else publisher.PublishDiagnostics(documentUri, diagnostics.ToArray());
    }

    private void DiagnoseLine (in DocumentLine line)
    {
        this.line = line;
        foreach (var error in line.Errors)
            AddParseError(error);
        if (line.Script is LabelLine labelLine)
            DiagnoseLabelLine(labelLine);
        if (line.Script is CommandLine commandLine)
            DiagnoseCommand(commandLine.Command);
        else if (line.Script is GenericLine genericLine)
            DiagnoseGenericLine(genericLine);
    }

    private void AddParseError (ParseError error)
    {
        var range = new Range(
            new(lineIndex, error.StartIndex),
            new(lineIndex, error.EndIndex + 1));
        diagnostics.Add(new(range, DiagnosticSeverity.Error, error.Message));
    }

    private void DiagnoseLabelLine (LabelLine labelLine)
    {
        if (!docs.IsUsed(documentUri, labelLine.Label))
            AddUnusedLabel(labelLine.Label);
    }

    private void DiagnoseGenericLine (GenericLine genericLine)
    {
        foreach (var content in genericLine.Content)
            if (content is InlinedCommand inlined)
                DiagnoseCommand(inlined.Command);
    }

    private void DiagnoseCommand (Parsing.Command command)
    {
        if (string.IsNullOrEmpty(command.Identifier)) return;
        var commandMeta = metaProvider.FindCommand(command.Identifier);
        if (commandMeta is null) AddUnknownCommand(command);
        else DiagnoseCommand(command, commandMeta);
    }

    private void DiagnoseCommand (Parsing.Command command, Metadata.Command commandMeta)
    {
        foreach (var paramMeta in commandMeta.Parameters)
            if (paramMeta.Required && !IsParameterDefined(paramMeta, command))
                AddMissingRequiredParameter(command, paramMeta);
        foreach (var param in command.Parameters)
            DiagnoseParameter(param, commandMeta);
    }

    private void DiagnoseParameter (Parsing.Parameter param, Metadata.Command commandMeta)
    {
        if (endpoint.TryResolve(param, commandMeta.Id, out var name, out var label) && IsEndpointUnknown(name, label))
            AddUnknownEndpoint(param);
        var paramMeta = metaProvider.FindParameter(commandMeta.Id, param.Identifier);
        if (paramMeta is null) AddUnknownParameter(param, commandMeta);
        else if (param.Value.Count == 0 || param.Value.Dynamic || param.Value[0] is not PlainText value) return;
        else if (!validator.Validate(value, paramMeta.ValueContainerType, paramMeta.ValueType)) AddInvalidValue(value, paramMeta);
    }

    private bool IsEndpointUnknown (string? name, string? label)
    {
        var uri = string.IsNullOrEmpty(name) ? documentUri : ResolveUriByScriptName(name);
        return uri is null || !docs.Contains(uri, label);
    }

    private string? ResolveUriByScriptName (string name)
    {
        var nameWithExtensions = name + ".nani";
        foreach (var uri in docs.GetAllUris())
            if (uri.EndsWithOrdinal(nameWithExtensions))
                return uri;
        return null;
    }

    private void AddUnknownCommand (Parsing.Command command)
    {
        var range = line.GetRange(command, lineIndex);
        var message = $"Command '{command.Identifier}' is unknown.";
        diagnostics.Add(new(range, DiagnosticSeverity.Error, message));
    }

    private void AddUnknownEndpoint (Parsing.Parameter param)
    {
        var range = line.GetRange(param.Value, lineIndex);
        var message = $"Unknown endpoint: {param.Value}.";
        diagnostics.Add(new(range, DiagnosticSeverity.Warning, message));
    }

    private void AddUnusedLabel (PlainText label)
    {
        var range = line.GetRange(label, lineIndex);
        diagnostics.Add(new(range, DiagnosticSeverity.Warning, "Unused label."));
    }

    private void AddUnknownParameter (Parsing.Parameter param, Metadata.Command commandMeta)
    {
        var range = line.GetRange(param, lineIndex);
        var message = param.Nameless
            ? $"Command '{commandMeta.Label}' doesn't have a nameless parameter."
            : $"Command '{commandMeta.Label}' doesn't have '{param.Identifier}' parameter.";
        diagnostics.Add(new(range, DiagnosticSeverity.Error, message));
    }

    private void AddMissingRequiredParameter (Parsing.Command command, Metadata.Parameter missingParam)
    {
        var range = line.GetRange(command, lineIndex);
        var message = $"Required parameter '{missingParam.Label}' is missing.";
        diagnostics.Add(new(range, DiagnosticSeverity.Error, message));
    }

    private void AddInvalidValue (PlainText value, Metadata.Parameter paramMeta)
    {
        var range = line.GetRange(value, lineIndex);
        var message = $"Invalid value: '{value}' is not a {paramMeta.TypeLabel}.";
        diagnostics.Add(new(range, DiagnosticSeverity.Error, message));
    }

    private static bool IsParameterDefined (Metadata.Parameter paramMeta, Parsing.Command command)
    {
        foreach (var param in command.Parameters)
            if (param.Nameless && paramMeta.Nameless) return true;
            else if (string.Equals(param.Identifier, paramMeta.Id, StringComparison.OrdinalIgnoreCase)) return true;
            else if (string.Equals(param.Identifier, paramMeta.Alias, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
