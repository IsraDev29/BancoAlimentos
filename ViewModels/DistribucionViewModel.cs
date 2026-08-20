using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BancoAlimentos.Avalonia.Common;
using BancoAlimentos.Avalonia.Models;
using BancoAlimentos.Avalonia.Services;

namespace BancoAlimentos.Avalonia.ViewModels;

public class DistribucionViewModel : ViewModelBase
{
    private readonly CatalogoService _catalogoService = new();
    private readonly InventarioService _inventarioService = new();
    private readonly DistribucionService _distribucionService = new();
    private readonly int _idUsuarioActual;

    public ObservableCollection<Beneficiario> Beneficiarios { get; } = new();
    public ObservableCollection<InventarioItem> LotesDisponibles { get; } = new();

    private Beneficiario? _beneficiarioSeleccionado;
    public Beneficiario? BeneficiarioSeleccionado
    {
        get => _beneficiarioSeleccionado;
        set => SetField(ref _beneficiarioSeleccionado, value);
    }

    private string _observaciones = string.Empty;
    public string Observaciones
    {
        get => _observaciones;
        set => SetField(ref _observaciones, value);
    }

    private string _mensaje = string.Empty;
    public string Mensaje
    {
        get => _mensaje;
        set => SetField(ref _mensaje, value);
    }

    public ICommand CargarCommand { get; }
    public ICommand RegistrarEntregaCommand { get; }

    public DistribucionViewModel(int idUsuarioActual)
    {
        _idUsuarioActual = idUsuarioActual;

        CargarCommand = new AsyncRelayCommand(_ => CargarAsync(),
            onError: ex => Mensaje = "Error cargando datos: " + ex.Message);

        RegistrarEntregaCommand = new AsyncRelayCommand(_ => RegistrarEntregaAsync(),
            onError: ex => Mensaje = "Error al registrar la entrega: " + ex.Message);

        // La carga inicial pasa por el comando para que los errores de conexión se
        // muestren en pantalla en lugar de perderse en una tarea sin observar.
        CargarCommand.Execute(null);
    }

    /// <summary>Se dispara al registrar una entrega, para que inventario y alertas se refresquen.</summary>
    public event Action? DistribucionRegistrada;

    private async Task CargarAsync()
    {
        Beneficiarios.Clear();
        foreach (var b in await _catalogoService.ObtenerBeneficiariosAsync())
            Beneficiarios.Add(b);

        LotesDisponibles.Clear();
        var inventario = await _inventarioService.ObtenerInventarioAsync();

        // Un banco de alimentos no debe entregar producto vencido: esos lotes se
        // gestionan desde la pestaña de alertas, no desde aquí.
        foreach (var item in inventario.Where(i => i.Estado != "Vencido"))
            LotesDisponibles.Add(item);

        var vencidos = inventario.Count(i => i.Estado == "Vencido");
        if (vencidos > 0)
            Mensaje = $"Se ocultaron {vencidos} lote(s) vencido(s); revíselos en la pestaña «Alertas de vencimiento».";
    }

    private async Task RegistrarEntregaAsync()
    {
        Mensaje = string.Empty;

        if (BeneficiarioSeleccionado is null)
        {
            Mensaje = "Seleccione un beneficiario (comedor u ONG).";
            return;
        }

        var lotesSeleccionados = LotesDisponibles.Where(l => l.CantidadAEntregar > 0).ToList();
        if (lotesSeleccionados.Count == 0)
        {
            Mensaje = "Indique la cantidad a entregar de al menos un producto.";
            return;
        }

        var idGenerado = await _distribucionService.RegistrarDistribucionAsync(
            BeneficiarioSeleccionado.IdBeneficiario,
            _idUsuarioActual,
            string.IsNullOrWhiteSpace(Observaciones) ? null : Observaciones,
            new List<InventarioItem>(lotesSeleccionados));

        Observaciones = string.Empty;
        BeneficiarioSeleccionado = null;

        // Recargar inventario para reflejar el descuento hecho por el trigger de la BD
        await CargarAsync();

        // Después de CargarAsync para que el mensaje de éxito no sea sobrescrito.
        Mensaje = $"Entrega registrada correctamente (Id #{idGenerado}).";

        DistribucionRegistrada?.Invoke();
    }
}
