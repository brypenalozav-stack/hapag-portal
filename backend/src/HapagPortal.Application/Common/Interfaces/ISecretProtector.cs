namespace HapagPortal.Application.Common.Interfaces;

/// <summary>
/// Cifra/descifra secretos en reposo. La clave maestra vive fuera del repo
/// (variable de entorno). El valor en claro nunca se persiste.
/// </summary>
public interface ISecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedValue);
}
