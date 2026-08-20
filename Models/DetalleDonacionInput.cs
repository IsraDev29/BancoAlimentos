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

    public string ProductoNombre => ProductoSeleccionado?.Nombre ?? string.Empty;
    public string UnidadAbreviatura => ProductoSeleccionado?.UnidadAbreviatura ?? string.Empty;
}
