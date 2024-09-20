using Moq;

namespace Naninovel.Language.Test;

public class FormattingTest
{
    private readonly MetadataMock meta = new();
    private readonly Mock<IDocumentRegistry> docs = new();
    private readonly FormattingHandler handler;

    public FormattingTest ()
    {
        handler = new(meta, docs.Object);
    }

    [Fact]
    public void CanFormat ()
    {
        docs.SetupScript(meta, "script.nani",
            " @command  v   p:v ",
            " #comment ",
            " generic ",
            " author.app: generic  [cmd   v   p:v  ] "
        );
        var edit = handler.Format("script.nani");
        Assert.Single(edit);
        Assert.Equal(new(new(0, 0), new(3, 40)), edit[0].Range);
        Assert.Equal(
            """
            @command v p:v
            # comment
            generic
            author.app: generic  [cmd v p:v]

            """, edit[0].NewText);
    }
}
