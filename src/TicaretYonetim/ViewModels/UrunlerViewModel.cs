using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Win32;
using TicaretYonetim.Data;
using TicaretYonetim.Models;
using TicaretYonetim.Services;

namespace TicaretYonetim.ViewModels;

public class UrunlerViewModel : ViewModelBase
{
    public ObservableCollection<Urun> Urunler { get; } = new();
    public ObservableCollection<Filament> FilamentSecenekleri { get; } = new();

    private string _aramaMetni = string.Empty;
    public string AramaMetni
    {
        get => _aramaMetni;
        set { if (SetField(ref _aramaMetni, value)) Yukle(); }
    }

    private Urun? _secili;
    public Urun? Secili
    {
        get => _secili;
        set
        {
            if (SetField(ref _secili, value) && value is not null)
                DuzenlenenUrun = Klonla(value);
        }
    }

    private Urun _duzenlenenUrun = YeniUrun();
    public Urun DuzenlenenUrun
    {
        get => _duzenlenenUrun;
        set => SetField(ref _duzenlenenUrun, value);
    }

    public Array TurListesi => Enum.GetValues(typeof(UrunTuru));

    public RelayCommand YeniCommand { get; }
    public RelayCommand KaydetCommand { get; }
    public RelayCommand SilCommand { get; }
    public RelayCommand ResimSecCommand { get; }
    public RelayCommand IceAktarCommand { get; }

    public UrunlerViewModel()
    {
        YeniCommand = new RelayCommand(_ => { DuzenlenenUrun = YeniUrun(); Secili = null; });
        KaydetCommand = new RelayCommand(_ => Kaydet());
        SilCommand = new RelayCommand(_ => Sil(), _ => Secili is not null);
        ResimSecCommand = new RelayCommand(_ => ResimSec());
        IceAktarCommand = new RelayCommand(_ => IceAktar());
    }

    private static Urun YeniUrun() => new() { Ad = string.Empty, Tur = UrunTuru.Baski3D };

    private static Urun Klonla(Urun u) => new()
    {
        Id = u.Id,
        Ad = u.Ad,
        Aciklama = u.Aciklama,
        Tur = u.Tur,
        Kategori = u.Kategori,
        BirimMaliyet = u.BirimMaliyet,
        SatisFiyati = u.SatisFiyati,
        StokAdedi = u.StokAdedi,
        FilamentGram = u.FilamentGram,
        FilamentId = u.FilamentId,
        ResimYolu = u.ResimYolu,
        OlusturmaTarihi = u.OlusturmaTarihi,
        Aktif = u.Aktif
    };

    public void Yukle()
    {
        using var db = new AppDbContext();

        FilamentSecenekleri.Clear();
        foreach (var f in db.Filamentler.Where(f => f.Aktif).OrderBy(f => f.Ad).ToList())
            FilamentSecenekleri.Add(f);

        var sorgu = db.Urunler.AsQueryable();
        if (!string.IsNullOrWhiteSpace(AramaMetni))
            sorgu = sorgu.Where(u => u.Ad.Contains(AramaMetni));

        var liste = sorgu.OrderByDescending(u => u.OlusturmaTarihi).ToList();
        Urunler.Clear();
        foreach (var u in liste) Urunler.Add(u);
    }

