using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BancoAlimentos.Avalonia.Views;

public partial class AlertasView : UserControl
{
    public AlertasView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
