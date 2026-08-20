using Avalonia;
using System;

namespace BancoAlimentos.Avalonia;

class Program
{
    // El punto de entrada NO debe usar ningún tipo de Avalonia antes de AppMain
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
