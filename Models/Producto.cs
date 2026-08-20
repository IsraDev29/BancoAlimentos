namespace BancoAlimentos.Avalonia.Models;

public class Producto
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public int IdCategoria { get; set; }
    public string Categoria { get; set; } = string.Empty;

    public int IdUnidad { get; set; }
    public string UnidadAbreviatura { get; set; } = string.Empty;
    public string UnidadNombre { get; set; } = string.Empty;

    public int DiasAlertaVencimiento { get; set; }
    public bool Activo { get; set; } = true;

    public override string ToString() => Nombre;

    public Producto Clonar() => (Producto)MemberwiseClone();
}
