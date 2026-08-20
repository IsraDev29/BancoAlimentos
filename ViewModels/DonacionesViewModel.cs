using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using BancoAlimentos.Avalonia.Common;
using BancoAlimentos.Avalonia.Models;
using BancoAlimentos.Avalonia.Services;

namespace BancoAlimentos.Avalonia.ViewModels;

public class DonacionesViewModel : ViewModelBase
{
    private readonly CatalogoService _catalogoService = new();
    private readonly DonacionService _donacionService = new();
    private readonly int _idUsuarioActual;

    public ObservableCollection<Donante> Donantes { get; } = new();
    public ObservableCollection<Producto> Productos { get; } = new();
    public ObservableCollection<DetalleDonacionInput> DetalleDonacion { get; } = new();

    private Donante? _donanteSeleccionado;
    public Donante? DonanteSeleccionado
    {
        get => _donanteSeleccionado;
        set => SetField(ref _donanteSeleccionado, value);
    }

    private string _observaciones = string.Empty;
    public string Observaciones
    {
        get => _observaciones;
        set => SetField(ref _observaciones, value);
    }

    private Producto? _productoParaAgregar;
    public Producto? ProductoParaAgregar
    {
        get => _productoParaAgregar;
        set => SetField(ref _productoParaAgregar, value);
    }

    private decimal _cantidadParaAgregar = 1;
    public decimal CantidadParaAgregar
    {
        get => _cantidadParaAgregar;
        set => SetField(ref _cantidadParaAgregar, value);
    }

    // DatePicker.SelectedDate es DateTimeOffset?. Con DateTime el binding falla en ambos
    // sentidos (Avalonia no convierte entre DateTime y DateTimeOffset) y la fecha elegida
    // por el usuario nunca llegaba al ViewModel.
    private DateTimeOffset? _fechaVencimientoParaAgregar = new DateTimeOffset(DateTime.Today.AddMonths(1));
    public DateTimeOffset? FechaVencimientoParaAgregar
    {
        get => _fechaVencimientoParaAgregar;
        set => SetField(ref _fechaVencimientoParaAgregar, value);
    }

    private string _mensaje = string.Empty;
    public string Mensaje
    {
        get => _mensaje;
        set => SetField(ref _mensaje, value);
    }

    private DetalleDonacionInput? _detalleSeleccionado;
    public DetalleDonacionInput? DetalleSeleccionado
    {
        get => _detalleSeleccionado;
        set => SetField(ref _detalleSeleccionado, value);
    }

    public ICommand CargarCatalogosCommand { get; }
    public ICommand AgregarDetalleCommand { get; }
    public ICommand QuitarDetalleSeleccionadoCommand { get; }
    public ICommand GuardarDonacionCommand { get; }

    public DonacionesViewModel(int idUsuarioActual)
    {
        _idUsuarioActual = idUsuarioActual;

        CargarCatalogosCommand = new AsyncRelayCommand(_ => CargarCatalogosAsync(),
            onError: ex => Mensaje = "Error cargando catálogos: " + ex.Message);

        AgregarDetalleCommand = new RelayCommand(_ => AgregarDetalle());

        QuitarDetalleSeleccionadoCommand = new RelayCommand(_ =>
        {
            if (DetalleSeleccionado is not null)
                DetalleDonacion.Remove(DetalleSeleccionado);
        });

        GuardarDonacionCommand = new AsyncRelayCommand(_ => GuardarDonacionAsync(),
            onError: ex => Mensaje = "Error al registrar la donación: " + ex.Message);

        // La carga inicial pasa por el comando para que los errores de conexión
        // se muestren en pantalla en lugar de perderse en una tarea sin observar.
        CargarCatalogosCommand.Execute(null);
    }

    /// <summary>Se dispara al registrar una donación, para que inventario y alertas se refresquen.</summary>
    public event Action? DonacionRegistrada;

    private async Task CargarCatalogosAsync()
    {
        Donantes.Clear();
        foreach (var d in await _catalogoService.ObtenerDonantesAsync())
            Donantes.Add(d);

        Productos.Clear();
        foreach (var p in await _catalogoService.ObtenerProductosAsync())
            Productos.Add(p);
    }

    private void AgregarDetalle()
    {
        Mensaje = string.Empty;

        if (ProductoParaAgregar is null)
        {
            Mensaje = "Seleccione un producto.";
            return;
        }
        if (CantidadParaAgregar <= 0)
        {
            Mensaje = "La cantidad debe ser mayor a cero.";
            return;
        }
        if (FechaVencimientoParaAgregar is null)
        {
            Mensaje = "Indique la fecha de vencimiento del producto.";
            return;
        }

        var fechaVencimiento = FechaVencimientoParaAgregar.Value.Date;
        if (fechaVencimiento < DateTime.Today)
        {
            Mensaje = "La fecha de vencimiento ya pasó: un producto vencido no puede ingresar al inventario.";
            return;
        }

        DetalleDonacion.Add(new DetalleDonacionInput
        {
            ProductoSeleccionado = ProductoParaAgregar,
            Cantidad = CantidadParaAgregar,
            FechaVencimiento = fechaVencimiento
        });

        // reset de la línea rápida de captura
        CantidadParaAgregar = 1;
    }

    private async Task GuardarDonacionAsync()
    {
        Mensaje = string.Empty;

        if (DonanteSeleccionado is null)
        {
            Mensaje = "Seleccione un donante.";
            return;
        }
        if (DetalleDonacion.Count == 0)
        {
            Mensaje = "Agregue al menos un producto a la donación.";
            return;
        }

        var idGenerado = await _donacionService.RegistrarDonacionAsync(
            DonanteSeleccionado.IdDonante,
            _idUsuarioActual,
            string.IsNullOrWhiteSpace(Observaciones) ? null : Observaciones,
            new List<DetalleDonacionInput>(DetalleDonacion));

        Mensaje = $"Donación registrada correctamente (Id #{idGenerado}).";
        DetalleDonacion.Clear();
        DetalleSeleccionado = null;
        Observaciones = string.Empty;
        DonanteSeleccionado = null;

        DonacionRegistrada?.Invoke();
    }
}
