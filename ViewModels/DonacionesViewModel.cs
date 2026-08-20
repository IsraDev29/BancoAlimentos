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
        set
        {
            if (SetField(ref _productoParaAgregar, value))
                OnPropertyChanged(nameof(UnidadDelProducto));
        }
    }

    /// <summary>Unidad de medida que trae el alimento desde el catálogo.</summary>
    public string UnidadDelProducto => ProductoParaAgregar is null
        ? "—"
        : $"{ProductoParaAgregar.UnidadNombre} ({ProductoParaAgregar.UnidadAbreviatura})";

    // ---------- Captura del lote ----------

    public ObservableCollection<UnidadMedida> Unidades { get; } = new();

    /// <summary>
    /// Alterna entre los dos modos de captura. Al marcarlo se deshabilita el
    /// bloque de producto individual, porque los datos salen del desglose
    /// por paquete.
    /// </summary>
    private bool _esEmpaquetado;
    public bool EsEmpaquetado
    {
        get => _esEmpaquetado;
        set
        {
            if (SetField(ref _esEmpaquetado, value))
                OnPropertyChanged(nameof(EsIndividual));
        }
    }

    /// <summary>Habilita el bloque de producto individual: es el modo contrario.</summary>
    public bool EsIndividual => !EsEmpaquetado;

    // --- Producto individual ---

    private decimal _cantidadProductos = 1;
    public decimal CantidadProductos
    {
        get => _cantidadProductos;
        set => SetField(ref _cantidadProductos, value);
    }

    // --- Producto empaquetado ---

    private decimal _cantidadPaquetes = 1;
    public decimal CantidadPaquetes
    {
        get => _cantidadPaquetes;
        set => SetField(ref _cantidadPaquetes, value);
    }

    private decimal _productosPorPaquete = 1;
    public decimal ProductosPorPaquete
    {
        get => _productosPorPaquete;
        set => SetField(ref _productosPorPaquete, value);
    }

    // --- Común a los dos modos ---

    private decimal _pesoPorProducto = 1;
    public decimal PesoPorProducto
    {
        get => _pesoPorProducto;
        set => SetField(ref _pesoPorProducto, value);
    }

    private UnidadMedida? _unidadPeso;
    public UnidadMedida? UnidadPeso
    {
        get => _unidadPeso;
        set => SetField(ref _unidadPeso, value);
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

    /// <summary>Esc: descarta la línea que se está capturando, sin tocar el detalle ya agregado.</summary>
    public void LimpiarCapturaRapida()
    {
        ProductoParaAgregar = null;
        FechaVencimientoParaAgregar = new DateTimeOffset(DateTime.Today.AddMonths(1));
        EsEmpaquetado = false;
        CantidadProductos = 1;
        CantidadPaquetes = 1;
        ProductosPorPaquete = 1;
        PesoPorProducto = 1;
        Mensaje = string.Empty;
    }

    private async Task CargarCatalogosAsync()
    {
        Donantes.Clear();
        foreach (var d in await _catalogoService.ObtenerDonantesAsync())
            Donantes.Add(d);

        Productos.Clear();
        foreach (var p in await _catalogoService.ObtenerProductosAsync())
            Productos.Add(p);

        if (Unidades.Count == 0)
            foreach (var u in await _catalogoService.ObtenerUnidadesAsync())
                Unidades.Add(u);

        UnidadPeso ??= Unidades.FirstOrDefault();
    }

    private void AgregarDetalle()
    {
        Mensaje = string.Empty;

        if (ProductoParaAgregar is null)
        {
            Mensaje = "Seleccione un producto.";
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

        if (PesoPorProducto <= 0)
        {
            Mensaje = "Indique el peso o volumen de un producto (mayor a cero).";
            return;
        }
        if (UnidadPeso is null)
        {
            Mensaje = "Seleccione la unidad de medida del peso.";
            return;
        }

        // Cuántos productos individuales trae el lote, según el modo de captura.
        decimal totalProductos;

        if (EsEmpaquetado)
        {
            if (CantidadPaquetes <= 0)
            {
                Mensaje = "Indique la cantidad de paquetes (mayor a cero).";
                return;
            }
            if (ProductosPorPaquete <= 0)
            {
                Mensaje = "Indique cuántos productos trae cada paquete (mayor a cero).";
                return;
            }

            totalProductos = CantidadPaquetes * ProductosPorPaquete;
        }
        else
        {
            if (CantidadProductos <= 0)
            {
                Mensaje = "Indique la cantidad de productos (mayor a cero).";
                return;
            }

            totalProductos = CantidadProductos;
        }

        // El inventario se lleva en la unidad del alimento: si el peso está en una
        // unidad de la misma familia se convierte, si no, el total es el conteo.
        var totalLote = ConversionUnidades.CalcularTotal(
            totalProductos, PesoPorProducto,
            UnidadPeso.Abreviatura, ProductoParaAgregar.UnidadAbreviatura);

        DetalleDonacion.Add(new DetalleDonacionInput
        {
            ProductoSeleccionado = ProductoParaAgregar,
            Cantidad = totalLote,
            FechaVencimiento = fechaVencimiento,
            EsEmpaquetado = EsEmpaquetado,
            CantidadProductos = totalProductos,
            PesoPorProducto = PesoPorProducto,
            UnidadPeso = UnidadPeso,
            CantidadPaquetes = EsEmpaquetado ? CantidadPaquetes : null,
            ProductosPorPaquete = EsEmpaquetado ? ProductosPorPaquete : null
        });

        Mensaje = $"Agregado: {totalProductos:0.##} producto(s), total {totalLote:0.##} " +
                  $"{ProductoParaAgregar.UnidadAbreviatura}.";

        // reset de la captura
        CantidadProductos = 1;
        CantidadPaquetes = 1;
        ProductosPorPaquete = 1;
        EsEmpaquetado = false;
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
