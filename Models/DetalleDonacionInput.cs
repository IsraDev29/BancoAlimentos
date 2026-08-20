using System;

namespace BancoAlimentos.Avalonia.Models;

/// <summary>
/// Una línea del detalle mientras se arma la donación en la UI, antes de
/// enviarla al procedimiento almacenado como JSON.
///
/// Hay dos formas excluyentes de describir el lote:
///   • Individual  → CantidadProductos productos, cada uno de PesoPorProducto.
///   • Empaquetado → CantidadPaquetes paquetes × ProductosPorPaquete productos,
///                   cada producto de PesoPorProducto.
/// </summary>
public class DetalleDonacionInput
{
    public Producto? ProductoSeleccionado { get; set; }

    /// <summary>Total del lote en la unidad del alimento; es lo que entra al inventario.</summary>
    public decimal Cantidad { get; set; }

    public DateTime FechaVencimiento { get; set; } = DateTime.Today.AddMonths(1);

    /// <summary>Marca si el lote se capturó como producto empaquetado.</summary>
    public bool EsEmpaquetado { get; set; }

    /// <summary>Cuántos productos individuales trae el lote (en ambos modos).</summary>
    public decimal? CantidadProductos { get; set; }

    /// <summary>Peso o volumen de un solo producto.</summary>
    public decimal? PesoPorProducto { get; set; }

    /// <summary>Unidad en la que se expresa el peso de un producto.</summary>
    public UnidadMedida? UnidadPeso { get; set; }

    // Sólo cuando EsEmpaquetado
    public decimal? CantidadPaquetes { get; set; }
    public decimal? ProductosPorPaquete { get; set; }

    public string ProductoNombre => ProductoSeleccionado?.Nombre ?? string.Empty;
    public string UnidadAbreviatura => ProductoSeleccionado?.UnidadAbreviatura ?? string.Empty;

    /// <summary>
    /// Columna «EMPAQUE» de las tablas: "2 paq × 12 de 0.4 Kg" cuando viene
    /// empaquetado, o "24 de 0.4 Kg" cuando es individual.
    /// </summary>
    public string Empaque
    {
        get
        {
            var peso = PesoPorProducto is > 0
                ? $" de {PesoPorProducto:0.###} {UnidadPeso?.Abreviatura}"
                : string.Empty;

            if (EsEmpaquetado && CantidadPaquetes is > 0 && ProductosPorPaquete is > 0)
                return $"{CantidadPaquetes:0.##} paq × {ProductosPorPaquete:0.##}{peso}";

            return CantidadProductos is > 0
                ? $"{CantidadProductos:0.##}{peso}"
                : "—";
        }
    }
}
