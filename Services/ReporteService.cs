using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using BancoAlimentos.Avalonia.Data;
using BancoAlimentos.Avalonia.Models;
using Microsoft.Data.SqlClient;

namespace BancoAlimentos.Avalonia.Services;

/// <summary>
/// Reportes para donantes (RF-04) y entes fiscalizadores (RF-05), con filtro
/// por rango de fechas y agregados para las gráficas.
/// </summary>
public class ReporteService
{
    public Task<List<FilaReporteDonacion>> ObtenerDonacionesAsync(DateTime desde, DateTime hasta) =>
        LeerAsync("dbo.sp_ReporteDonaciones", desde, hasta, r => new FilaReporteDonacion
        {
            IdDonacion = r.GetInt32(r.GetOrdinal("IdDonacion")),
            FechaRecepcion = r.GetDateTime(r.GetOrdinal("FechaRecepcion")),
            Donante = r.GetString(r.GetOrdinal("Donante")),
            TipoDonante = r.GetString(r.GetOrdinal("TipoDonante")),
            Producto = r.GetString(r.GetOrdinal("Producto")),
            Categoria = r.GetString(r.GetOrdinal("Categoria")),
            Cantidad = r.GetDecimal(r.GetOrdinal("Cantidad")),
            Unidad = r.GetString(r.GetOrdinal("Unidad")),
            FechaVencimiento = r.GetDateTime(r.GetOrdinal("FechaVencimiento")),
            EsEmpaquetado = r.GetBoolean(r.GetOrdinal("EsEmpaquetado")),
            CantidadPaquetes = Decimal(r, "CantidadEnvases"),
            ProductosPorPaquete = Decimal(r, "ProductosPorPaquete"),
            CantidadProductos = Decimal(r, "CantidadProductos"),
            PesoPorProducto = Decimal(r, "PesoPorEnvase"),
            UnidadPeso = Texto(r, "UnidadPeso"),
            RegistradoPor = r.GetString(r.GetOrdinal("RegistradoPor")),
        });

    public Task<List<FilaReporteDistribucion>> ObtenerDistribucionAsync(DateTime desde, DateTime hasta) =>
        LeerAsync("dbo.sp_ReporteDistribucion", desde, hasta, r => new FilaReporteDistribucion
        {
            IdDistribucion = r.GetInt32(r.GetOrdinal("IdDistribucion")),
            FechaEntrega = r.GetDateTime(r.GetOrdinal("FechaEntrega")),
            Beneficiario = r.GetString(r.GetOrdinal("Beneficiario")),
            TipoBeneficiario = r.GetString(r.GetOrdinal("TipoBeneficiario")),
            Producto = r.GetString(r.GetOrdinal("Producto")),
            Categoria = r.GetString(r.GetOrdinal("Categoria")),
            CantidadEntregada = r.GetDecimal(r.GetOrdinal("CantidadEntregada")),
            Unidad = r.GetString(r.GetOrdinal("Unidad")),
            EntregadoPor = r.GetString(r.GetOrdinal("EntregadoPor")),
        });

    public Task<List<BarraReporte>> DonacionesPorDonanteAsync(DateTime desde, DateTime hasta) =>
        LeerBarrasAsync("dbo.sp_ReporteDonacionesPorDonante", desde, hasta);

    public Task<List<BarraReporte>> DonacionesPorCategoriaAsync(DateTime desde, DateTime hasta) =>
        LeerBarrasAsync("dbo.sp_ReporteDonacionesPorCategoria", desde, hasta);

    public Task<List<BarraReporte>> DistribucionPorBeneficiarioAsync(DateTime desde, DateTime hasta) =>
        LeerBarrasAsync("dbo.sp_ReporteDistribucionPorBeneficiario", desde, hasta);

    public Task<List<BarraReporte>> DistribucionPorProductoAsync(DateTime desde, DateTime hasta) =>
        LeerBarrasAsync("dbo.sp_ReporteDistribucionPorProducto", desde, hasta);

    // ------------------------------------------------------------------

    private static Task<List<BarraReporte>> LeerBarrasAsync(string sp, DateTime desde, DateTime hasta) =>
        LeerAsync(sp, desde, hasta, r => new BarraReporte
        {
            Etiqueta = r.GetString(r.GetOrdinal("Etiqueta")),
            Valor = r.GetDecimal(r.GetOrdinal("Valor")),
            Lotes = r.GetInt32(r.GetOrdinal("Lotes")),
        });

    private static async Task<List<T>> LeerAsync<T>(
        string procedimiento, DateTime desde, DateTime hasta, Func<SqlDataReader, T> mapear)
    {
        var lista = new List<T>();
        using var conn = ConexionBD.ObtenerConexion();
        using var cmd = new SqlCommand(procedimiento, conn) { CommandType = CommandType.StoredProcedure };
        cmd.Parameters.Add("@Desde", SqlDbType.Date).Value = desde.Date;
        cmd.Parameters.Add("@Hasta", SqlDbType.Date).Value = hasta.Date;

        await conn.OpenAsync();
        try
        {
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                lista.Add(mapear(reader));
        }
        catch (SqlException ex) when (ErroresSql.Traducir(ex) is { } traducido)
        {
            throw traducido;
        }

        return lista;
    }

    private static decimal? Decimal(SqlDataReader r, string columna)
    {
        var i = r.GetOrdinal(columna);
        return r.IsDBNull(i) ? null : r.GetDecimal(i);
    }

    private static string? Texto(SqlDataReader r, string columna)
    {
        var i = r.GetOrdinal(columna);
        return r.IsDBNull(i) ? null : r.GetString(i);
    }
}
