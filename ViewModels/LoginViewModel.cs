using System;
using System.Threading.Tasks;
using System.Windows.Input;
using BancoAlimentos.Avalonia.Common;
using BancoAlimentos.Avalonia.Models;
using BancoAlimentos.Avalonia.Services;

namespace BancoAlimentos.Avalonia.ViewModels;

public class LoginViewModel : ViewModelBase
{
    private readonly AuthService _authService = new();

    // ---------- Iniciar sesión ----------

    private string _nombreUsuario = string.Empty;
    public string NombreUsuario
    {
        get => _nombreUsuario;
        set => SetField(ref _nombreUsuario, value);
    }

    private string _contrasena = string.Empty;
    public string Contrasena
    {
        get => _contrasena;
        set => SetField(ref _contrasena, value);
    }

    private string _mensajeError = string.Empty;
    public string MensajeError
    {
        get => _mensajeError;
        set => SetField(ref _mensajeError, value);
    }

    // ---------- Crear cuenta ----------

    private string _regNombreCompleto = string.Empty;
    public string RegNombreCompleto
    {
        get => _regNombreCompleto;
        set => SetField(ref _regNombreCompleto, value);
    }

    private string _regNombreUsuario = string.Empty;
    public string RegNombreUsuario
    {
        get => _regNombreUsuario;
        set => SetField(ref _regNombreUsuario, value);
    }

    private string _regContrasena = string.Empty;
    public string RegContrasena
    {
        get => _regContrasena;
        set => SetField(ref _regContrasena, value);
    }

    private string _regConfirmacion = string.Empty;
    public string RegConfirmacion
    {
        get => _regConfirmacion;
        set => SetField(ref _regConfirmacion, value);
    }

    private string _regMensajeError = string.Empty;
    public string RegMensajeError
    {
        get => _regMensajeError;
        set => SetField(ref _regMensajeError, value);
    }

    private string _regMensajeExito = string.Empty;
    public string RegMensajeExito
    {
        get => _regMensajeExito;
        set => SetField(ref _regMensajeExito, value);
    }

    /// <summary>0 = iniciar sesión, 1 = crear cuenta. Enlazado al TabControl.</summary>
    private int _pestanaActiva;
    public int PestanaActiva
    {
        get => _pestanaActiva;
        set => SetField(ref _pestanaActiva, value);
    }

    /// <summary>Se dispara cuando el login es exitoso, entregando el usuario autenticado.</summary>
    public event Action<Usuario>? LoginExitoso;

    public ICommand IniciarSesionCommand { get; }
    public ICommand RegistrarCommand { get; }

    public LoginViewModel()
    {
        IniciarSesionCommand = new AsyncRelayCommand(
            _ => IniciarSesionAsync(),
            onError: ex => MensajeError = "Error al conectar con la base de datos: " + ex.Message);

        RegistrarCommand = new AsyncRelayCommand(
            _ => RegistrarAsync(),
            onError: ex => RegMensajeError = ex is InvalidOperationException
                ? ex.Message
                : "Error al crear la cuenta: " + ex.Message);
    }

    private async Task IniciarSesionAsync()
    {
        MensajeError = string.Empty;

        if (string.IsNullOrWhiteSpace(NombreUsuario) || string.IsNullOrWhiteSpace(Contrasena))
        {
            MensajeError = "Ingrese usuario y contraseña.";
            return;
        }

        var usuario = await _authService.ValidarLoginAsync(NombreUsuario.Trim(), Contrasena);

        if (usuario is null)
        {
            MensajeError = "Usuario o contraseña incorrectos.";
            return;
        }

        LoginExitoso?.Invoke(usuario);
    }

    private async Task RegistrarAsync()
    {
        RegMensajeError = string.Empty;
        RegMensajeExito = string.Empty;

        if (string.IsNullOrWhiteSpace(RegNombreCompleto))
        {
            RegMensajeError = "Ingrese su nombre completo.";
            return;
        }
        if (string.IsNullOrWhiteSpace(RegNombreUsuario) || RegNombreUsuario.Trim().Length < 4)
        {
            RegMensajeError = "El nombre de usuario debe tener al menos 4 caracteres.";
            return;
        }
        if (RegContrasena.Length < 8)
        {
            RegMensajeError = "La contraseña debe tener al menos 8 caracteres.";
            return;
        }
        if (RegContrasena != RegConfirmacion)
        {
            RegMensajeError = "Las contraseñas no coinciden.";
            return;
        }

        await _authService.RegistrarUsuarioAsync(
            RegNombreCompleto.Trim(), RegNombreUsuario.Trim(), RegContrasena);

        // Deja el usuario cargado en la pestaña de login para que entre directo.
        NombreUsuario = RegNombreUsuario.Trim();
        Contrasena = string.Empty;
        MensajeError = string.Empty;

        RegNombreCompleto = string.Empty;
        RegNombreUsuario = string.Empty;
        RegContrasena = string.Empty;
        RegConfirmacion = string.Empty;
        RegMensajeExito = "Cuenta creada. Ya puede iniciar sesión.";

        PestanaActiva = 0;
    }

    /// <summary>Esc: limpia el formulario visible sin cerrar la ventana.</summary>
    public void LimpiarFormularioActivo()
    {
        if (PestanaActiva == 0)
        {
            NombreUsuario = string.Empty;
            Contrasena = string.Empty;
            MensajeError = string.Empty;
        }
        else
        {
            RegNombreCompleto = string.Empty;
            RegNombreUsuario = string.Empty;
            RegContrasena = string.Empty;
            RegConfirmacion = string.Empty;
            RegMensajeError = string.Empty;
            RegMensajeExito = string.Empty;
        }
    }

    /// <summary>Enter: envía el formulario de la pestaña visible.</summary>
    public void EnviarFormularioActivo()
    {
        var cmd = PestanaActiva == 0 ? IniciarSesionCommand : RegistrarCommand;
        if (cmd.CanExecute(null)) cmd.Execute(null);
    }
}
