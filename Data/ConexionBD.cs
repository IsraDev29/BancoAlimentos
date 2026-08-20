using Microsoft.Data.SqlClient;

namespace BancoAlimentos.Avalonia.Data;

/// <summary>
/// Crea conexiones a SQL Server. Se usa "using" en cada servicio para abrir/cerrar
/// la conexión por operación (patrón recomendado con ADO.NET / pooling de conexiones).
/// </summary>
public static class ConexionBD
{
    public static SqlConnection ObtenerConexion()
    {
        return new SqlConnection(DatabaseConfig.ConnectionString);
    }
}
