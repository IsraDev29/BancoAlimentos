using System;
using System.Collections.Generic;

namespace BancoAlimentos.Avalonia.Models;

/// <summary>
/// Convierte entre unidades de la misma familia para calcular el total de un lote
/// empaquetado. Sin esto, "12 envases × 400 g" de un producto medido en Kg daba
/// 4800, mezclando envases con gramos.
/// </summary>
public static class ConversionUnidades
{
    /// <summary>Factor de cada unidad respecto a la base de su familia (gramo o mililitro).</summary>
    private static readonly Dictionary<string, (string Familia, decimal Factor)> Tabla =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["g"]    = ("masa", 1m),
            ["Kg"]   = ("masa", 1000m),
            ["Lb"]   = ("masa", 453.59237m),
            ["ml"]   = ("volumen", 1m),
            ["L"]    = ("volumen", 1000m),
        };

    /// <summary>
    /// Total del lote expresado en la unidad del producto.
    /// Si ambas unidades son de la misma familia (masa o volumen) convierte y multiplica;
    /// si el producto se mide en piezas (Unid, Caja) o las familias no coinciden,
    /// el total es simplemente la cantidad de envases.
    /// </summary>
    public static decimal CalcularTotal(
        decimal cantidadEnvases, decimal pesoPorEnvase,
        string? unidadEnvase, string? unidadProducto)
    {
        if (unidadEnvase is null || unidadProducto is null)
            return cantidadEnvases;

        if (!Tabla.TryGetValue(unidadEnvase, out var origen) ||
            !Tabla.TryGetValue(unidadProducto, out var destino) ||
            origen.Familia != destino.Familia)
            return cantidadEnvases;

        // pesoPorEnvase -> base de la familia -> unidad del producto
        var totalEnBase = cantidadEnvases * pesoPorEnvase * origen.Factor;
        return Math.Round(totalEnBase / destino.Factor, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>Indica si el total se pudo derivar por conversión y no es un simple conteo.</summary>
    public static bool SonConvertibles(string? unidadEnvase, string? unidadProducto) =>
        unidadEnvase is not null && unidadProducto is not null &&
        Tabla.TryGetValue(unidadEnvase, out var a) &&
        Tabla.TryGetValue(unidadProducto, out var b) &&
        a.Familia == b.Familia;
}
