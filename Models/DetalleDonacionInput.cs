using System;

namespace BancoAlimentos.Avalonia.Models;

/// <summary>
/// Representa una línea del detalle mientras se está armando una donación en la UI,
/// antes de enviarla al procedimiento almacenado como JSON.
/// </summary>
public class DetalleDonacionInput
{
    public Producto? ProductoSeleccionado { get; set; }
    public decimal Cantidad { get; set; }
    public DateTime FechaVencimiento { get; set; } = DateTime.Today.AddMonths(1);

    // ---------- Empaquetado / envasado ----------

    /// <summary>Marca si el alimento viene en envases (latas, bolsas, cajas).</summary>
    public bool EsEmpaquetado { get; set; }

    /// <summary>Cuántos envases entraron.</summary>
    public decimal? CantidadEnvases { get; set; }

    /// <summary>Peso o volumen de cada envase.</summary>
    public decimal? PesoPorEnvase { get; set; }

    /// <summary>Unidad en la que se expresa el peso de cada envase.</summary>
    public UnidadMedida? UnidadPeso { get; set; }

    public string ProductoNombre => ProductoSeleccionado?.Nombre ?? string.Empty;
    public string UnidadAbreviatura => ProductoSeleccionado?.UnidadAbreviatura ?? string.Empty;

    /// <summary>Se muestra en la tabla del detalle: "10 × 2.5 Kg", o "Granel".</summary>
    public string Empaque =>
        EsEmpaquetado && CantidadEnvases is > 0 && PesoPorEnvase is > 0
            ? $"{CantidadEnvases:0.##} × {PesoPorEnvase:0.###} {UnidadPeso?.Abreviatura}"
            : "Granel";
}
