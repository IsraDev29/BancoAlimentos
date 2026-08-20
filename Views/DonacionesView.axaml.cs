using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using BancoAlimentos.Avalonia.ViewModels;

namespace BancoAlimentos.Avalonia.Views;

public partial class DonacionesView : UserControl
{
    public DonacionesView()
    {
        InitializeComponent();
        KeyDown += AlPresionarTecla;
    }

    private void AlPresionarTecla(object? sender, KeyEventArgs e)
    {
        if (DataContext is not DonacionesViewModel vm) return;

        // El campo de observaciones acepta saltos de línea: ahí Enter debe
        // escribir, no confirmar el formulario.
        if (e.Source is TextBox { AcceptsReturn: true }) return;

        switch (e.Key)
        {
            case Key.Enter when vm.AgregarDetalleCommand.CanExecute(null):
                vm.AgregarDetalleCommand.Execute(null);   // agrega la línea capturada
                e.Handled = true;
                break;

            case Key.Escape:
                vm.LimpiarCapturaRapida();                // descarta la captura en curso
                e.Handled = true;
                break;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
