namespace BancoAlimentos.Avalonia.Models;

public class Beneficiario
{
    public int IdBeneficiario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? ResponsableContacto { get; set; }
    public bool Activo { get; set; } = true;

    public override string ToString() => Nombre;

    public Beneficiario Clonar() => (Beneficiario)MemberwiseClone();
}
