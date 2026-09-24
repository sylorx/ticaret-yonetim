using System.Windows.Controls;
using System.Windows.Input;

namespace TicaretYonetim.Views;

public partial class AsistanView : UserControl
{
    public AsistanView()
    {
        InitializeComponent();
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
