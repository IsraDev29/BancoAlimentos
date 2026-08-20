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

/// <summary>
/// Una presentación del alimento: gramaje o volumen con su unidad ("400 g", "1 Kg").
/// Se usa en el constructor de variantes de la pantalla de donaciones.
/// </summary>
public class PresentacionVariante
{
    public decimal Valor { get; set; }
    public UnidadMedida? Unidad { get; set; }

    public string Texto => $"{Valor:0.###} {Unidad?.Abreviatura}";
    public override string ToString() => Texto;
}

/// <summary>Una cantidad de envases dentro del constructor de variantes.</summary>
public class CantidadVariante
{
    public decimal Valor { get; set; }

    public string Texto => $"{Valor:0.##}";
    public override string ToString() => Texto;
}
