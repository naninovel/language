using System;
using Naninovel.Parsing;

namespace Naninovel.Language;

public class TokenHandler : ITokenHandler
{
    private readonly IDocumentRegistry registry;
    private readonly TokenBuilder builder = new();

    private DocumentLine line;
    private Range range;
    private int lineIndex;

    public TokenHandler (IDocumentRegistry registry)
    {
        this.registry = registry;
    }

    public TokenLegend GetTokenLegend () => new(
        TokenTypes: Enum.GetNames<TokenType>(),
        TokenModifiers: Array.Empty<string>()
    );

    public Tokens GetAllTokens (string documentUri)
    {
        var document = registry.Get(documentUri);
        var range = GetFullRange(document);
        return CreateTokens(document, range);
    }

    public Tokens GetTokens (string documentUri, Range range)
    {
        var document = registry.Get(documentUri);
        return CreateTokens(document, range);
    }

    private Tokens CreateTokens (IDocument document, in Range range)
    {
        ResetState(range);
        for (int i = lineIndex; i <= range.End.Line; i++)
            AppendLine(document[i]);
        return new Tokens(builder.Build());
    }

    private Range GetFullRange (IDocument document)
    {
        var endLine = document.LineCount - 1;
        var endChar = document[^1].Text.Length;
        return new Range(new(0, 0), new(endLine, endChar));
    }

    private void ResetState (in Range range)
    {
        builder.Clear();
        this.range = range;
        lineIndex = range.Start.Line;
    }

    private void AppendLine (in DocumentLine line)
    {
        this.line = line;
        if (line.Script is CommentLine comment) AppendCommentLine(comment);
        else if (line.Script is LabelLine label) AppendLabelLine(label);
        else if (line.Script is CommandLine command) AppendCommandLine(command);
        else if (line.Script is GenericLine generic) AppendGenericLine(generic);
        lineIndex++;
    }

    private void AppendCommentLine (CommentLine line)
    {
        AppendContent(this.line.Range, TokenType.CommentLine);
        AppendContent(line.Comment, TokenType.CommentText);
    }

    private void AppendLabelLine (LabelLine line)
    {
        AppendContent(this.line.Range, TokenType.LabelLine);
        AppendContent(line.Label, TokenType.LabelText);
    }

    private void AppendCommandLine (CommandLine line)
    {
        AppendContent(this.line.Range, TokenType.CommandLine);
        AppendCommand(line.Command);
    }

    private void AppendGenericLine (GenericLine line)
    {
        AppendContent(this.line.Range, TokenType.GenericTextLine);
        if (line.Prefix is not null)
            AppendGenericPrefix(line.Prefix);
        foreach (var content in line.Content)
            AppendGenericContent(content);
    }

    private void AppendCommand (Command command)
    {
        AppendContent(command, TokenType.Command);
        AppendContent(command.Identifier, TokenType.CommandIdentifier);
        foreach (var parameter in command.Parameters)
            AppendParameter(parameter);
    }

    private void AppendParameter (Parameter parameter)
    {
        AppendContent(parameter, TokenType.Parameter);
        AppendContent(parameter.Identifier, TokenType.ParameterIdentifier);
        AppendContent(parameter.Value, TokenType.ParameterValue);
        AppendMixedValue(parameter.Value);
    }

    private void AppendExpression (Expression expression)
    {
        AppendContent(expression, TokenType.Expression);
    }

    private void AppendTextIdentifier (TextIdentifier textIdentifier)
    {
        AppendContent(textIdentifier, TokenType.TextIdentifier);
    }

    private void AppendGenericPrefix (GenericPrefix prefix)
    {
        AppendContent(prefix, TokenType.GenericTextPrefix);
        AppendContent(prefix.Author, TokenType.GenericTextAuthor);
        if (prefix.Appearance is not null)
            AppendContent(prefix.Appearance, TokenType.GenericTextAuthorAppearance);
    }

    private void AppendGenericContent (IGenericContent content)
    {
        if (content is InlinedCommand inlined) AppendInlined(inlined);
        else if (content is MixedValue text) AppendMixedValue(text);
    }

    private void AppendInlined (InlinedCommand inlined)
    {
        AppendContent(inlined, TokenType.InlinedCommand);
        AppendCommand(inlined.Command);
    }

    private void AppendMixedValue (MixedValue mixed)
    {
        foreach (var component in mixed)
            if (component is Expression expression)
                AppendExpression(expression);
            else if (component is IdentifiedText id)
                AppendTextIdentifier(id.Id);
    }

    private void AppendContent (ILineComponent? content, TokenType type)
    {
        if (content is null || !line.TryResolve(content, out var lineRange) || !IsInRange(lineRange)) return;
        builder.Append(lineIndex, lineRange.Start, lineRange.Length, type);
    }

    private void AppendContent (in InlineRange inlineRange, TokenType type)
    {
        if (inlineRange.Length <= 0 || !IsInRange(inlineRange)) return;
        builder.Append(lineIndex, inlineRange.Start, inlineRange.Length, type);
    }

    private bool IsInRange (in InlineRange inlineRange)
    {
        if (lineIndex == range.Start.Line)
            return inlineRange.Start >= range.Start.Character;
        if (lineIndex == range.End.Line)
            return inlineRange.Start < range.End.Character;
        return true;
    }
}
