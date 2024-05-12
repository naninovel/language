using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

public class TokenHandler (IMetadata meta, IDocumentRegistry registry) : ITokenHandler
{
    private readonly TokenBuilder builder = new();

    private DocumentLine line;
    private Range range;
    private int lineIndex;

    public TokenLegend GetTokenLegend () => new(
        TokenTypes: Enum.GetNames<TokenType>(),
        TokenModifiers: Array.Empty<string>()
    );

    public Tokens GetAllTokens (string documentUri)
    {
        documentUri = Uri.UnescapeDataString(documentUri);
        var document = registry.Get(documentUri);
        var range = GetFullRange(document);
        return CreateTokens(document, range);
    }

    public Tokens GetTokens (string documentUri, Range range)
    {
        documentUri = Uri.UnescapeDataString(documentUri);
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

    private void AppendCommand (Parsing.Command command)
    {
        var commandMeta = meta.FindCommand(command.Identifier);
        AppendContent(command, TokenType.Command);
        AppendContent(command.Identifier, TokenType.CommandIdentifier);
        foreach (var parameter in command.Parameters)
            AppendParameter(parameter, commandMeta);
    }

    private void AppendParameter (Parsing.Parameter param, Metadata.Command? commandMeta)
    {
        var paramMeta = commandMeta != null ? meta.FindParameter(commandMeta.Id, param.Identifier) : null;
        AppendContent(param, TokenType.Parameter);
        AppendContent(param.Identifier, TokenType.ParameterIdentifier);
        if (paramMeta?.ValueContext?.FirstOrDefault()?.Type == ValueContextType.Expression)
            AppendContent(param.Value, TokenType.Expression);
        else if (paramMeta?.Localizable ?? false)
            AppendContent(param.Value, TokenType.LocalizableValue);
        else AppendContent(param.Value, TokenType.ParameterValue);
        AppendMixedValue(param.Value);
    }

    private void AppendExpression (Parsing.Expression expression)
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
            if (component is Parsing.Expression expression)
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
