namespace Naninovel.Language;

public class Configurator (IObserverNotifier<ISettingsObserver> notifier) : IConfigurator
{
    public void Configure (Settings settings)
    {
        notifier.Notify(n => n.HandleSettingsChanged(settings));
    }
}
