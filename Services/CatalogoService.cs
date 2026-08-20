using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using BancoAlimentos.Avalonia.Data;
using BancoAlimentos.Avalonia.Models;
using Microsoft.Data.SqlClient;

namespace BancoAlimentos.Avalonia.Services;

/// <summary>
/// Altas, bajas, modificaciones y consultas de los catálogos: alimentos,
/// donantes y beneficiarios. Todo pasa por procedimientos almacenados.
/// </summary>
public class CatalogoService
{
    // ------------------------------------------------------------------
    // Catálogos de apoyo para los formularios
    // ------------------------------------------------------------------

    public Task<List<UnidadMedida>> ObtenerUnidadesAsync() =>
        LeerAsync("dbo.sp_ObtenerUnidadesMedida", r => new UnidadMedida
        {
            IdUnidad = r.GetInt32(r.GetOrdinal("IdUnidad")),
            Nombre = r.GetString(r.GetOrdinal("Nombre")),
            Abreviatura = r.GetString(r.GetOrdinal("Abreviatura")),
        });

    public Task<List<CategoriaProducto>> ObtenerCategoriasAsync() =>
        LeerAsync("dbo.sp_ObtenerCategorias", r => new CategoriaProducto
        {
            IdCategoria = r.GetInt32(r.GetOrdinal("IdCategoria")),
            Nombre = r.GetString(r.GetOrdinal("Nombre")),
        });

    public Task<List<TipoDonante>> ObtenerTiposDonanteAsync() =>
        LeerAsync("dbo.sp_ObtenerTiposDonante", r => new TipoDonante
        {
            IdTipoDonante = r.GetInt32(r.GetOrdinal("IdTipoDonante")),
            Descripcion = r.GetString(r.GetOrdinal("Descripcion")),
        });

    // ------------------------------------------------------------------
    // Alimentos (dbo.Producto)
    // ------------------------------------------------------------------

    public Task<List<Producto>> ObtenerProductosAsync(bool incluirInactivos = false) =>
        LeerAsync("dbo.sp_ObtenerProductos", r => new Producto
        {
            IdProducto = r.GetInt32(r.GetOrdinal("IdProducto")),
            Nombre = r.GetString(r.GetOrdinal("Nombre")),
            IdCategoria = r.GetInt32(r.GetOrdinal("IdCategoria")),
            Categoria = r.GetString(r.GetOrdinal("Categoria")),
            IdUnidad = r.GetInt32(r.GetOrdinal("IdUnidad")),
            UnidadAbreviatura = r.GetString(r.GetOrdinal("UnidadAbreviatura")),
            UnidadNombre = r.GetString(r.GetOrdinal("UnidadNombre")),
            DiasAlertaVencimiento = r.GetInt32(r.GetOrdinal("DiasAlertaVencimiento")),
            Activo = r.GetBoolean(r.GetOrdinal("Activo")),
        }, cmd => cmd.Parameters.Add("@IncluirInactivos", SqlDbType.Bit).Value = incluirInactivos);

    /// <summary>Inserta si IdProducto es 0; si no, actualiza. Devuelve el Id.</summary>
    public Task<int> GuardarProductoAsync(Producto p) =>
        EjecutarEscalarAsync("dbo.sp_GuardarProducto", cmd =>
        {
            cmd.Parameters.Add("@IdProducto", SqlDbType.Int).Value = p.IdProducto;
            cmd.Parameters.Add("@Nombre", SqlDbType.VarChar, 120).Value = p.Nombre;
            cmd.Parameters.Add("@IdCategoria", SqlDbType.Int).Value = p.IdCategoria;
            cmd.Parameters.Add("@IdUnidad", SqlDbType.Int).Value = p.IdUnidad;
            cmd.Parameters.Add("@DiasAlertaVencimiento", SqlDbType.Int).Value = p.DiasAlertaVencimiento;
            cmd.Parameters.Add("@Activo", SqlDbType.Bit).Value = p.Activo;
        });

    /// <summary>true = borrado definitivo; false = desactivado por tener donaciones asociadas.</summary>
    public async Task<bool> EliminarProductoAsync(int idProducto) =>
        await EjecutarEscalarAsync("dbo.sp_EliminarProducto",
            cmd => cmd.Parameters.Add("@IdProducto", SqlDbType.Int).Value = idProducto) == 1;

    // ------------------------------------------------------------------
    // Donantes
    // ------------------------------------------------------------------

    public Task<List<Donante>> ObtenerDonantesAsync(bool incluirInactivos = false) =>
        LeerAsync("dbo.sp_ObtenerDonantes", r => new Donante
        {
            IdDonante = r.GetInt32(r.GetOrdinal("IdDonante")),
            Nombre = r.GetString(r.GetOrdinal("Nombre")),
            IdTipoDonante = r.GetInt32(r.GetOrdinal("IdTipoDonante")),
            TipoDonanteDescripcion = r.GetString(r.GetOrdinal("TipoDonanteDescripcion")),
            Telefono = Texto(r, "Telefono"),
            Correo = Texto(r, "Correo"),
            Direccion = Texto(r, "Direccion"),
            Activo = r.GetBoolean(r.GetOrdinal("Activo")),
        }, cmd => cmd.Parameters.Add("@IncluirInactivos", SqlDbType.Bit).Value = incluirInactivos);

