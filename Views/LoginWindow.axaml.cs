using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using BancoAlimentos.Avalonia.ViewModels;

namespace BancoAlimentos.Avalonia.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;

    public LoginWindow()
    {
        InitializeComponent();

        _vm = new LoginViewModel();
        _vm.LoginExitoso += usuario =>
        {
            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel(usuario)
            };
            mainWindow.Show();
            Close();
        };

        DataContext = _vm;

        // Los TextBox de una línea no consumen Enter ni Escape, así que el evento
        // llega hasta la ventana por burbujeo y no hace falta interceptar en túnel.
        KeyDown += AlPresionarTecla;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        // Foco en el primer campo para poder escribir y tabular de inmediato.
        this.FindControl<TextBox>("CampoUsuario")?.Focus();
    }

    private void AlPresionarTecla(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:      // envía el formulario de la pestaña visible
                _vm.EnviarFormularioActivo();
                e.Handled = true;
                break;

            case Key.Escape:     // limpia el formulario visible
                _vm.LimpiarFormularioActivo();
                e.Handled = true;
                break;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
