using System;
using System.Data;
using System.Threading.Tasks;
using BancoAlimentos.Avalonia.Data;
using BancoAlimentos.Avalonia.Models;
using Microsoft.Data.SqlClient;

namespace BancoAlimentos.Avalonia.Services;

public class AuthService
{
    /// <summary>
    /// Valida credenciales contra dbo.sp_ValidarLogin. Devuelve null si no son válidas.
    /// </summary>
    public async Task<Usuario?> ValidarLoginAsync(string nombreUsuario, string contrasena)
    {
        using var conn = ConexionBD.ObtenerConexion();
        using var cmd = new SqlCommand("dbo.sp_ValidarLogin", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        // Tipos explícitos: las columnas son VARCHAR y AddWithValue enviaría NVARCHAR,
        // lo que obliga a SQL Server a convertir e impide usar el índice de NombreUsuario.
        cmd.Parameters.Add("@NombreUsuario", SqlDbType.VarChar, 50).Value = nombreUsuario;
        cmd.Parameters.Add("@Contrasena", SqlDbType.VarChar, 256).Value = contrasena;

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return new Usuario
            {
                IdUsuario = reader.GetInt32(reader.GetOrdinal("IdUsuario")),
                NombreCompleto = reader.GetString(reader.GetOrdinal("NombreCompleto")),
                NombreUsuario = reader.GetString(reader.GetOrdinal("NombreUsuario")),
                NombreRol = reader.GetString(reader.GetOrdinal("NombreRol")),
            };
        }

        return null;
    }
}
