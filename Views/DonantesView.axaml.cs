using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BancoAlimentos.Avalonia.ViewModels;

namespace BancoAlimentos.Avalonia.Views;

public partial class DonantesView : UserControl
{
    public DonantesView()
    {
        InitializeComponent();
        KeyDown += (_, e) => TeclasCrud.Manejar(e, DataContext as DonantesViewModel);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