    public Task<int> GuardarDonanteAsync(Donante d) =>
        EjecutarEscalarAsync("dbo.sp_GuardarDonante", cmd =>
        {
            cmd.Parameters.Add("@IdDonante", SqlDbType.Int).Value = d.IdDonante;
            cmd.Parameters.Add("@Nombre", SqlDbType.VarChar, 120).Value = d.Nombre;
            cmd.Parameters.Add("@IdTipoDonante", SqlDbType.Int).Value = d.IdTipoDonante;
            cmd.Parameters.Add("@Telefono", SqlDbType.VarChar, 20).Value = Nulo(d.Telefono);
            cmd.Parameters.Add("@Correo", SqlDbType.VarChar, 100).Value = Nulo(d.Correo);
            cmd.Parameters.Add("@Direccion", SqlDbType.VarChar, 200).Value = Nulo(d.Direccion);
            cmd.Parameters.Add("@Activo", SqlDbType.Bit).Value = d.Activo;
        });

    public async Task<bool> EliminarDonanteAsync(int idDonante) =>
        await EjecutarEscalarAsync("dbo.sp_EliminarDonante",
            cmd => cmd.Parameters.Add("@IdDonante", SqlDbType.Int).Value = idDonante) == 1;

    // ------------------------------------------------------------------
    // Beneficiarios
    // ------------------------------------------------------------------

    public Task<List<Beneficiario>> ObtenerBeneficiariosAsync(bool incluirInactivos = false) =>
        LeerAsync("dbo.sp_ObtenerBeneficiarios", r => new Beneficiario
        {
            IdBeneficiario = r.GetInt32(r.GetOrdinal("IdBeneficiario")),
            Nombre = r.GetString(r.GetOrdinal("Nombre")),
            Tipo = r.GetString(r.GetOrdinal("Tipo")),
            Direccion = Texto(r, "Direccion"),
            Telefono = Texto(r, "Telefono"),
            ResponsableContacto = Texto(r, "ResponsableContacto"),
            Activo = r.GetBoolean(r.GetOrdinal("Activo")),
        }, cmd => cmd.Parameters.Add("@IncluirInactivos", SqlDbType.Bit).Value = incluirInactivos);

    public Task<int> GuardarBeneficiarioAsync(Beneficiario b) =>
        EjecutarEscalarAsync("dbo.sp_GuardarBeneficiario", cmd =>
        {
            cmd.Parameters.Add("@IdBeneficiario", SqlDbType.Int).Value = b.IdBeneficiario;
            cmd.Parameters.Add("@Nombre", SqlDbType.VarChar, 120).Value = b.Nombre;
            cmd.Parameters.Add("@Tipo", SqlDbType.VarChar, 30).Value = b.Tipo;
            cmd.Parameters.Add("@Direccion", SqlDbType.VarChar, 200).Value = Nulo(b.Direccion);
            cmd.Parameters.Add("@Telefono", SqlDbType.VarChar, 20).Value = Nulo(b.Telefono);
            cmd.Parameters.Add("@ResponsableContacto", SqlDbType.VarChar, 100).Value = Nulo(b.ResponsableContacto);
            cmd.Parameters.Add("@Activo", SqlDbType.Bit).Value = b.Activo;
        });

    public async Task<bool> EliminarBeneficiarioAsync(int idBeneficiario) =>
        await EjecutarEscalarAsync("dbo.sp_EliminarBeneficiario",
            cmd => cmd.Parameters.Add("@IdBeneficiario", SqlDbType.Int).Value = idBeneficiario) == 1;

    // ------------------------------------------------------------------
    // Ayudantes
    // ------------------------------------------------------------------

    private static string? Texto(SqlDataReader r, string columna)
    {
        var i = r.GetOrdinal(columna);
        return r.IsDBNull(i) ? null : r.GetString(i);
    }

    private static object Nulo(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? DBNull.Value : valor.Trim();

    private static async Task<List<T>> LeerAsync<T>(
        string procedimiento, Func<SqlDataReader, T> mapear, Action<SqlCommand>? parametros = null)
    {
        var lista = new List<T>();
        using var conn = ConexionBD.ObtenerConexion();
        using var cmd = new SqlCommand(procedimiento, conn) { CommandType = CommandType.StoredProcedure };
        parametros?.Invoke(cmd);

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

    /// <summary>
    /// Ejecuta un procedimiento de escritura y devuelve el entero que produce.
    /// Traduce los errores de validación de SQL (50020+) a InvalidOperationException
    /// para que el ViewModel los muestre tal cual al usuario.
    /// </summary>
    private static async Task<int> EjecutarEscalarAsync(string procedimiento, Action<SqlCommand> parametros)
    {
        using var conn = ConexionBD.ObtenerConexion();
        using var cmd = new SqlCommand(procedimiento, conn) { CommandType = CommandType.StoredProcedure };
        parametros(cmd);

        await conn.OpenAsync();
        try
        {
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }
        catch (SqlException ex) when (ErroresSql.Traducir(ex) is not null)
        {
            throw ErroresSql.Traducir(ex)!;
        }
        catch (SqlException ex) when (ex.Number >= 50020 && ex.Number < 51000)
        {
            throw new InvalidOperationException(ex.Message, ex);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new InvalidOperationException("Ya existe un registro con ese nombre.", ex);
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            throw new InvalidOperationException(
                "No se puede completar la operación porque el registro está referenciado por otros datos.", ex);
        }
    }
}
