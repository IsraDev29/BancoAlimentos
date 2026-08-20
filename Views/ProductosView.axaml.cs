using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using BancoAlimentos.Avalonia.ViewModels;

namespace BancoAlimentos.Avalonia.Views;

public partial class ProductosView : UserControl
{
    public ProductosView()
    {
        InitializeComponent();
        KeyDown += (_, e) => TeclasCrud.Manejar(e, DataContext as ProductosViewModel);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
