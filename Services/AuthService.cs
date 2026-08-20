using System;
using System.Data;
using System.Threading.Tasks;
using BancoAlimentos.Avalonia.Common;
using BancoAlimentos.Avalonia.Data;
using BancoAlimentos.Avalonia.Models;
using Microsoft.Data.SqlClient;

namespace BancoAlimentos.Avalonia.Services;

public class AuthService
{
    /// <summary>
    /// Valida credenciales verificando el hash PBKDF2 en la aplicación.
    /// Devuelve null si el usuario no existe, está inactivo o la clave no coincide
    /// (sin distinguir el motivo, para no revelar qué usuarios existen).
    /// </summary>
    public async Task<Usuario?> ValidarLoginAsync(string nombreUsuario, string contrasena)
    {
        using var conn = ConexionBD.ObtenerConexion();
        using var cmd = new SqlCommand("dbo.sp_ObtenerCredencial", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.Add("@NombreUsuario", SqlDbType.VarChar, 50).Value = nombreUsuario;

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        if (!reader.GetBoolean(reader.GetOrdinal("Activo")))
            return null;

        var hashGuardado = reader.GetString(reader.GetOrdinal("Contrasena"));
        if (!PasswordHasher.Verificar(contrasena, hashGuardado))
            return null;

        return new Usuario
        {
            IdUsuario = reader.GetInt32(reader.GetOrdinal("IdUsuario")),
            NombreCompleto = reader.GetString(reader.GetOrdinal("NombreCompleto")),
            NombreUsuario = reader.GetString(reader.GetOrdinal("NombreUsuario")),
            NombreRol = reader.GetString(reader.GetOrdinal("NombreRol")),
        };
    }

    /// <summary>
    /// Registra un usuario nuevo con rol Operador. El hash se calcula aquí:
    /// la contraseña en claro nunca se envía a SQL Server.
    /// </summary>
    public async Task<Usuario> RegistrarUsuarioAsync(
        string nombreCompleto, string nombreUsuario, string contrasena, string nombreRol = "Operador")
    {
        using var conn = ConexionBD.ObtenerConexion();
        using var cmd = new SqlCommand("dbo.sp_RegistrarUsuario", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.Add("@NombreCompleto", SqlDbType.VarChar, 100).Value = nombreCompleto;
        cmd.Parameters.Add("@NombreUsuario", SqlDbType.VarChar, 50).Value = nombreUsuario;
        cmd.Parameters.Add("@HashContrasena", SqlDbType.VarChar, 256).Value = PasswordHasher.Hash(contrasena);
        cmd.Parameters.Add("@NombreRol", SqlDbType.VarChar, 30).Value = nombreRol;

        await conn.OpenAsync();
        try
        {
            var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return new Usuario
            {
                IdUsuario = id,
                NombreCompleto = nombreCompleto,
                NombreUsuario = nombreUsuario,
                NombreRol = nombreRol
            };
        }
        catch (SqlException ex) when (ex.Number is 50010 or 50011 or 2601 or 2627)
        {
            // 50010/50011 los lanza el procedimiento; 2601/2627 son la violación
            // del índice único de NombreUsuario si dos altas coinciden a la vez.
            throw new InvalidOperationException(
                ex.Number == 50011
                    ? "El rol indicado no existe."
                    : "Ese nombre de usuario ya está registrado. Elija otro.", ex);
        }
    }
}
