using Naninovel.Metadata;
using Naninovel.Parsing;

namespace Naninovel.Language;

public class DocumentFactory
{
    private readonly ScriptParser parser;
    private readonly ErrorCollector errors = [];
    private readonly RangeMapper mapper = new();

    public DocumentFactory (MetadataProvider meta)
    {
        parser = new(new() {
            Handlers = new() { ErrorHandler = errors, RangeAssociator = mapper },
            Identifiers = meta.Preferences.Identifiers
        });
    }

    public Document CreateDocument (string scriptText)
    {
        var lines = new List<DocumentLine>();
        foreach (var lineText in ScriptParser.SplitText(scriptText))
            lines.Add(CreateLine(lineText));
        return new(lines);
    }

    public DocumentLine CreateLine (string lineText)
    {
        var lineModel = parser.ParseLine(lineText);
        var lineErrors = CollectErrors();
        var lineMapper = MapComponents();
        return new DocumentLine(lineText, lineModel, lineErrors, lineMapper);
    }

    private ParseError[] CollectErrors ()
    {
        if (errors.Count == 0) return [];
        var lineErrors = errors.ToArray();
        errors.Clear();
        return lineErrors;
    }

    private RangeMapper MapComponents ()
    {
        var lineMapper = new RangeMapper();
        foreach (var (component, range) in mapper)
            lineMapper.Associate(component, range);
        mapper.Clear();
        return lineMapper;
    }
}
