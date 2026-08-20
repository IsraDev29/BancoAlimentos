namespace BancoAlimentos.Avalonia.Models;

/// <summary>Unidad de medida (Kilogramo/Kg, Litro/L, Unidad/Unid, Caja).</summary>
public class UnidadMedida
{
    public int IdUnidad { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Abreviatura { get; set; } = string.Empty;

    public override string ToString() => $"{Nombre} ({Abreviatura})";
}

/// <summary>Categoría de producto (Granos y Cereales, Lácteos, …).</summary>
public class CategoriaProducto
{
    public int IdCategoria { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public override string ToString() => Nombre;
}

/// <summary>Tipo de donante: Empresa o Particular.</summary>
public class TipoDonante
{
    public int IdTipoDonante { get; set; }
    public string Descripcion { get; set; } = string.Empty;

    public override string ToString() => Descripcion;
}
