using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BancoAlimentos.Avalonia.Common;
using BancoAlimentos.Avalonia.Models;
using BancoAlimentos.Avalonia.Services;

namespace BancoAlimentos.Avalonia.ViewModels;

public class InventarioViewModel : ViewModelBase
{
    private readonly InventarioService _inventarioService = new();
    private ObservableCollection<InventarioItem> _todosLosItems = new();

    public ObservableCollection<InventarioItem> Items { get; } = new();

    private string _filtroTexto = string.Empty;
    public string FiltroTexto
    {
        get => _filtroTexto;
        set
        {
            if (SetField(ref _filtroTexto, value))
                AplicarFiltro();
        }
    }

    public string[] EstadosDisponibles { get; } = { "Todos", "Vigente", "Por vencer", "Vencido" };

    private string _estadoSeleccionado = "Todos";
    public string EstadoSeleccionado
    {
        get => _estadoSeleccionado;
        set
        {
            if (SetField(ref _estadoSeleccionado, value))
                AplicarFiltro();
        }
    }

    private string _mensaje = string.Empty;
    public string Mensaje
    {
        get => _mensaje;
        set => SetField(ref _mensaje, value);
    }

    public ICommand CargarCommand { get; }

    public InventarioViewModel()
    {
        CargarCommand = new AsyncRelayCommand(_ => CargarAsync(),
            onError: ex => Mensaje = "Error cargando el inventario: " + ex.Message);

        // La carga inicial pasa por el comando para que los errores de conexión se
        // muestren en pantalla en lugar de perderse en una tarea sin observar.
        CargarCommand.Execute(null);
    }

    private async Task CargarAsync()
    {
        Mensaje = string.Empty;
        var datos = await _inventarioService.ObtenerInventarioAsync();
        _todosLosItems = new ObservableCollection<InventarioItem>(datos);
        AplicarFiltro();

        if (_todosLosItems.Count == 0)
            Mensaje = "No hay existencias registradas. Registre una donación en la pestaña «Donaciones».";
    }

    private void AplicarFiltro()
    {
        Items.Clear();

        var query = _todosLosItems.AsEnumerable();

        if (EstadoSeleccionado != "Todos")
            query = query.Where(i => i.Estado == EstadoSeleccionado);

        if (!string.IsNullOrWhiteSpace(FiltroTexto))
            query = query.Where(i =>
                i.Producto.Contains(FiltroTexto, System.StringComparison.OrdinalIgnoreCase) ||
                i.Donante.Contains(FiltroTexto, System.StringComparison.OrdinalIgnoreCase));

        foreach (var item in query)
            Items.Add(item);
    }
}
