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

    /// <summary>Se dispara cuando el login es exitoso, entregando el usuario autenticado.</summary>
    public event Action<Usuario>? LoginExitoso;

    public ICommand IniciarSesionCommand { get; }

    public LoginViewModel()
    {
        IniciarSesionCommand = new AsyncRelayCommand(
            _ => IniciarSesionAsync(),
            onError: ex => MensajeError = "Error al conectar con la base de datos: " + ex.Message);
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
}
