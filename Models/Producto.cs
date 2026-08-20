namespace BancoAlimentos.Avalonia.Models;

public class Producto
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string UnidadAbreviatura { get; set; } = string.Empty;
    public int DiasAlertaVencimiento { get; set; }

    public override string ToString() => Nombre;
}
