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
}
