using System;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace BancoAlimentos.Avalonia.Data;

/// <summary>
/// Traduce errores de SQL Server a mensajes accionables.
///
/// El caso importante es el 2812 («Could not find stored procedure»): significa
/// que la base de datos está en una versión anterior a la del código, y el
/// mensaje nativo no dice qué hacer. Aquí se indica exactamente qué script de
/// la carpeta Database falta aplicar.
/// </summary>
public static class ErroresSql
{
    /// <summary>Error 2812 de SQL Server: procedimiento almacenado inexistente.</summary>
    private const int ProcedimientoNoEncontrado = 2812;

    /// <summary>Qué script crea cada familia de procedimientos.</summary>
    private static string ScriptQueLoCrea(string procedimiento) => procedimiento switch
    {
        var p when p.Contains("Credencial") || p.Contains("RegistrarUsuario")
            => "Database/02-fase1-usuarios.sql",
        var p when p.Contains("Reporte")
            => "Database/04-fase3-reportes.sql",
        var p when p.Contains("Guardar") || p.Contains("Eliminar") ||
                   p.Contains("ObtenerProductos") || p.Contains("ObtenerDonantes") ||
                   p.Contains("ObtenerBeneficiarios") || p.Contains("ObtenerUnidades") ||
                   p.Contains("ObtenerCategorias") || p.Contains("ObtenerTiposDonante")
            => "Database/03-fase2-crud.sql",
        _ => "los scripts de la carpeta Database"
    };

    /// <summary>
    /// Devuelve una excepción con mensaje entendible, o null si el error no es
    /// de los que sabemos traducir.
    /// </summary>
    public static InvalidOperationException? Traducir(SqlException ex)
    {
        if (ex.Number != ProcedimientoNoEncontrado)
            return null;

        // El mensaje nativo trae el nombre entre comillas simples.
        var nombre = Regex.Match(ex.Message, @"'([^']+)'").Groups[1].Value;
        var corto = nombre.Contains('.') ? nombre[(nombre.LastIndexOf('.') + 1)..] : nombre;

        return new InvalidOperationException(
            $"La base de datos está desactualizada: falta el procedimiento «{corto}». " +
            $"Aplique {ScriptQueLoCrea(corto)} sobre la base BancoAlimentos y vuelva a intentarlo.",
            ex);
    }
}
