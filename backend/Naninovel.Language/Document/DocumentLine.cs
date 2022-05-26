using Naninovel.Parsing;

namespace Naninovel.Language;

public record DocumentLine(string Text, IScriptLine Script, ParseError[] Errors);
