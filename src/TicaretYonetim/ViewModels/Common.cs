using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace TicaretYonetim.ViewModels;

[ValueConversion(typeof(string), typeof(BitmapImage))]
public class KucukResimConverter : IValueConverter
{
    public static readonly KucukResimConverter Instance = new();

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string yol || string.IsNullOrWhiteSpace(yol) || !File.Exists(yol))
            return null;

        try
        {
            var genislik = parameter is string s && int.TryParse(s, out var g) ? g : 280;
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bmp.DecodePixelWidth = genislik;
            bmp.UriSource = new Uri(yol, UriKind.Absolute);
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

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
