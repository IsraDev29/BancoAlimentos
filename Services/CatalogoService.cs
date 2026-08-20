using System.Collections.Generic;
using System.Threading.Tasks;
using BancoAlimentos.Avalonia.Data;
using BancoAlimentos.Avalonia.Models;
using Microsoft.Data.SqlClient;

namespace BancoAlimentos.Avalonia.Services;

/// <summary>
/// Consultas de solo lectura a los catálogos usados en los formularios (ComboBox, etc).
/// </summary>
public class CatalogoService
{
    public async Task<List<Donante>> ObtenerDonantesAsync()
    {
        var lista = new List<Donante>();
        using var conn = ConexionBD.ObtenerConexion();
        using var cmd = new SqlCommand(@"
            SELECT d.IdDonante, d.Nombre, d.IdTipoDonante, d.Telefono, d.Correo, t.Descripcion AS TipoDonanteDescripcion
            FROM dbo.Donante d
            INNER JOIN dbo.TipoDonante t ON t.IdTipoDonante = d.IdTipoDonante
            WHERE d.Activo = 1
            ORDER BY d.Nombre;", conn);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new Donante
            {
                IdDonante = reader.GetInt32(reader.GetOrdinal("IdDonante")),
                Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                IdTipoDonante = reader.GetInt32(reader.GetOrdinal("IdTipoDonante")),
                Telefono = reader.IsDBNull(reader.GetOrdinal("Telefono")) ? null : reader.GetString(reader.GetOrdinal("Telefono")),
                Correo = reader.IsDBNull(reader.GetOrdinal("Correo")) ? null : reader.GetString(reader.GetOrdinal("Correo")),
                TipoDonanteDescripcion = reader.GetString(reader.GetOrdinal("TipoDonanteDescripcion")),
            });
        }
        return lista;
    }

    public async Task<List<Producto>> ObtenerProductosAsync()
    {
        var lista = new List<Producto>();
        using var conn = ConexionBD.ObtenerConexion();
        using var cmd = new SqlCommand(@"
            SELECT p.IdProducto, p.Nombre, c.Nombre AS Categoria, u.Abreviatura AS UnidadAbreviatura, p.DiasAlertaVencimiento
            FROM dbo.Producto p
            INNER JOIN dbo.CategoriaProducto c ON c.IdCategoria = p.IdCategoria
            INNER JOIN dbo.UnidadMedida u ON u.IdUnidad = p.IdUnidad
            WHERE p.Activo = 1
            ORDER BY p.Nombre;", conn);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new Producto
            {
                IdProducto = reader.GetInt32(reader.GetOrdinal("IdProducto")),
                Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                Categoria = reader.GetString(reader.GetOrdinal("Categoria")),
                UnidadAbreviatura = reader.GetString(reader.GetOrdinal("UnidadAbreviatura")),
                DiasAlertaVencimiento = reader.GetInt32(reader.GetOrdinal("DiasAlertaVencimiento")),
            });
        }
        return lista;
    }

    public async Task<List<Beneficiario>> ObtenerBeneficiariosAsync()
    {
        var lista = new List<Beneficiario>();
        using var conn = ConexionBD.ObtenerConexion();
        using var cmd = new SqlCommand(@"
            SELECT IdBeneficiario, Nombre, Tipo, Direccion, Telefono
            FROM dbo.Beneficiario
            WHERE Activo = 1
            ORDER BY Nombre;", conn);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            lista.Add(new Beneficiario
            {
                IdBeneficiario = reader.GetInt32(reader.GetOrdinal("IdBeneficiario")),
                Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                Tipo = reader.GetString(reader.GetOrdinal("Tipo")),
                Direccion = reader.IsDBNull(reader.GetOrdinal("Direccion")) ? null : reader.GetString(reader.GetOrdinal("Direccion")),
                Telefono = reader.IsDBNull(reader.GetOrdinal("Telefono")) ? null : reader.GetString(reader.GetOrdinal("Telefono")),
            });
        }
        return lista;
    }
}
