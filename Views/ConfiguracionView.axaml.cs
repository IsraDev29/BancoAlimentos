using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BancoAlimentos.Avalonia.Views;

public partial class ConfiguracionView : UserControl
{
    public ConfiguracionView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
