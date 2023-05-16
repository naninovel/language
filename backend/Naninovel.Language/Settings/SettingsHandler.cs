namespace Naninovel.Language;

public class SettingsHandler : ISettingsHandler
{
    private readonly IObserverNotifier<ISettingsObserver> notifier;

    public SettingsHandler (IObserverNotifier<ISettingsObserver> notifier)
    {
        this.notifier = notifier;
    }

    public void Configure (Settings settings)
    {
        notifier.Notify(n => n.HandleSettingsChanged(settings));
    }
}
