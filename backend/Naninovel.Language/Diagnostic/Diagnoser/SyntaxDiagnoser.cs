﻿using Naninovel.Parsing;

namespace Naninovel.Language;

internal class SyntaxDiagnoser : Diagnoser
{
    public override DiagnosticContext Context => DiagnosticContext.Syntax;

    public SyntaxDiagnoser (IDocumentRegistry docs, DiagnosticRegistry registry)
        : base(docs, registry) { }

    public override void HandleDocumentAdded (string uri)
    {
        Diagnose(uri);
    }

    public override void HandleDocumentRemoved (string uri)
    {
        Registry.Remove(uri, i => i.Context == Context);
    }

    public override void HandleDocumentChanged (string uri, in LineRange range)
    {
        Diagnose(uri, range);
    }

    protected override void DiagnoseLine (in DocumentLine line)
    {
        foreach (var error in line.Errors)
            AddError(error);
    }

    private void AddError (ParseError error)
    {
        var range = new Range(
            new(LineIndex, error.StartIndex),
            new(LineIndex, error.EndIndex + 1));
        AddError(range, error.Message);
    }
}