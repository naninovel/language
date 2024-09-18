namespace Naninovel.Language;

public interface ISettingsObserver
{
    void HandleSettingsChanged (Settings settings);
}
