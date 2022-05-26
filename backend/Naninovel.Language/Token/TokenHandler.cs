using System;
using Naninovel.Parsing;

namespace Naninovel.Language;

// https://microsoft.github.io/language-server-protocol/specifications/specification-3-17/#textDocument_semanticTokens

public class TokenHandler
{
    private readonly DocumentRegistry registry;
    private readonly TokenBuilder builder = new();

    private Range range = null!;
    private int lineIndex;

    public TokenHandler (DocumentRegistry registry)
    {
        this.registry = registry;
    }

    public TokenLegend GetTokenLegend () => new() {
        TokenTypes = Enum.GetNames<TokenType>()
    };

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

    private Tokens CreateTokens (Document document, Range range)
    {
        ResetState(range);
        for (int i = lineIndex; i <= range.End.Line; i++)
            AppendLine(document.Lines[i].Script);
        return new Tokens(builder.Build());
    }

    private Range GetFullRange (Document document)
    {
        var endLine = document.Lines.Count - 1;
        var endChar = document.Lines[^1].Text.Length;
        return new Range(new(0, 0), new(endLine, endChar));
    }

    private void ResetState (Range range)
    {
        builder.Clear();
        this.range = range;
        lineIndex = range.Start.Line;
    }

    private void AppendLine (IScriptLine line)
    {
        if (line is CommentLine comment) AppendCommentLine(comment);
        else if (line is LabelLine label) AppendLabelLine(label);
        else if (line is CommandLine command) AppendCommandLine(command);
        else if (line is GenericTextLine generic) AppendGenericLine(generic);
        lineIndex++;
    }

    private void AppendCommentLine (CommentLine line)
    {
        AppendContent(line, TokenType.CommentLine);
        AppendContent(line.CommentText, TokenType.CommentText);
    }

    private void AppendLabelLine (LabelLine line)
    {
        AppendContent(line, TokenType.LabelLine);
        AppendContent(line.LabelText, TokenType.LabelText);
    }

    private void AppendCommandLine (CommandLine line)
    {
        AppendContent(line, TokenType.CommandLine);
        AppendCommand(line.Command);
    }

    private void AppendGenericLine (GenericTextLine line)
    {
        AppendContent(line, TokenType.GenericTextLine);
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
        AppendParameterValue(parameter.Value);
    }

    private void AppendParameterValue (ParameterValue value)
    {
        AppendContent(value, TokenType.ParameterValue);
        foreach (var expression in value.Expressions)
            AppendExpression(expression);
    }

    private void AppendExpression (LineText expression)
    {
        AppendContent(expression, TokenType.Expression);
    }

    private void AppendGenericPrefix (GenericTextPrefix prefix)
    {
        AppendContent(prefix, TokenType.GenericTextPrefix);
        AppendContent(prefix.AuthorIdentifier, TokenType.GenericTextAuthor);
        AppendContent(prefix.AuthorAppearance, TokenType.GenericTextAuthorAppearance);
    }

    private void AppendGenericContent (IGenericContent content)
    {
        if (content is InlinedCommand inlined) AppendInlined(inlined);
        else if (content is GenericText text) AppendGenericText(text);
    }

    private void AppendInlined (InlinedCommand inlined)
    {
        AppendContent(inlined, TokenType.InlinedCommand);
        AppendCommand(inlined.Command);
    }

    private void AppendGenericText (GenericText text)
    {
        foreach (var expression in text.Expressions)
            AppendExpression(expression);
    }

    private void AppendContent (LineContent content, TokenType type)
    {
        if (content.Length <= 0 || !IsInRange(content)) return;
        builder.Append(lineIndex, content.StartIndex, content.Length, type);
    }

    private bool IsInRange (LineContent content)
    {
        if (lineIndex == range.Start.Line)
            return content.StartIndex >= range.Start.Character;
        if (lineIndex == range.End.Line)
            return content.StartIndex < range.End.Character;
        return true;
    }
}
