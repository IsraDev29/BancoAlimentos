using System.Windows.Input;
using BancoAlimentos.Avalonia.Common;
using BancoAlimentos.Avalonia.Models;

namespace BancoAlimentos.Avalonia.ViewModels;

public class MainViewModel : ViewModelBase
{
    /// <summary>Secciones de la navegación lateral, en el orden en que se muestran.</summary>
    public enum Seccion
    {
        Donaciones = 0,
        Inventario = 1,
        Distribucion = 2,
        Alertas = 3
    }

    public Usuario UsuarioActual { get; }

    public string TituloBienvenida => $"{UsuarioActual.NombreCompleto} ({UsuarioActual.NombreRol})";
    public string NombreUsuario => UsuarioActual.NombreCompleto;
    public string RolUsuario => UsuarioActual.NombreRol;

    /// <summary>Iniciales para el avatar de la barra lateral.</summary>
    public string InicialesUsuario
    {
        get
        {
            var partes = UsuarioActual.NombreCompleto.Split(' ',
                System.StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 0) return "?";
            if (partes.Length == 1) return partes[0][..1].ToUpperInvariant();
            return (partes[0][..1] + partes[^1][..1]).ToUpperInvariant();
        }
    }

    public DonacionesViewModel DonacionesVm { get; }
    public InventarioViewModel InventarioVm { get; }
    public DistribucionViewModel DistribucionVm { get; }
    public AlertasViewModel AlertasVm { get; }

    private Seccion _seccionActual = Seccion.Donaciones;
    public Seccion SeccionActual
    {
        get => _seccionActual;
        set
        {
            if (!SetField(ref _seccionActual, value)) return;

            OnPropertyChanged(nameof(EsDonaciones));
            OnPropertyChanged(nameof(EsInventario));
            OnPropertyChanged(nameof(EsDistribucion));
            OnPropertyChanged(nameof(EsAlertas));
            OnPropertyChanged(nameof(TituloSeccion));
            OnPropertyChanged(nameof(DescripcionSeccion));
        }
    }

    public bool EsDonaciones => SeccionActual == Seccion.Donaciones;
    public bool EsInventario => SeccionActual == Seccion.Inventario;
    public bool EsDistribucion => SeccionActual == Seccion.Distribucion;
    public bool EsAlertas => SeccionActual == Seccion.Alertas;

    public string TituloSeccion => SeccionActual switch
    {
        Seccion.Donaciones => "Registro de donaciones",
        Seccion.Inventario => "Inventario",
        Seccion.Distribucion => "Distribución a beneficiarios",
        _ => "Alertas de vencimiento"
    };

    public string DescripcionSeccion => SeccionActual switch
    {
        Seccion.Donaciones => "Registra el ingreso de alimentos y su fecha de vencimiento por lote.",
        Seccion.Inventario => "Existencias disponibles por lote, con filtro por producto, donante y estado.",
        Seccion.Distribucion => "Entrega lotes a comedores comunitarios y ONG; el stock se descuenta al confirmar.",
        _ => "Lotes vencidos o próximos a vencer que requieren atención."
    };

    public ICommand SeleccionarSeccionCommand { get; }

    public MainViewModel(Usuario usuarioActual)
    {
        UsuarioActual = usuarioActual;
        DonacionesVm = new DonacionesViewModel(usuarioActual.IdUsuario);
        InventarioVm = new InventarioViewModel();
        DistribucionVm = new DistribucionViewModel(usuarioActual.IdUsuario);
        AlertasVm = new AlertasViewModel();

        SeleccionarSeccionCommand = new RelayCommand(p =>
        {
            if (p is Seccion s) SeccionActual = s;
            else if (p is not null && int.TryParse(p.ToString(), out var i)) SeccionActual = (Seccion)i;
        });

        // Sin esto las secciones quedaban desactualizadas: al registrar una donación,
        // inventario y alertas seguían mostrando los datos previos hasta pulsar «Actualizar».
        DonacionesVm.DonacionRegistrada += RefrescarInventarioYAlertas;
        DistribucionVm.DistribucionRegistrada += RefrescarInventarioYAlertas;
    }

    private void RefrescarInventarioYAlertas()
    {
        InventarioVm.CargarCommand.Execute(null);
        AlertasVm.CargarCommand.Execute(null);
    }
}
