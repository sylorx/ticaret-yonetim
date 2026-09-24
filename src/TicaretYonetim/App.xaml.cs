using System.Windows;
using TicaretYonetim.Data;

namespace TicaretYonetim;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (s, ex) =>
        {
            MessageBox.Show(
                "Beklenmeyen bir hata oluştu:\n" + ex.Exception.Message,
                "Ticaret Yönetim", MessageBoxButton.OK, MessageBoxImage.Warning);
            ex.Handled = true;
        };

        using var db = new AppDbContext();
        db.SemaGuncelle();
    }
}
