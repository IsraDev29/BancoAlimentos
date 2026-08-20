using System;
using System.Security.Cryptography;

namespace BancoAlimentos.Avalonia.Common;

/// <summary>
/// Hash de contraseñas con PBKDF2-HMAC-SHA256 y salt por usuario.
///
/// La verificación se hace en la aplicación, NO en SQL: el procedimiento
/// almacenado sólo devuelve el hash guardado. Así la contraseña en claro nunca
/// viaja a la base de datos ni queda en el caché de planes de ejecución.
///
/// Formato guardado en dbo.Usuario.Contrasena (cabe en VARCHAR(256)):
///     pbkdf2-sha256$&lt;iteraciones&gt;$&lt;saltBase64&gt;$&lt;hashBase64&gt;
/// </summary>
public static class PasswordHasher
{
    private const string Etiqueta = "pbkdf2-sha256";
    private const int Iteraciones = 120_000;
    private const int TamanoSalt = 16;   // 128 bits
    private const int TamanoHash = 32;   // 256 bits

    public static string Hash(string contrasena)
    {
        if (string.IsNullOrEmpty(contrasena))
            throw new ArgumentException("La contraseña no puede estar vacía.", nameof(contrasena));

        var salt = RandomNumberGenerator.GetBytes(TamanoSalt);
        var hash = Derivar(contrasena, salt, Iteraciones);

        return $"{Etiqueta}${Iteraciones}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// Comprueba la contraseña contra el hash guardado. Devuelve false ante
    /// cualquier formato inesperado en lugar de lanzar, para que un registro
    /// corrupto se traduzca en "credenciales inválidas" y no en un error.
    /// </summary>
    public static bool Verificar(string contrasena, string? hashGuardado)
    {
        if (string.IsNullOrWhiteSpace(contrasena) || string.IsNullOrWhiteSpace(hashGuardado))
            return false;

        var partes = hashGuardado.Split('$');
        if (partes.Length != 4 || partes[0] != Etiqueta)
            return false;

        if (!int.TryParse(partes[1], out var iteraciones) || iteraciones <= 0)
            return false;

        byte[] salt, esperado;
        try
        {
            salt = Convert.FromBase64String(partes[2]);
            esperado = Convert.FromBase64String(partes[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var calculado = Derivar(contrasena, salt, iteraciones, esperado.Length);

        // Comparación en tiempo constante: evita filtrar información por el
        // tiempo que tarda en fallar.
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }

    /// <summary>True si el hash no está en el formato PBKDF2 (por ejemplo, texto plano heredado).</summary>
    public static bool NecesitaMigracion(string? hashGuardado) =>
        string.IsNullOrWhiteSpace(hashGuardado) || !hashGuardado.StartsWith(Etiqueta + "$", StringComparison.Ordinal);

    private static byte[] Derivar(string contrasena, byte[] salt, int iteraciones, int tamano = TamanoHash) =>
        Rfc2898DeriveBytes.Pbkdf2(contrasena, salt, iteraciones, HashAlgorithmName.SHA256, tamano);
}
