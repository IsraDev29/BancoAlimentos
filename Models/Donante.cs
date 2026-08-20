namespace BancoAlimentos.Avalonia.Models;

public class Donante
{
    public int IdDonante { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int IdTipoDonante { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;

    public string TipoDonanteDescripcion { get; set; } = string.Empty;

    /// <summary>
    /// Los dos tipos de donante del catálogo dbo.TipoDonante: Empresa y Particular.
    /// Se usan para colorear el distintivo en la tabla de donantes.
    /// </summary>
    public bool EsEmpresa => TipoDonanteDescripcion == "Empresa";
    public bool EsParticular => !EsEmpresa;

    // Se muestra en el ComboBox de donaciones: "Nombre (Tipo)".
    public override string ToString() =>
        string.IsNullOrEmpty(TipoDonanteDescripcion) ? Nombre : $"{Nombre} ({TipoDonanteDescripcion})";

    public Donante Clonar() => (Donante)MemberwiseClone();
}
