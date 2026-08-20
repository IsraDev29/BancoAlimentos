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
/// CRUD de donantes. Hay dos tipos, Empresa y Particular, que vienen del
/// catálogo dbo.TipoDonante.
/// </summary>
public class DonantesViewModel : ViewModelBase, Views.IEditorCrud
{
    private readonly CatalogoService _catalogo = new();

    public ObservableCollection<Donante> Donantes { get; } = new();
    public ObservableCollection<TipoDonante> TiposDonante { get; } = new();

    private Donante? _seleccionado;
    public Donante? Seleccionado
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

    public int TotalDonantes => Donantes.Count;
    public int TotalEmpresas => Donantes.Count(d => d.EsEmpresa);
    public int TotalParticulares => Donantes.Count(d => !d.EsEmpresa);

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

    private TipoDonante? _edTipo;
    public TipoDonante? EdTipo
    {
        get => _edTipo;
        set => SetField(ref _edTipo, value);
    }

    private string _edTelefono = string.Empty;
    public string EdTelefono
    {
        get => _edTelefono;
        set => SetField(ref _edTelefono, value);
    }

    private string _edCorreo = string.Empty;
    public string EdCorreo
    {
        get => _edCorreo;
        set => SetField(ref _edCorreo, value);
    }

    private string _edDireccion = string.Empty;
    public string EdDireccion
    {
        get => _edDireccion;
        set => SetField(ref _edDireccion, value);
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

    /// <summary>Avisa a los demás módulos de que la lista de donantes cambió.</summary>
    public event Action? CatalogoModificado;

    public DonantesViewModel()
    {
        CargarCommand = new AsyncRelayCommand(_ => CargarAsync(),
            onError: ex => Mensaje = "Error cargando donantes: " + ex.Message);

        NuevoCommand = new RelayCommand(_ => AbrirEditorNuevo());
        EditarCommand = new RelayCommand(_ => AbrirEditorEdicion());
        CancelarCommand = new RelayCommand(_ => CerrarEditor());

        GuardarCommand = new AsyncRelayCommand(_ => GuardarAsync(),
            onError: ex => Mensaje = ex is InvalidOperationException
                ? ex.Message
                : "Error al guardar: " + ex.Message);

        EliminarCommand = new AsyncRelayCommand(_ => EliminarAsync(),
            onError: ex => Mensaje = ex is InvalidOperationException
                ? ex.Message
                : "Error al eliminar: " + ex.Message);

        CargarCommand.Execute(null);
    }

    private async Task CargarAsync()
    {
        Mensaje = string.Empty;

        if (TiposDonante.Count == 0)
            foreach (var t in await _catalogo.ObtenerTiposDonanteAsync())
                TiposDonante.Add(t);

        var idPrevio = Seleccionado?.IdDonante;

        Donantes.Clear();
        foreach (var d in await _catalogo.ObtenerDonantesAsync(MostrarInactivos))
            Donantes.Add(d);

        Seleccionado = Donantes.FirstOrDefault(d => d.IdDonante == idPrevio);

        OnPropertyChanged(nameof(TotalDonantes));
        OnPropertyChanged(nameof(TotalEmpresas));
        OnPropertyChanged(nameof(TotalParticulares));

        if (Donantes.Count == 0)
            Mensaje = "No hay donantes registrados. Use «Nuevo donante» para agregar el primero.";
    }

    private void AbrirEditorNuevo()
    {
        Mensaje = string.Empty;
        TituloEditor = "Nuevo donante";
        _edId = 0;
        EdNombre = string.Empty;
        EdTipo = TiposDonante.FirstOrDefault();
        EdTelefono = string.Empty;
        EdCorreo = string.Empty;
        EdDireccion = string.Empty;
        EdActivo = true;
        EditorVisible = true;
    }

    private void AbrirEditorEdicion()
    {
        if (Seleccionado is null)
        {
            Mensaje = "Seleccione un donante de la tabla para editarlo.";
            return;
        }

        Mensaje = string.Empty;
        TituloEditor = "Editar donante";
        _edId = Seleccionado.IdDonante;
        EdNombre = Seleccionado.Nombre;
        EdTipo = TiposDonante.FirstOrDefault(t => t.IdTipoDonante == Seleccionado.IdTipoDonante);
        EdTelefono = Seleccionado.Telefono ?? string.Empty;
        EdCorreo = Seleccionado.Correo ?? string.Empty;
        EdDireccion = Seleccionado.Direccion ?? string.Empty;
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
            Mensaje = "El nombre del donante es obligatorio.";
            return;
        }
        if (EdTipo is null)
        {
            Mensaje = "Seleccione el tipo de donante.";
            return;
        }
        if (!string.IsNullOrWhiteSpace(EdCorreo) && !EdCorreo.Contains('@'))
        {
            Mensaje = "El correo no parece válido.";
            return;
        }

        var donante = new Donante
        {
            IdDonante = _edId,
            Nombre = EdNombre.Trim(),
            IdTipoDonante = EdTipo.IdTipoDonante,
            Telefono = EdTelefono,
            Correo = EdCorreo,
            Direccion = EdDireccion,
            Activo = EdActivo
        };

        var id = await _catalogo.GuardarDonanteAsync(donante);
        var eraNuevo = _edId == 0;

        EditorVisible = false;
        await CargarAsync();

        Seleccionado = Donantes.FirstOrDefault(d => d.IdDonante == id);
        Mensaje = eraNuevo
            ? $"Donante «{donante.Nombre}» agregado."
            : $"Donante «{donante.Nombre}» actualizado.";

        CatalogoModificado?.Invoke();
    }

    private async Task EliminarAsync()
    {
        if (Seleccionado is null)
        {
            Mensaje = "Seleccione un donante de la tabla para eliminarlo.";
            return;
        }

        var nombre = Seleccionado.Nombre;
        var borrado = await _catalogo.EliminarDonanteAsync(Seleccionado.IdDonante);

        await CargarAsync();

        Mensaje = borrado
            ? $"Donante «{nombre}» eliminado."
            : $"«{nombre}» tiene donaciones registradas, así que se desactivó en lugar de borrarse " +
              "(borrarlo rompería la trazabilidad de esas donaciones).";

        CatalogoModificado?.Invoke();
    }
}
