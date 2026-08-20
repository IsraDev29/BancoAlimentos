using System;
using System.Windows.Input;
using BancoAlimentos.Avalonia.Common;
using BancoAlimentos.Avalonia.Models;

namespace BancoAlimentos.Avalonia.ViewModels;

public class MainViewModel : ViewModelBase
{
    /// <summary>Módulos de la barra lateral, en el orden en que se muestran.</summary>
    public enum Seccion
    {
        Inicio = 0,
        Donaciones = 1,
        Beneficiarios = 2,
        Donantes = 3,
        Inventario = 4,
        Reportes = 5,
        Distribucion = 6,
        Configuracion = 7
    }

    public Usuario UsuarioActual { get; }

    public string NombreUsuario => UsuarioActual.NombreCompleto;
    public string RolUsuario => UsuarioActual.NombreRol;
    public string PrimerNombre =>
        UsuarioActual.NombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries) is { Length: > 0 } p
            ? p[0]
            : UsuarioActual.NombreUsuario;

    /// <summary>Iniciales para el avatar de la barra lateral.</summary>
    public string InicialesUsuario
    {
        get
        {
            var partes = UsuarioActual.NombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 0) return "?";
            if (partes.Length == 1) return partes[0][..1].ToUpperInvariant();
            return (partes[0][..1] + partes[^1][..1]).ToUpperInvariant();
        }
    }

    public DonacionesViewModel DonacionesVm { get; }
    public InventarioViewModel InventarioVm { get; }
    public DistribucionViewModel DistribucionVm { get; }
    public AlertasViewModel AlertasVm { get; }
    public DonantesViewModel DonantesVm { get; }
    public BeneficiariosViewModel BeneficiariosVm { get; }

    private Seccion _seccionActual = Seccion.Inicio;
    public Seccion SeccionActual
    {
        get => _seccionActual;
        set
        {
            if (!SetField(ref _seccionActual, value)) return;

            OnPropertyChanged(nameof(EsInicio));
            OnPropertyChanged(nameof(EsDonaciones));
            OnPropertyChanged(nameof(EsBeneficiarios));
            OnPropertyChanged(nameof(EsDonantes));
            OnPropertyChanged(nameof(EsInventario));
            OnPropertyChanged(nameof(EsReportes));
            OnPropertyChanged(nameof(EsDistribucion));
            OnPropertyChanged(nameof(EsConfiguracion));
            OnPropertyChanged(nameof(TituloSeccion));
            OnPropertyChanged(nameof(DescripcionSeccion));
        }
    }

    public bool EsInicio => SeccionActual == Seccion.Inicio;
    public bool EsDonaciones => SeccionActual == Seccion.Donaciones;
    public bool EsBeneficiarios => SeccionActual == Seccion.Beneficiarios;
    public bool EsDonantes => SeccionActual == Seccion.Donantes;
    public bool EsInventario => SeccionActual == Seccion.Inventario;
    public bool EsReportes => SeccionActual == Seccion.Reportes;
    public bool EsDistribucion => SeccionActual == Seccion.Distribucion;
    public bool EsConfiguracion => SeccionActual == Seccion.Configuracion;

    public string TituloSeccion => SeccionActual switch
    {
        Seccion.Inicio => "Inicio",
        Seccion.Donaciones => "Registro de donaciones",
        Seccion.Beneficiarios => "Beneficiarios",
        Seccion.Donantes => "Donantes",
        Seccion.Inventario => "Inventario",
        Seccion.Reportes => "Reportes",
        Seccion.Distribucion => "Distribución a beneficiarios",
        _ => "Configuración"
    };

    public string DescripcionSeccion => SeccionActual switch
    {
        Seccion.Inicio => "Resumen del estado del banco de alimentos.",
        Seccion.Donaciones => "Registra el ingreso de alimentos y su fecha de vencimiento por lote.",
        Seccion.Beneficiarios => "Comedores comunitarios y ONG que reciben las entregas.",
        Seccion.Donantes => "Personas y empresas que donan alimentos.",
        Seccion.Inventario => "Existencias disponibles por lote, con filtro por producto, donante y estado.",
        Seccion.Reportes => "Reportes para donantes y entes fiscalizadores.",
        Seccion.Distribucion => "Entrega lotes a comedores comunitarios y ONG; el stock se descuenta al confirmar.",
        _ => "Conexión, cuenta y preferencias de la aplicación."
    };

    public ICommand SeleccionarSeccionCommand { get; }
    public ICommand CerrarSesionCommand { get; }

    /// <summary>Lo atiende MainWindow: vuelve a la pantalla de acceso.</summary>
    public event Action? CerrarSesionSolicitado;

    public MainViewModel(Usuario usuarioActual)
    {
        UsuarioActual = usuarioActual;

        DonacionesVm = new DonacionesViewModel(usuarioActual.IdUsuario);
        InventarioVm = new InventarioViewModel();
        DistribucionVm = new DistribucionViewModel(usuarioActual.IdUsuario);
        AlertasVm = new AlertasViewModel();
        DonantesVm = new DonantesViewModel();
        BeneficiariosVm = new BeneficiariosViewModel();

        SeleccionarSeccionCommand = new RelayCommand(p =>
        {
            if (p is Seccion s) SeccionActual = s;
            else if (p is not null && int.TryParse(p.ToString(), out var i) &&
                     Enum.IsDefined(typeof(Seccion), i)) SeccionActual = (Seccion)i;
        });

        CerrarSesionCommand = new RelayCommand(_ => CerrarSesionSolicitado?.Invoke());

        // Sin esto los módulos quedaban desactualizados: al registrar una donación,
        // inventario y alertas seguían mostrando los datos previos.
        DonacionesVm.DonacionRegistrada += RefrescarInventarioYAlertas;
        DistribucionVm.DistribucionRegistrada += RefrescarInventarioYAlertas;

        // Al tocar un catálogo hay que refrescar los combos que lo usan.
        DonantesVm.CatalogoModificado += () => DonacionesVm.CargarCatalogosCommand.Execute(null);
        BeneficiariosVm.CatalogoModificado += () => DistribucionVm.CargarCommand.Execute(null);
        InventarioVm.ProductosVm.CatalogoModificado += () => DonacionesVm.CargarCatalogosCommand.Execute(null);
    }

    private void RefrescarInventarioYAlertas()
    {
        InventarioVm.CargarCommand.Execute(null);
        AlertasVm.CargarCommand.Execute(null);
    }

    /// <summary>F5: recarga los datos del módulo visible.</summary>
    public void RecargarSeccionActual()
    {
        switch (SeccionActual)
        {
            case Seccion.Inicio:
                InventarioVm.CargarCommand.Execute(null);
                AlertasVm.CargarCommand.Execute(null);
                break;
            case Seccion.Donaciones: DonacionesVm.CargarCatalogosCommand.Execute(null); break;
            case Seccion.Beneficiarios: BeneficiariosVm.CargarCommand.Execute(null); break;
            case Seccion.Donantes: DonantesVm.CargarCommand.Execute(null); break;
            case Seccion.Inventario: InventarioVm.CargarCommand.Execute(null); break;
            case Seccion.Distribucion: DistribucionVm.CargarCommand.Execute(null); break;
        }
    }

    /// <summary>Esc: descarta el mensaje de estado del módulo visible.</summary>
    public void LimpiarMensajes()
    {
        DonacionesVm.Mensaje = string.Empty;
        InventarioVm.Mensaje = string.Empty;
        DistribucionVm.Mensaje = string.Empty;
        AlertasVm.Mensaje = string.Empty;
        DonantesVm.Mensaje = string.Empty;
        BeneficiariosVm.Mensaje = string.Empty;
    }
}
