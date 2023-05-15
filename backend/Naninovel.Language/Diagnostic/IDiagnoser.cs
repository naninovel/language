namespace Naninovel.Language;

public interface IDiagnoser
{
    void Diagnose (string documentUri, LineRange? range = null);
}
