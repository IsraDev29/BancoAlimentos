using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BancoAlimentos.Avalonia.Common;
using BancoAlimentos.Avalonia.Models;
using BancoAlimentos.Avalonia.Services;

namespace BancoAlimentos.Avalonia.ViewModels;

/// <summary>
/// CRUD del catálogo de alimentos (dbo.Producto). Cada alimento define su
/// unidad de medida y a cuántos días antes del vencimiento debe alertar.
/// </summary>
public class ProductosViewModel : ViewModelBase, Views.IEditorCrud
{
    private readonly CatalogoService _catalogo = new();

    public ObservableCollection<Producto> Productos { get; } = new();
    public ObservableCollection<CategoriaProducto> Categorias { get; } = new();
    public ObservableCollection<UnidadMedida> Unidades { get; } = new();

    private Producto? _seleccionado;
    public Producto? Seleccionado
    {
        get => _seleccionado;
        set
        {
            if (SetField(ref _seleccionado, value))
                OnPropertyChanged(nameof(HaySeleccion));
        }
    }

    public bool HaySeleccion => Seleccionado is not null;

    private bool _mostrarInactivos;
    public bool MostrarInactivos
    {
        get => _mostrarInactivos;
        set { if (SetField(ref _mostrarInactivos, value)) CargarCommand.Execute(null); }
    }

    private string _mensaje = string.Empty;
    public string Mensaje
    {
        get => _mensaje;
        set => SetField(ref _mensaje, value);
    }

    public int TotalAlimentos => Productos.Count;

    // ---------- Editor ----------

    private bool _editorVisible;
    public bool EditorVisible
    {
        get => _editorVisible;
        set => SetField(ref _editorVisible, value);
    }

    private string _tituloEditor = string.Empty;
    public string TituloEditor
    {
        get => _tituloEditor;
        set => SetField(ref _tituloEditor, value);
    }

    private int _edId;

    private string _edNombre = string.Empty;
    public string EdNombre
    {
        get => _edNombre;
        set => SetField(ref _edNombre, value);
    }

    private CategoriaProducto? _edCategoria;
    public CategoriaProducto? EdCategoria
    {
        get => _edCategoria;
        set => SetField(ref _edCategoria, value);
    }

    private UnidadMedida? _edUnidad;
    public UnidadMedida? EdUnidad
    {
        get => _edUnidad;
        set => SetField(ref _edUnidad, value);
    }

    private decimal _edDiasAlerta = 15;
    public decimal EdDiasAlerta
    {
        get => _edDiasAlerta;
        set => SetField(ref _edDiasAlerta, value);
    }

    private bool _edActivo = true;
    public bool EdActivo
    {
        get => _edActivo;
        set => SetField(ref _edActivo, value);
    }

    public ICommand CargarCommand { get; }
    public ICommand NuevoCommand { get; }
    public ICommand EditarCommand { get; }
    public ICommand GuardarCommand { get; }
    public ICommand CancelarCommand { get; }
    public ICommand EliminarCommand { get; }

    public event Action? CatalogoModificado;

    public ProductosViewModel()
    {
        CargarCommand = new AsyncRelayCommand(_ => CargarAsync(),
            onError: ex => Mensaje = "Error cargando alimentos: " + ex.Message);

        NuevoCommand = new RelayCommand(_ => AbrirEditorNuevo());
        EditarCommand = new RelayCommand(_ => AbrirEditorEdicion());
        CancelarCommand = new RelayCommand(_ => CerrarEditor());

        GuardarCommand = new AsyncRelayCommand(_ => GuardarAsync(),
            onError: ex => Mensaje = ex is InvalidOperationException
                ? ex.Message : "Error al guardar: " + ex.Message);

        EliminarCommand = new AsyncRelayCommand(_ => EliminarAsync(),
            onError: ex => Mensaje = ex is InvalidOperationException
                ? ex.Message : "Error al eliminar: " + ex.Message);

        CargarCommand.Execute(null);
    }

