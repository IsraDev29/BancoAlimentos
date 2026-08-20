using System;

namespace BancoAlimentos.Avalonia.Models;

/// <summary>Una línea del reporte de donaciones (RF-04).</summary>
public class FilaReporteDonacion
{
    public int IdDonacion { get; set; }
    public DateTime FechaRecepcion { get; set; }
    public string Donante { get; set; } = string.Empty;
    public string TipoDonante { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public string Unidad { get; set; } = string.Empty;
    public DateTime FechaVencimiento { get; set; }
    public bool EsEmpaquetado { get; set; }
    public decimal? CantidadPaquetes { get; set; }
    public decimal? ProductosPorPaquete { get; set; }
    public decimal? CantidadProductos { get; set; }
    public decimal? PesoPorProducto { get; set; }
    public string? UnidadPeso { get; set; }
    public string RegistradoPor { get; set; } = string.Empty;

    public string Empaque
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

/// <summary>Una línea del reporte de distribución (RF-05).</summary>
public class FilaReporteDistribucion
{
    public int IdDistribucion { get; set; }
    public DateTime FechaEntrega { get; set; }
    public string Beneficiario { get; set; } = string.Empty;
    public string TipoBeneficiario { get; set; } = string.Empty;
    public string Producto { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public decimal CantidadEntregada { get; set; }
    public string Unidad { get; set; } = string.Empty;
    public string EntregadoPor { get; set; } = string.Empty;
}

/// <summary>
/// Una barra de las gráficas. El ancho se calcula en el ViewModel porque
/// Avalonia no permite enlazar un ancho proporcional (tipo estrella) desde datos.
/// </summary>
public class BarraReporte
{
    public string Etiqueta { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int Lotes { get; set; }

    /// <summary>Ancho en píxeles de la barra, ya escalado contra el valor máximo.</summary>
    public double Ancho { get; set; }

    public string ValorTexto => $"{Valor:0.##}";
    public string DetalleTexto => Lotes == 1 ? "1 lote" : $"{Lotes} lotes";
}
