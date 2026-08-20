using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BancoAlimentos.Avalonia.ViewModels;

namespace BancoAlimentos.Avalonia.Views;

public partial class BeneficiariosView : UserControl
{
    public BeneficiariosView()
    {
        InitializeComponent();
        KeyDown += (_, e) => TeclasCrud.Manejar(e, DataContext as BeneficiariosViewModel);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
