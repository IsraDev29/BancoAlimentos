using System;
using System.IO;
using System.Text.Json;

namespace BancoAlimentos.Avalonia.Data;

/// <summary>
/// Carga la cadena de conexión desde appsettings.json (ubicado junto al ejecutable).
/// </summary>
public static class DatabaseConfig
{
    private static string? _connectionString;

    public static string ConnectionString
    {
        get
        {
            if (_connectionString is not null)
                return _connectionString;

            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

            if (!File.Exists(path))
                throw new FileNotFoundException(
                    $"No se encontró appsettings.json en '{path}'. Verifique que el archivo se copie al directorio de salida.");

            using var stream = File.OpenRead(path);
            using var doc = JsonDocument.Parse(stream);

            _connectionString = doc.RootElement
                .GetProperty("ConnectionStrings")
                .GetProperty("BancoAlimentos")
                .GetString()
                ?? throw new InvalidOperationException("La cadena de conexión 'BancoAlimentos' no está configurada.");

            return _connectionString;
        }
    }
}
