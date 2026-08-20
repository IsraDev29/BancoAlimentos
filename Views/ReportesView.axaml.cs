using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BancoAlimentos.Avalonia.Views;

public partial class ReportesView : UserControl
{
    public ReportesView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
