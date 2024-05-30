using Naninovel.Expression;
using Naninovel.Parsing;

namespace Naninovel.Language;

public readonly record struct DocumentLine
{
    public string Text { get; }
    public IScriptLine Script { get; }
    public IReadOnlyList<ParseError> Errors { get; }
    public InlineRange Range { get; }

    private readonly RangeMapper mapper;

    public DocumentLine (string text, IScriptLine script, ParseError[] errors, RangeMapper mapper)
    {
        Text = text;
        Script = script;
        Errors = errors;
        this.mapper = mapper;
        Range = new InlineRange(0, Text.Length);
    }

    public bool TryResolve (ILineComponent component, out InlineRange range)
    {
        return mapper.TryResolve(component, out range);
    }

    public bool TryResolve (PlainText expressionBody, ExpressionRange expressionRange, out InlineRange range)
    {
        var resolved = TryResolve(expressionBody, out var expRange);
        range = new InlineRange(expRange.Start + expressionRange.Index, expressionRange.Length);
        return resolved;
    }

    public string Extract (in InlineRange range)
    {
        if (range.Length <= 0 || (range.Start + range.Length) > Text.Length) return "";
        return Text.Substring(range.Start, range.Length);
    }

    public string Extract (ILineComponent? content)
    {
        if (content is null || !mapper.TryResolve(content, out var range)) return "";
        return Extract(range);
    }

    public bool IsCursorOver (ILineComponent? content, in Position cursor)
    {
        if (content is null || !mapper.TryResolve(content, out var range)) return false;
        return IsCursorOver(range, cursor);
    }

    public bool IsCursorOver (in InlineRange range, in Position cursor)
    {
        return cursor.Character >= range.Start &&
               cursor.Character <= range.End + 1;
    }

    public char GetCharBehindCursor (in Position cursor)
    {
        if (cursor.Character <= 0 || cursor.Character > Text.Length) return default;
        return Text[cursor.Character - 1];
    }

    public InlineRange GetLineRange (ILineComponent? content)
    {
        if (content is null || !mapper.TryResolve(content, out var range))
            return new InlineRange(0, 0);
        return range;
    }

    public Range GetRange (int lineIndex)
    {
        var start = new Position(lineIndex, Range.Start);
        var end = new Position(lineIndex, Range.End + 1);
        return new Range(start, end);
    }

    public Range GetRange (ILineComponent? content, int lineIndex)
    {
        if (content is null || !mapper.TryResolve(content, out var inline))
            return Language.Range.Empty;
        return GetRange(inline, lineIndex);
    }

    public Range GetRange (InlineRange inline, int lineIndex)
    {
        var start = new Position(lineIndex, inline.Start);
        var end = new Position(lineIndex, inline.End + 1);
        return new Range(start, end);
    }
}
