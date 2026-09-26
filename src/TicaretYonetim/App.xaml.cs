using System.IO;
using System.Windows;
using TicaretYonetim.Data;
using TicaretYonetim.Services;

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

        OllamaAyarlari.Yukle().Uygula();

        OtoYedekAl();
    }

    private static void OtoYedekAl()
    {
        try
        {
            var yedekler = YedeklemeServisi.YedekleriListele();
            var son = yedekler.FirstOrDefault();
            if (son is null || (DateTime.Now - File.GetLastWriteTime(son)).TotalHours > 24)
                YedeklemeServisi.YedekOlustur();
        }
        catch
        {
        }
    }
}
