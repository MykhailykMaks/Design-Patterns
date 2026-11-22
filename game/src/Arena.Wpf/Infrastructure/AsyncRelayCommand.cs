using System.Windows.Input;

namespace Arena.Wpf.Infrastructure;

public sealed class AsyncRelayCommand(
    Func<object?, Task> execute, Action<Exception> onError, Predicate<object?>? canExecute = null) : ICommand
{
    private bool _running;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => !_running && (canExecute?.Invoke(parameter) ?? true);
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) { return; }
        _running = true;
        Refresh();
        try { await execute(parameter); }
        catch (Exception exception) { onError(exception); }
        finally { _running = false; Refresh(); }
    }
    public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
