using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

public class Diagnoser : IDiagnoser, IMetadataObserver
{
    private readonly MetadataProvider metaProvider = new();
    private readonly IDiagnosticPublisher publisher;
    private readonly EndpointDiagnoser endpoints;
    private readonly IDocumentRegistry docs;
    private readonly List<Diagnostic> diagnostics = new();

    private string documentUri = null!;
    private int lineIndex;
    private DocumentLine line;

    public Diagnoser (IDocumentRegistry docs, IDiagnosticPublisher publisher)
    {
        this.publisher = publisher;
        this.docs = docs;
        endpoints = new(metaProvider);
    }

    public void HandleMetadataChanged (Project meta)
    {
        metaProvider.Update(meta);
        foreach (var uri in docs.GetAllUris())
            Diagnose(uri);
    }

    public void Diagnose (string documentUri)
    {
        ResetState(documentUri);
        var document = docs.Get(documentUri);
        Diagnose(document, new(new(0, 0), new(document.LineCount - 1, 0)));
    }

    public void Diagnose (string documentUri, in Range range)
    {
        ResetState(documentUri);
        Diagnose(docs.Get(documentUri), range);
    }

    private void Diagnose (IDocument document, in Range range)
    {
        for (lineIndex = range.Start.Line; lineIndex <= range.End.Line; lineIndex++)
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
            DiagnoseLabel(labelLine.Label);
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

    private void DiagnoseLabel (PlainText label) { }

    private void DiagnoseCommand (Parsing.Command command)
    {
        if (string.IsNullOrEmpty(command.Identifier)) return;
        var commandMeta = metaProvider.FindCommand(command.Identifier);
        if (commandMeta is null) AddUnknownCommand(command);
        else DiagnoseCommand(command, commandMeta);
    }

    private void DiagnoseGenericLine (GenericLine genericLine)
    {
        foreach (var content in genericLine.Content)
            if (content is InlinedCommand inlined)
                DiagnoseCommand(inlined.Command);
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
        var paramMeta = metaProvider.FindParameter(commandMeta.Id, param.Identifier);
        if (paramMeta is null) AddUnknownParameter(param, commandMeta);
        else if (param.Value.Count == 0 || param.Value.Dynamic || param.Value[0] is not PlainText value) return;
        else if (!IsValueValid(value, paramMeta)) AddInvalidValue(value, paramMeta);
    }

    private void AddUnknownCommand (Parsing.Command command)
    {
        var range = line.GetRange(command, lineIndex);
        var message = $"Command '{command.Identifier}' is unknown.";
        diagnostics.Add(new(range, DiagnosticSeverity.Error, message));
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

    private static bool IsValueValid (PlainText value, Metadata.Parameter paramMeta)
    {
        if (paramMeta.ValueContainerType is ValueContainerType.List)
            return value.Text.Split(',', StringSplitOptions.RemoveEmptyEntries).All(IsValid);
        if (paramMeta.ValueContainerType is ValueContainerType.NamedList)
            return value.Text.Split(',', StringSplitOptions.RemoveEmptyEntries).All(v => IsValid(GetNamedValue(v)));
        if (paramMeta.ValueContainerType is ValueContainerType.Named)
            return IsValid(GetNamedValue(value));
        return IsValid(value);

        bool IsValid (string value) => paramMeta.ValueType switch {
            Metadata.ValueType.Integer => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            Metadata.ValueType.Decimal => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
            Metadata.ValueType.Boolean => bool.TryParse(value, out _),
            _ => true
        };
    }

    private static string GetNamedValue (string value)
    {
        var dotIdx = value.LastIndexOf('.');
        if (dotIdx < 0 || dotIdx + 1 >= value.Length) return "";
        return value[(dotIdx + 1)..];
    }
}