    private async Task CargarAsync()
    {
        Mensaje = string.Empty;

        if (Categorias.Count == 0)
            foreach (var c in await _catalogo.ObtenerCategoriasAsync())
                Categorias.Add(c);

        if (Unidades.Count == 0)
            foreach (var u in await _catalogo.ObtenerUnidadesAsync())
                Unidades.Add(u);

        var idPrevio = Seleccionado?.IdProducto;

        Productos.Clear();
        foreach (var p in await _catalogo.ObtenerProductosAsync(MostrarInactivos))
            Productos.Add(p);

        Seleccionado = Productos.FirstOrDefault(p => p.IdProducto == idPrevio);
        OnPropertyChanged(nameof(TotalAlimentos));

        if (Productos.Count == 0)
            Mensaje = "No hay alimentos en el catálogo. Use «Nuevo alimento» para agregar el primero.";
    }

    private void AbrirEditorNuevo()
    {
        Mensaje = string.Empty;
        TituloEditor = "Nuevo alimento";
        _edId = 0;
        EdNombre = string.Empty;
        EdCategoria = Categorias.FirstOrDefault();
        EdUnidad = Unidades.FirstOrDefault();
        EdDiasAlerta = 15;
        EdActivo = true;
        EditorVisible = true;
    }

    private void AbrirEditorEdicion()
    {
        if (Seleccionado is null)
        {
            Mensaje = "Seleccione un alimento de la tabla para editarlo.";
            return;
        }

        Mensaje = string.Empty;
        TituloEditor = "Editar alimento";
        _edId = Seleccionado.IdProducto;
        EdNombre = Seleccionado.Nombre;
        EdCategoria = Categorias.FirstOrDefault(c => c.IdCategoria == Seleccionado.IdCategoria);
        EdUnidad = Unidades.FirstOrDefault(u => u.IdUnidad == Seleccionado.IdUnidad);
        EdDiasAlerta = Seleccionado.DiasAlertaVencimiento;
        EdActivo = Seleccionado.Activo;
        EditorVisible = true;
    }

    public void CerrarEditor()
    {
        EditorVisible = false;
        Mensaje = string.Empty;
    }

    private async Task GuardarAsync()
    {
        Mensaje = string.Empty;

        if (string.IsNullOrWhiteSpace(EdNombre))
        {
            Mensaje = "El nombre del alimento es obligatorio.";
            return;
        }
        if (EdCategoria is null)
        {
            Mensaje = "Seleccione la categoría.";
            return;
        }
        if (EdUnidad is null)
        {
            Mensaje = "Seleccione la unidad de medida.";
            return;
        }
        if (EdDiasAlerta < 0)
        {
            Mensaje = "Los días de alerta no pueden ser negativos.";
            return;
        }

        var producto = new Producto
        {
            IdProducto = _edId,
            Nombre = EdNombre.Trim(),
            IdCategoria = EdCategoria.IdCategoria,
            IdUnidad = EdUnidad.IdUnidad,
            DiasAlertaVencimiento = (int)EdDiasAlerta,
            Activo = EdActivo
        };

        var id = await _catalogo.GuardarProductoAsync(producto);
        var eraNuevo = _edId == 0;

        EditorVisible = false;
        await CargarAsync();

        Seleccionado = Productos.FirstOrDefault(p => p.IdProducto == id);
        Mensaje = eraNuevo
            ? $"Alimento «{producto.Nombre}» agregado al catálogo."
            : $"Alimento «{producto.Nombre}» actualizado.";

        CatalogoModificado?.Invoke();
    }

    private async Task EliminarAsync()
    {
        if (Seleccionado is null)
        {
            Mensaje = "Seleccione un alimento de la tabla para eliminarlo.";
            return;
        }

        var nombre = Seleccionado.Nombre;
        var borrado = await _catalogo.EliminarProductoAsync(Seleccionado.IdProducto);

        await CargarAsync();

        Mensaje = borrado
            ? $"Alimento «{nombre}» eliminado del catálogo."
            : $"«{nombre}» ya fue donado alguna vez, así que se desactivó en lugar de borrarse " +
              "(borrarlo rompería la trazabilidad de esas donaciones).";

        CatalogoModificado?.Invoke();
    }
}
