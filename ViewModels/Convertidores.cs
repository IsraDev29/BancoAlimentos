using Avalonia.Data.Converters;

namespace BancoAlimentos.Avalonia.ViewModels;

/// <summary>Convertidores usados desde XAML.</summary>
public static class Convertidores
{
    /// <summary>bool Activo -> "Activo" / "Inactivo", para los distintivos de las tablas.</summary>
    public static readonly IValueConverter ActivoInactivo =
        new FuncValueConverter<bool, string>(activo => activo ? "Activo" : "Inactivo");
}
