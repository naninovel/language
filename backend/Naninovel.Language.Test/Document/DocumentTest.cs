using Naninovel.Parsing;
using static Naninovel.Language.Test.Common;

namespace Naninovel.Language.Test;

public class DocumentTest
{
    [Fact]
    public void CanResolveRange ()
    {
        Assert.Equal(Range.Empty, new Document([]).GetRange());
        Assert.Equal(new(new(0, 0), new(1, 8)), CreateDocument("# label", "@command").GetRange());
    }

    [Fact]
    public void CanEnumerateScript ()
    {
        foreach (var line in CreateDocument("# label").EnumerateScript())
            Assert.True(line is LabelLine);
    }
}
