using System;

namespace BancoAlimentos.Avalonia.Models;

public class InventarioItem
{
    public int IdDetalleDonacion { get; set; }
    public int IdProducto { get; set; }
    public string Producto { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string Unidad { get; set; } = string.Empty;
    public decimal CantidadDisponible { get; set; }
    public DateTime FechaVencimiento { get; set; }
    public DateTime FechaRecepcion { get; set; }
    public string Donante { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;

    // Para el DataGrid de distribución: cuánto se va a entregar de este lote
    public decimal CantidadAEntregar { get; set; }

    // Permiten colorear el distintivo de estado en las tablas sin un IValueConverter:
    // se enlazan a Classes.ok / Classes.aviso / Classes.peligro.
    public bool EstadoEsVigente => Estado == "Vigente";
    public bool EstadoEsPorVencer => Estado == "Por vencer";
    public bool EstadoEsVencido => Estado == "Vencido";

    // ---------- Empaquetado del lote ----------

    public bool EsEmpaquetado { get; set; }
    public decimal? CantidadPaquetes { get; set; }
    public decimal? ProductosPorPaquete { get; set; }
    public decimal? CantidadProductos { get; set; }
    public decimal? PesoPorProducto { get; set; }
    public string? UnidadPeso { get; set; }

    /// <summary>
    /// Columna «EMPAQUE» de las tablas: "2 paq × 12 de 0.4 Kg" si viene
    /// empaquetado, o "24 de 0.4 Kg" si se capturó como producto individual.
    /// </summary>
    public string DescripcionEmpaque
    {
        get
        {
            var peso = PesoPorProducto is > 0
                ? $" de {PesoPorProducto:0.###} {UnidadPeso}"
                : string.Empty;

            if (EsEmpaquetado && CantidadPaquetes is > 0 && ProductosPorPaquete is > 0)
                return $"{CantidadPaquetes:0.##} paq × {ProductosPorPaquete:0.##}{peso}";

            return CantidadProductos is > 0 ? $"{CantidadProductos:0.##}{peso}" : "—";
        }
    }
}
