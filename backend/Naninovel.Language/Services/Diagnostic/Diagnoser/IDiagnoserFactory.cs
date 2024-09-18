namespace Naninovel.Language;

public interface IDiagnoserFactory
{
    IDiagnoser Create (DiagnosticContext context);
}
