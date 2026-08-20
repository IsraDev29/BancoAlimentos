using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Input;

namespace BancoAlimentos.Avalonia.Views;

/// <summary>
/// Contrato mínimo que necesitan los módulos con CRUD para responder al teclado.
/// </summary>
public interface IEditorCrud
{
    bool EditorVisible { get; }
    ICommand NuevoCommand { get; }
    ICommand EditarCommand { get; }
    ICommand GuardarCommand { get; }
    void CerrarEditor();
}

/// <summary>
/// Teclado compartido por los módulos con CRUD, para no repetirlo en cada vista:
/// Enter guarda, Esc cancela, Insert crea y F2 edita.
/// </summary>
public static class TeclasCrud
{
    public static void Manejar(KeyEventArgs e, IEditorCrud? vm)
    {
        if (vm is null) return;

        // En un campo multilínea Enter debe escribir, no guardar.
        if (e.Key == Key.Enter && e.Source is TextBox { AcceptsReturn: true }) return;

        switch (e.Key)
        {
            case Key.Enter when vm.EditorVisible:
                if (vm.GuardarCommand.CanExecute(null)) vm.GuardarCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.Escape when vm.EditorVisible:
                vm.CerrarEditor();
                e.Handled = true;
                break;

            case Key.Insert when !vm.EditorVisible:
                if (vm.NuevoCommand.CanExecute(null)) vm.NuevoCommand.Execute(null);
                e.Handled = true;
                break;

            case Key.F2 when !vm.EditorVisible:
                if (vm.EditarCommand.CanExecute(null)) vm.EditarCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }
}
