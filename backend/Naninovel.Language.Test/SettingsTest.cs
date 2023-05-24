using Moq;
using Naninovel.TestUtilities;
using Xunit;

namespace Naninovel.Language.Test;

public class SettingsTest
{
    private readonly NotifierMock<ISettingsObserver> notifier = new();
    private readonly SettingsHandler handler;

    public SettingsTest ()
    {
        handler = new(notifier);
    }

    [Fact]
    public void NotifiesOnConfigure ()
    {
        var settings = new Settings();
        handler.Configure(settings);
        notifier.Verify(n => n.HandleSettingsChanged(settings), Times.Once);
        notifier.VerifyNoOtherCalls();
    }
}
