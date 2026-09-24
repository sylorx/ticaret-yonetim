using System.Collections.ObjectModel;
using Microsoft.Win32;
using TicaretYonetim.Data;
using TicaretYonetim.Models;
using TicaretYonetim.Services;

namespace TicaretYonetim.ViewModels;

public class UrunlerViewModel : ViewModelBase
{
    public ObservableCollection<Urun> Urunler { get; } = new();

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

    public UrunlerViewModel()
    {
        YeniCommand = new RelayCommand(_ => { DuzenlenenUrun = YeniUrun(); Secili = null; });
        KaydetCommand = new RelayCommand(_ => Kaydet());
        SilCommand = new RelayCommand(_ => Sil(), _ => Secili is not null);
        ResimSecCommand = new RelayCommand(_ => ResimSec());
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
        ResimYolu = u.ResimYolu,
        OlusturmaTarihi = u.OlusturmaTarihi,
        Aktif = u.Aktif
    };

    public void Yukle()
    {
        using var db = new AppDbContext();
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
}
