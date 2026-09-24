using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace TicaretYonetim.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetField<T>(ref T alan, T deger, [CallerMemberName] string? adi = null)
    {
        if (Equals(alan, deger)) return false;
        alan = deger;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(adi));
        return true;
    }

    protected void Bildir([CallerMemberName] string? adi = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(adi));
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute is null ? null : _ => canExecute())
    {
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);
}
