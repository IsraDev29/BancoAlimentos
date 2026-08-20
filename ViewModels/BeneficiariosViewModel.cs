using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BancoAlimentos.Avalonia.Common;
using BancoAlimentos.Avalonia.Models;
using BancoAlimentos.Avalonia.Services;

namespace BancoAlimentos.Avalonia.ViewModels;

/// <summary>CRUD de beneficiarios: comedores comunitarios, ONG y similares.</summary>
public class BeneficiariosViewModel : ViewModelBase, Views.IEditorCrud
{
    private readonly CatalogoService _catalogo = new();

    public ObservableCollection<Beneficiario> Beneficiarios { get; } = new();

    /// <summary>dbo.Beneficiario.Tipo es texto libre; estas son las opciones sugeridas.</summary>
    public string[] TiposDisponibles { get; } =
        { "Comedor Comunitario", "ONG", "Albergue", "Iglesia", "Centro escolar", "Otro" };

    private Beneficiario? _seleccionado;
    public Beneficiario? Seleccionado
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

    public int TotalBeneficiarios => Beneficiarios.Count;
    public int TotalComedores => Beneficiarios.Count(b => b.Tipo.Contains("Comedor", StringComparison.OrdinalIgnoreCase));
    public int TotalOng => Beneficiarios.Count(b => b.Tipo.Contains("ONG", StringComparison.OrdinalIgnoreCase));

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

    private string _edTipo = "Comedor Comunitario";
    public string EdTipo
    {
        get => _edTipo;
        set => SetField(ref _edTipo, value);
    }

    private string _edDireccion = string.Empty;
    public string EdDireccion
    {
        get => _edDireccion;
        set => SetField(ref _edDireccion, value);
    }

    private string _edTelefono = string.Empty;
    public string EdTelefono
    {
        get => _edTelefono;
        set => SetField(ref _edTelefono, value);
    }

    private string _edResponsable = string.Empty;
    public string EdResponsable
    {
        get => _edResponsable;
        set => SetField(ref _edResponsable, value);
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

    public BeneficiariosViewModel()
    {
        CargarCommand = new AsyncRelayCommand(_ => CargarAsync(),
            onError: ex => Mensaje = "Error cargando beneficiarios: " + ex.Message);

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
        var idPrevio = Seleccionado?.IdBeneficiario;

        Beneficiarios.Clear();
        foreach (var b in await _catalogo.ObtenerBeneficiariosAsync(MostrarInactivos))
            Beneficiarios.Add(b);

        Seleccionado = Beneficiarios.FirstOrDefault(b => b.IdBeneficiario == idPrevio);

        OnPropertyChanged(nameof(TotalBeneficiarios));
        OnPropertyChanged(nameof(TotalComedores));
        OnPropertyChanged(nameof(TotalOng));

        if (Beneficiarios.Count == 0)
            Mensaje = "No hay beneficiarios registrados. Use «Nuevo beneficiario» para agregar el primero.";
    }

    private void AbrirEditorNuevo()
    {
        Mensaje = string.Empty;
        TituloEditor = "Nuevo beneficiario";
        _edId = 0;
        EdNombre = string.Empty;
        EdTipo = TiposDisponibles[0];
        EdDireccion = string.Empty;
        EdTelefono = string.Empty;
        EdResponsable = string.Empty;
        EdActivo = true;
        EditorVisible = true;
    }

    private void AbrirEditorEdicion()
    {
        if (Seleccionado is null)
        {
            Mensaje = "Seleccione un beneficiario de la tabla para editarlo.";
            return;
        }

        Mensaje = string.Empty;
        TituloEditor = "Editar beneficiario";
        _edId = Seleccionado.IdBeneficiario;
        EdNombre = Seleccionado.Nombre;
        // Si el tipo guardado no está entre las opciones, se agrega para no perderlo.
        EdTipo = TiposDisponibles.Contains(Seleccionado.Tipo) ? Seleccionado.Tipo : TiposDisponibles[^1];
        EdDireccion = Seleccionado.Direccion ?? string.Empty;
        EdTelefono = Seleccionado.Telefono ?? string.Empty;
        EdResponsable = Seleccionado.ResponsableContacto ?? string.Empty;
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
            Mensaje = "El nombre del beneficiario es obligatorio.";
            return;
        }
        if (string.IsNullOrWhiteSpace(EdTipo))
        {
            Mensaje = "Indique el tipo de beneficiario.";
            return;
        }

        var beneficiario = new Beneficiario
        {
            IdBeneficiario = _edId,
            Nombre = EdNombre.Trim(),
            Tipo = EdTipo,
            Direccion = EdDireccion,
            Telefono = EdTelefono,
            ResponsableContacto = EdResponsable,
            Activo = EdActivo
        };

        var id = await _catalogo.GuardarBeneficiarioAsync(beneficiario);
        var eraNuevo = _edId == 0;

        EditorVisible = false;
        await CargarAsync();

        Seleccionado = Beneficiarios.FirstOrDefault(b => b.IdBeneficiario == id);
        Mensaje = eraNuevo
            ? $"Beneficiario «{beneficiario.Nombre}» agregado."
            : $"Beneficiario «{beneficiario.Nombre}» actualizado.";

        CatalogoModificado?.Invoke();
    }

    private async Task EliminarAsync()
    {
        if (Seleccionado is null)
        {
            Mensaje = "Seleccione un beneficiario de la tabla para eliminarlo.";
            return;
        }

        var nombre = Seleccionado.Nombre;
        var borrado = await _catalogo.EliminarBeneficiarioAsync(Seleccionado.IdBeneficiario);

        await CargarAsync();

        Mensaje = borrado
            ? $"Beneficiario «{nombre}» eliminado."
            : $"«{nombre}» ya recibió entregas, así que se desactivó en lugar de borrarse " +
              "(borrarlo rompería la trazabilidad de esas entregas).";

        CatalogoModificado?.Invoke();
    }
}
