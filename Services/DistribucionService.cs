using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using BancoAlimentos.Avalonia.Data;
using BancoAlimentos.Avalonia.Models;
using Microsoft.Data.SqlClient;

namespace BancoAlimentos.Avalonia.Services;

public class DistribucionService
{
    /// <summary>
    /// Registra la entrega de uno o más lotes de inventario a un beneficiario,
    /// llamando a dbo.sp_RegistrarDistribucion. El trigger de la BD descuenta
    /// automáticamente la CantidadDisponible de cada lote.
    /// </summary>
    public async Task<int> RegistrarDistribucionAsync(
        int idBeneficiario, int idUsuarioResponsable, string? observaciones,
        List<InventarioItem> lotesAEntregar)
    {
        var detalle = new List<object>();
        foreach (var lote in lotesAEntregar)
        {
            if (lote.CantidadAEntregar <= 0)
                continue;

            if (lote.CantidadAEntregar > lote.CantidadDisponible)
                throw new ArgumentException(
                    $"La cantidad a entregar de '{lote.Producto}' ({lote.CantidadAEntregar}) excede el disponible ({lote.CantidadDisponible}).");

            detalle.Add(new
            {
                IdDetalleDonacion = lote.IdDetalleDonacion,
                CantidadEntregada = lote.CantidadAEntregar
            });
        }

        if (detalle.Count == 0)
            throw new ArgumentException("Debe indicar la cantidad a entregar de al menos un lote.");

        var json = JsonSerializer.Serialize(detalle);

        using var conn = ConexionBD.ObtenerConexion();
        using var cmd = new SqlCommand("dbo.sp_RegistrarDistribucion", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.Add("@IdBeneficiario", SqlDbType.Int).Value = idBeneficiario;
        cmd.Parameters.Add("@IdUsuarioResponsable", SqlDbType.Int).Value = idUsuarioResponsable;
        cmd.Parameters.Add("@Observaciones", SqlDbType.VarChar, 300).Value = (object?)observaciones ?? DBNull.Value;
        cmd.Parameters.Add("@DetalleJSON", SqlDbType.NVarChar, -1).Value = json;

        await conn.OpenAsync();
        try
        {
            var resultado = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(resultado);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            // 547 = violación de CHECK/FK. En la práctica se dispara cuando dos entregas
            // concurrentes dejan CantidadDisponible negativa; el mensaje nativo de
            // SQL Server viene en inglés y menciona el nombre interno de la restricción.
            throw new InvalidOperationException(
                "La entrega no se pudo registrar porque la cantidad solicitada ya no está disponible. " +
                "Actualice el inventario e inténtelo de nuevo.", ex);
        }
    }
}
