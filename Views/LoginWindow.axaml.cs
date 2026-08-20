using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BancoAlimentos.Avalonia.ViewModels;

namespace BancoAlimentos.Avalonia.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();

        var vm = new LoginViewModel();
        vm.LoginExitoso += usuario =>
        {
            var mainWindow = new MainWindow
            {
                DataContext = new MainViewModel(usuario)
            };
            mainWindow.Show();
            Close();
        };

        DataContext = vm;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
