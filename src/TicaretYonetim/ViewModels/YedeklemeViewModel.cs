using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using TicaretYonetim.Services;

namespace TicaretYonetim.ViewModels;

public class YedekBilgi
{
    public string Yol { get; init; } = "";
    public string Ad => Path.GetFileName(Yol);
    public DateTime Tarih => File.GetLastWriteTime(Yol);
    public string Boyut => (new FileInfo(Yol).Length / 1024.0).ToString("N0") + " KB";
}

public class YedeklemeViewModel : ViewModelBase
{
    public ObservableCollection<YedekBilgi> Yedekler { get; } = new();

    private YedekBilgi? _secili;
    public YedekBilgi? Secili { get => _secili; set => SetField(ref _secili, value); }

    private string _durum = "";
    public string Durum { get => _durum; set => SetField(ref _durum, value); }

    public RelayCommand YedekOlusturCommand { get; }
    public RelayCommand DisariAktarCommand { get; }
    public RelayCommand IceriAktarCommand { get; }
    public RelayCommand GeriYukleCommand { get; }
    public RelayCommand YedekSilCommand { get; }

    public YedeklemeViewModel()
    {
        YedekOlusturCommand = new RelayCommand(_ => YedekOlustur());
        DisariAktarCommand = new RelayCommand(_ => DisariAktar());
        IceriAktarCommand = new RelayCommand(_ => IceriAktar());
        GeriYukleCommand = new RelayCommand(_ => GeriYukle(), _ => Secili is not null);
        YedekSilCommand = new RelayCommand(_ => YedekSil(), _ => Secili is not null);
    }

    public void Yukle()
    {
        Yedekler.Clear();
        foreach (var yol in YedeklemeServisi.YedekleriListele())
            Yedekler.Add(new YedekBilgi { Yol = yol });
        Durum = Yedekler.Count == 0
            ? "Henüz yedek yok. İlk yedeğinizi oluşturun."
            : $"{Yedekler.Count} yedek mevcut. Son yedek: {Yedekler[0].Tarih:dd.MM.yyyy HH:mm}";
    }

    private void YedekOlustur()
    {
        try
        {
            var yol = YedeklemeServisi.YedekOlustur();
            Yukle();
            Durum = $"✅ Yedek oluşturuldu: {Path.GetFileName(yol)}";
        }
        catch (Exception ex)
        {
            Durum = $"⚠️ Yedek oluşturulamadı: {ex.Message}";
        }
    }

    private void DisariAktar()
    {
        var dlg = new SaveFileDialog
        {
            Title = "Yedeği Dışa Aktar (ZIP)",
            Filter = "ZIP arşivi (*.zip)|*.zip",
            FileName = $"ticaret_yedek_{DateTime.Now:yyyy-MM-dd}.zip"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            YedeklemeServisi.DisariAktar(dlg.FileName);
            Durum = $"✅ Dışa aktarıldı: {dlg.FileName}";
        }
        catch (Exception ex)
        {
            Durum = $"⚠️ Dışa aktarılamadı: {ex.Message}";
        }
    }

    private void IceriAktar()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Yedek Dosyasını Seç (ZIP)",
            Filter = "ZIP arşivi (*.zip)|*.zip"
        };
        if (dlg.ShowDialog() != true) return;
        var onay = MessageBox.Show(
            "İçe aktarma mevcut tüm verilerinizi bu yedekteki verilerle DEĞİŞTİRİR.\n\n" +
            "Güvenlik için mevcut verileriniz önce otomatik yedeklenir.\n\nDevam edilsin mi?",
            "Yedekten Geri Yükle", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (onay != MessageBoxResult.Yes) return;
        try
        {
            YedeklemeServisi.IceriAktar(dlg.FileName);
            VerileriYenile();
            Yukle();
            Durum = "✅ Yedek geri yüklendi. Tüm veriler güncellendi.";
        }
        catch (Exception ex)
        {
            Durum = $"⚠️ Geri yükleme başarısız: {ex.Message}";
        }
    }

    private void GeriYukle()
    {
        if (Secili is null) return;
        var onay = MessageBox.Show(
            $"'{Secili.Ad}' yedeğinden geri yüklenecek.\n\n" +
            "Mevcut verileriniz önce otomatik yedeklenir.\n\nDevam edilsin mi?",
            "Yedekten Geri Yükle", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (onay != MessageBoxResult.Yes) return;
        try
        {
            YedeklemeServisi.YedektenGeriYukle(Secili.Yol);
            VerileriYenile();
            Yukle();
            Durum = $"✅ '{Secili.Ad}' yedeğinden geri yüklendi.";
        }
        catch (Exception ex)
        {
            Durum = $"⚠️ Geri yükleme başarısız: {ex.Message}";
        }
    }

    private void YedekSil()
    {
        if (Secili is null) return;
        var onay = MessageBox.Show($"'{Secili.Ad}' yedeği silinsin mi?", "Yedek Sil",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (onay != MessageBoxResult.Yes) return;
        YedeklemeServisi.YedekSil(Secili.Yol);
        Yukle();
        Durum = "Yedek silindi.";
    }

    private static void VerileriYenile()
    {
        if (Application.Current.MainWindow?.DataContext is MainViewModel mv)
            mv.Yenile();
    }
}
