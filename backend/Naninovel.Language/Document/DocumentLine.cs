using Naninovel.Parsing;

namespace Naninovel.Language;

public class DocumentLine
{
    public string Text { get; }
    public IScriptLine Script { get; }
    public ParseError[] Errors { get; }
    public RangeMapper Mapper { get; }
    public LineRange Range { get; }

    public DocumentLine (string text, IScriptLine script, ParseError[] errors, RangeMapper mapper)
    {
        Text = text;
        Script = script;
        Errors = errors;
        Mapper = mapper;
        Range = new LineRange(0, Text.Length);
    }

    public string Extract (LineRange range)
    {
        if (range.StartIndex < 0 || range.Length <= 0 ||
            range.StartIndex + range.Length > Text.Length) return "";
        return Text.Substring(range.StartIndex, range.Length);
    }

    public string Extract (ILineComponent? content)
    {
        if (content is null || !Mapper.TryResolve(content, out var range)) return "";
        return Extract(range);
    }

    public bool IsCursorOver (ILineComponent? content, Position cursor)
    {
        if (content is null || !Mapper.TryResolve(content, out var range)) return false;
        return cursor.Character >= range.StartIndex &&
               cursor.Character <= range.EndIndex + 1;
    }

    public char GetCharBehindCursor (Position cursor)
    {
        if (cursor.Character <= 0 || cursor.Character > Text.Length) return default;
        return Text[cursor.Character - 1];
    }

    public LineRange GetLineRange (ILineComponent? content)
    {
        if (content is null || !Mapper.TryResolve(content, out var range))
            return new LineRange(0, 0);
        return range;
    }

    public Range GetRange (int lineIndex)
    {
        var start = new Position(lineIndex, Range.StartIndex);
        var end = new Position(lineIndex, Range.EndIndex + 1);
        return new Range(start, end);
    }

    public Range GetRange (ILineComponent? content, int lineIndex)
    {
        if (content is null || !Mapper.TryResolve(content, out var range))
            return Language.Range.Empty;
        var start = new Position(lineIndex, range.StartIndex);
        var end = new Position(lineIndex, range.EndIndex + 1);
        return new Range(start, end);
    }
}
