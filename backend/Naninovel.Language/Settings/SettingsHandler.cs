namespace Naninovel.Language;

public class SettingsHandler(IObserverNotifier<ISettingsObserver> notifier) : ISettingsHandler
{
    public void Configure (Settings settings)
    {
        notifier.Notify(n => n.HandleSettingsChanged(settings));
    }
}
