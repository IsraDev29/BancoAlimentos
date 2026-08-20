using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BancoAlimentos.Avalonia.Common;
using BancoAlimentos.Avalonia.Models;
using BancoAlimentos.Avalonia.Services;

namespace BancoAlimentos.Avalonia.ViewModels;

public class AlertasViewModel : ViewModelBase
{
    private readonly InventarioService _inventarioService = new();

    public ObservableCollection<InventarioItem> Alertas { get; } = new();

    private int _totalAlertas;
    public int TotalAlertas
    {
        get => _totalAlertas;
        set
        {
            if (SetField(ref _totalAlertas, value))
                OnPropertyChanged(nameof(TieneAlertas));
        }
    }

    /// <summary>Controla el distintivo numérico de la barra lateral.</summary>
    public bool TieneAlertas => TotalAlertas > 0;

    public int TotalVencidos => Alertas.Count(a => a.EstadoEsVencido);
    public int TotalPorVencer => Alertas.Count(a => a.EstadoEsPorVencer);

    private string _mensaje = string.Empty;
    public string Mensaje
    {
        get => _mensaje;
        set => SetField(ref _mensaje, value);
    }

    public ICommand CargarCommand { get; }

    public AlertasViewModel()
    {
        CargarCommand = new AsyncRelayCommand(_ => CargarAsync(),
            onError: ex => Mensaje = "Error cargando las alertas: " + ex.Message);

        // La carga inicial pasa por el comando para que los errores de conexión se
        // muestren en pantalla en lugar de perderse en una tarea sin observar.
        CargarCommand.Execute(null);
    }

    private async Task CargarAsync()
    {
        Mensaje = string.Empty;
        Alertas.Clear();
        var datos = await _inventarioService.ObtenerAlertasAsync();
        foreach (var item in datos)
            Alertas.Add(item);

        TotalAlertas = Alertas.Count;
        OnPropertyChanged(nameof(TotalVencidos));
        OnPropertyChanged(nameof(TotalPorVencer));

        if (TotalAlertas == 0)
            Mensaje = "Sin alertas: ningún lote está vencido ni próximo a vencer.";
    }
}
