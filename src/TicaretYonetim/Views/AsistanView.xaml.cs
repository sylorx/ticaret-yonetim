using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace TicaretYonetim.Views;

public partial class AsistanView : UserControl
{
    public AsistanView()
    {
        InitializeComponent();
    }

    private void Kopyala_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string metin && !string.IsNullOrEmpty(metin))
        {
            Clipboard.SetText(metin);
            var eski = btn.Content;
            btn.Content = "✓ Kopyalandı";
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            t.Tick += (s, _) => { btn.Content = eski; t.Stop(); };
            t.Start();
        }
    }

    private void MesajKutusu_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            e.Handled = true;
            var vm = DataContext as ViewModels.AsistanViewModel;
            if (vm?.GonderCommand.CanExecute(null) == true)
                vm.GonderCommand.Execute(null);
        }
    }
}
