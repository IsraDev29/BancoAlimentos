namespace BancoAlimentos.Avalonia.Models;

public class Donante
{
    public int IdDonante { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int IdTipoDonante { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }

    public string TipoDonanteDescripcion { get; set; } = string.Empty;

    // Se muestra en el ComboBox de donaciones: "Nombre (Tipo)".
    public override string ToString() =>
        string.IsNullOrEmpty(TipoDonanteDescripcion) ? Nombre : $"{Nombre} ({TipoDonanteDescripcion})";
}
