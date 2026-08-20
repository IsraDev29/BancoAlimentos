using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using BancoAlimentos.Avalonia.ViewModels;

namespace BancoAlimentos.Avalonia.Views;

public partial class MainWindow : Window
{
    private MainViewModel? _vm;

    public MainWindow()
    {
        InitializeComponent();

        KeyDown += AlPresionarTecla;
        DataContextChanged += (_, _) => EngancharViewModel();
    }

    private void EngancharViewModel()
    {
        if (_vm is not null)
            _vm.CerrarSesionSolicitado -= VolverAlLogin;

        _vm = DataContext as MainViewModel;

        if (_vm is not null)
            _vm.CerrarSesionSolicitado += VolverAlLogin;
    }

    private void VolverAlLogin()
    {
        // Abrir antes de cerrar: con ShutdownMode.OnLastWindowClose, cerrar la
        // única ventana visible terminaría la aplicación.
        new LoginWindow().Show();
        Close();
    }

    private void AlPresionarTecla(object? sender, KeyEventArgs e)
    {
        if (_vm is null) return;

        // Ctrl + 1..8 salta directo a cada módulo de la barra lateral.
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key is >= Key.D1 and <= Key.D8)
        {
            _vm.SeleccionarSeccionCommand.Execute(((int)e.Key - (int)Key.D1).ToString());
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.F5:              // recarga el módulo visible
                _vm.RecargarSeccionActual();
                e.Handled = true;
                break;

            case Key.Escape:          // descarta los mensajes de estado
                _vm.LimpiarMensajes();
                e.Handled = true;
                break;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
