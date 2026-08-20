using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using BancoAlimentos.Avalonia.Data;
using BancoAlimentos.Avalonia.Models;
using Microsoft.Data.SqlClient;

namespace BancoAlimentos.Avalonia.Services;

public class InventarioService
{
    public async Task<List<InventarioItem>> ObtenerInventarioAsync()
    {
        var lista = new List<InventarioItem>();
        using var conn = ConexionBD.ObtenerConexion();
        using var cmd = new SqlCommand("SELECT * FROM dbo.vw_Inventario ORDER BY FechaVencimiento;", conn);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(LeerFila(reader));
        }
        return lista;
    }

    /// <summary>
    /// Alertas de stock crítico (productos "Por vencer" o "Vencido"), vía sp_ObtenerAlertasStockCritico.
    /// </summary>
    public async Task<List<InventarioItem>> ObtenerAlertasAsync()
    {
        var lista = new List<InventarioItem>();
        using var conn = ConexionBD.ObtenerConexion();
        using var cmd = new SqlCommand("dbo.sp_ObtenerAlertasStockCritico", conn)
        {
            CommandType = CommandType.StoredProcedure
        };

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(LeerFila(reader));
        }
        return lista;
    }

    private static InventarioItem LeerFila(SqlDataReader reader)
    {
        return new InventarioItem
        {
            IdDetalleDonacion = reader.GetInt32(reader.GetOrdinal("IdDetalleDonacion")),
            IdProducto = reader.GetInt32(reader.GetOrdinal("IdProducto")),
            Producto = reader.GetString(reader.GetOrdinal("Producto")),
            Categoria = reader.GetString(reader.GetOrdinal("Categoria")),
            Unidad = reader.GetString(reader.GetOrdinal("Unidad")),
            CantidadDisponible = reader.GetDecimal(reader.GetOrdinal("CantidadDisponible")),
            FechaVencimiento = reader.GetDateTime(reader.GetOrdinal("FechaVencimiento")),
            FechaRecepcion = reader.GetDateTime(reader.GetOrdinal("FechaRecepcion")),
            Donante = reader.GetString(reader.GetOrdinal("Donante")),
            Estado = reader.GetString(reader.GetOrdinal("Estado")),
        };
    }
}
