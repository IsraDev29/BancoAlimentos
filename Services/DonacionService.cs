using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using BancoAlimentos.Avalonia.Data;
using BancoAlimentos.Avalonia.Models;
using Microsoft.Data.SqlClient;

namespace BancoAlimentos.Avalonia.Services;

public class DonacionService
{
    /// <summary>
    /// Registra una donación con su detalle llamando a dbo.sp_RegistrarDonacion.
    /// Devuelve el IdDonacion generado.
    /// </summary>
    public async Task<int> RegistrarDonacionAsync(
        int idDonante, int idUsuarioRegistro, string? observaciones, List<DetalleDonacionInput> detalle)
    {
        if (detalle is null || detalle.Count == 0)
            throw new ArgumentException("Debe agregar al menos un producto a la donación.");

        var detalleJsonList = new List<object>();
        foreach (var d in detalle)
        {
            if (d.ProductoSeleccionado is null)
                throw new ArgumentException("Hay una línea de detalle sin producto seleccionado.");
            if (d.Cantidad <= 0)
                throw new ArgumentException("La cantidad debe ser mayor a cero.");

            if (d.EsEmpaquetado &&
                (d.CantidadEnvases is not > 0 || d.PesoPorEnvase is not > 0 || d.UnidadPeso is null))
                throw new ArgumentException(
                    $"'{d.ProductoNombre}' está marcado como empaquetado: indique cantidad de envases, " +
                    "peso por envase y su unidad.");

            detalleJsonList.Add(new
            {
                IdProducto = d.ProductoSeleccionado.IdProducto,
                Cantidad = d.Cantidad,
                FechaVencimiento = d.FechaVencimiento.ToString("yyyy-MM-dd"),
                EsEmpaquetado = d.EsEmpaquetado,
                CantidadEnvases = d.EsEmpaquetado ? d.CantidadEnvases : null,
                PesoPorEnvase = d.EsEmpaquetado ? d.PesoPorEnvase : null,
                IdUnidadPeso = d.EsEmpaquetado ? d.UnidadPeso?.IdUnidad : null
            });
        }

        var json = JsonSerializer.Serialize(detalleJsonList);

        using var conn = ConexionBD.ObtenerConexion();
        using var cmd = new SqlCommand("dbo.sp_RegistrarDonacion", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.Add("@IdDonante", SqlDbType.Int).Value = idDonante;
        cmd.Parameters.Add("@IdUsuarioRegistro", SqlDbType.Int).Value = idUsuarioRegistro;
        cmd.Parameters.Add("@Observaciones", SqlDbType.VarChar, 300).Value = (object?)observaciones ?? DBNull.Value;
        cmd.Parameters.Add("@DetalleJSON", SqlDbType.NVarChar, -1).Value = json;

        await conn.OpenAsync();
        var resultado = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(resultado);
    }
}
