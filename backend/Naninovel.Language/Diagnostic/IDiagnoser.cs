namespace Naninovel.Language;

public interface IDiagnoser
{
    void Diagnose (string documentUri);
    void Diagnose (string documentUri, in Range range);
}