    private void ResimSec()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Resim Dosyaları|*.jpg;*.jpeg;*.png;*.bmp;*.webp;*.gif",
            Title = "Ürün Resmi Seç"
        };
        if (dlg.ShowDialog() == true)
        {
            var yeniYol = ResimServisi.Kopyala(dlg.FileName);
            if (yeniYol is not null)
            {
                DuzenlenenUrun.ResimYolu = yeniYol;
                Bildir(nameof(DuzenlenenUrun));
            }
        }
    }

    private void Kaydet()
    {
        if (string.IsNullOrWhiteSpace(DuzenlenenUrun.Ad)) return;

        using var db = new AppDbContext();
        if (DuzenlenenUrun.Id == 0)
        {
            db.Urunler.Add(DuzenlenenUrun);
        }
        else
        {
            db.Urunler.Update(DuzenlenenUrun);
        }
        db.SaveChanges();

        DuzenlenenUrun = YeniUrun();
        Secili = null;
        Yukle();
    }

    private void Sil()
    {
        if (Secili is null) return;

        var sonuc = System.Windows.MessageBox.Show(
            $"\"{Secili.Ad}\" ürününü silmek istediğinize emin misiniz? Bu işlem geri alınamaz.",
            "Ürünü Sil", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question,
            System.Windows.MessageBoxResult.No);
        if (sonuc != System.Windows.MessageBoxResult.Yes) return;

        try
        {
            using var db = new AppDbContext();
            var kayit = db.Urunler.Find(Secili.Id);
            if (kayit is not null)
            {
                ResimServisi.Sil(kayit.ResimYolu);
                db.Urunler.Remove(kayit);
                db.SaveChanges();
            }
        }
        catch (Exception)
        {
            System.Windows.MessageBox.Show(
                "Bu ürüne bağlı satış kayıtları olduğu için silinemiyor. Önce ilgili satışları silin.",
                "Silinemedi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }
        Secili = null;
        DuzenlenenUrun = YeniUrun();
        Yukle();
    }

    private void IceAktar()
    {
        var dlg = new OpenFileDialog { Filter = "CSV Dosyası|*.csv", Title = "Ürünleri CSV'den İçe Aktar" };
        if (dlg.ShowDialog() != true) return;

        var satirlar = File.ReadAllLines(dlg.FileName, Encoding.UTF8);
        if (satirlar.Length == 0) return;

        var baslikVar = satirlar[0].Contains("Ad", StringComparison.OrdinalIgnoreCase)
                        && !decimal.TryParse(satirlar[0].Split(';').ElementAtOrDefault(4), out _);
        var basla = baslikVar ? 1 : 0;

        int eklendi = 0, hatali = 0;
        using var db = new AppDbContext();
        for (int i = basla; i < satirlar.Length; i++)
        {
            var satir = satirlar[i];
            if (string.IsNullOrWhiteSpace(satir)) continue;

            var sutunlar = satir.Split(';');
            if (sutunlar.Length < 1 || string.IsNullOrWhiteSpace(sutunlar[0])) { hatali++; continue; }

            try
            {
                var urun = new Urun
                {
                    Ad = sutunlar.ElementAtOrDefault(0)?.Trim() ?? string.Empty,
                    Aciklama = sutunlar.ElementAtOrDefault(1)?.Trim() ?? string.Empty,
                    Tur = Enum.TryParse<UrunTuru>(sutunlar.ElementAtOrDefault(2)?.Trim(), true, out var tur) ? tur : UrunTuru.Baski3D,
                    Kategori = sutunlar.ElementAtOrDefault(3)?.Trim() ?? string.Empty,
                    BirimMaliyet = decimal.TryParse(sutunlar.ElementAtOrDefault(4)?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var maliyet) ? maliyet : 0,
                    SatisFiyati = decimal.TryParse(sutunlar.ElementAtOrDefault(5)?.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var fiyat) ? fiyat : 0,
                    StokAdedi = int.TryParse(sutunlar.ElementAtOrDefault(6)?.Trim(), out var stok) ? stok : 0
                };

                if (string.IsNullOrWhiteSpace(urun.Ad)) { hatali++; continue; }

                db.Urunler.Add(urun);
                eklendi++;
            }
            catch
            {
                hatali++;
            }
        }

        db.SaveChanges();
        Yukle();

        System.Windows.MessageBox.Show(
            $"İçe aktarma tamamlandı.\n\nEklenen ürün: {eklendi}\nHatalı/atlanan satır: {hatali}\n\n" +
            "Beklenen CSV sütun sırası (noktalı virgülle ayrılmış):\n" +
            "Ad;Açıklama;Tür (Website/Katalog/Baski3D);Kategori;Birim Maliyet;Satış Fiyatı;Stok Adedi",
            "CSV İçe Aktarma", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
}
