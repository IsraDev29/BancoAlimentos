using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BancoAlimentos.Avalonia.Views;

public partial class InventarioView : UserControl
{
    public InventarioView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
