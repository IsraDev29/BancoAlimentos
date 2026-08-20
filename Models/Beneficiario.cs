namespace BancoAlimentos.Avalonia.Models;

public class Beneficiario
{
    public int IdBeneficiario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }

    public override string ToString() => Nombre;
}
