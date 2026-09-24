using System.Windows;
using TicaretYonetim.Data;

namespace TicaretYonetim;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        using var db = new AppDbContext();
        db.SemaGuncelle();
    }
}
