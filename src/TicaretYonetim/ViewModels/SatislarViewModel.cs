using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using TicaretYonetim.Data;
using TicaretYonetim.Models;

namespace TicaretYonetim.ViewModels;

public class SatislarViewModel : ViewModelBase
{
    private readonly UrunlerViewModel _urunlerVm;

    public ObservableCollection<Satis> Satislar { get; } = new();
    public ObservableCollection<Urun> UrunSecenekleri { get; } = new();

    public Array KanalListesi => Enum.GetValues(typeof(SatisKanali));

    private DateTime? _baslangic;
    public DateTime? Baslangic { get => _baslangic; set { if (SetField(ref _baslangic, value)) Yukle(); } }

    private DateTime? _bitis;
    public DateTime? Bitis { get => _bitis; set { if (SetField(ref _bitis, value)) Yukle(); } }

    private Satis _yeniSatis = YeniKayit();
    public Satis YeniSatis
    {
        get => _yeniSatis;
        set { if (SetField(ref _yeniSatis, value)) Bildir(nameof(TahminiKar)); }
    }

    private Satis? _secili;
    public Satis? Secili { get => _secili; set => SetField(ref _secili, value); }

    public int SecilenUrunId
    {
        get => YeniSatis.UrunId;
        set
        {
            YeniSatis.UrunId = value;
            var urun = UrunSecenekleri.FirstOrDefault(u => u.Id == value);
            if (urun is not null && YeniSatis.BirimFiyat <= 0)
                YeniSatis.BirimFiyat = urun.SatisFiyati;
            Bildir(nameof(SecilenUrunId));
            Bildir(nameof(YeniSatis));
            Bildir(nameof(TahminiKar));
            Bildir(nameof(TahminiFilamentGram));
        }
    }

    public int GirilenAdet
    {
        get => YeniSatis.Adet;
        set { YeniSatis.Adet = value; Bildir(nameof(TahminiKar)); Bildir(nameof(TahminiFilamentGram)); }
    }

    public decimal GirilenBirimFiyat
    {
        get => YeniSatis.BirimFiyat;
        set { YeniSatis.BirimFiyat = value; Bildir(nameof(TahminiKar)); }
    }

    public decimal TahminiKar
    {
        get
        {
            var urun = UrunSecenekleri.FirstOrDefault(u => u.Id == YeniSatis.UrunId);
            if (urun is null) return 0;
            var birimFiyat = YeniSatis.BirimFiyat > 0 ? YeniSatis.BirimFiyat : urun.SatisFiyati;
            return (birimFiyat - urun.BirimMaliyet) * Math.Max(YeniSatis.Adet, 0);
        }
    }

    public decimal TahminiFilamentGram
    {
        get
        {
            var urun = UrunSecenekleri.FirstOrDefault(u => u.Id == YeniSatis.UrunId);
            if (urun is null) return 0;
            return urun.FilamentGram * Math.Max(YeniSatis.Adet, 0);
        }
    }

    public RelayCommand EkleCommand { get; }
    public RelayCommand SilCommand { get; }

    public SatislarViewModel(UrunlerViewModel urunlerVm)
    {
        _urunlerVm = urunlerVm;
        EkleCommand = new RelayCommand(_ => Ekle());
        SilCommand = new RelayCommand(_ => Sil(), _ => Secili is not null);
    }

    private static Satis YeniKayit() => new() { Tarih = DateTime.Now, Adet = 1, Kanal = SatisKanali.Website };

    public void Yukle()
    {
        using var db = new AppDbContext();

        UrunSecenekleri.Clear();
        foreach (var u in db.Urunler.Where(u => u.Aktif).OrderBy(u => u.Ad).ToList())
            UrunSecenekleri.Add(u);

        var sorgu = db.Satislar.Include(s => s.Urun).AsQueryable();
        if (Baslangic.HasValue) sorgu = sorgu.Where(s => s.Tarih >= Baslangic.Value);
        if (Bitis.HasValue) sorgu = sorgu.Where(s => s.Tarih <= Bitis.Value.AddDays(1).AddTicks(-1));

        Satislar.Clear();
        foreach (var s in sorgu.OrderByDescending(s => s.Tarih).ToList())
            Satislar.Add(s);
    }

    private void Ekle()
    {
        if (YeniSatis.UrunId == 0 || YeniSatis.Adet <= 0) return;

        using var db = new AppDbContext();
        var urun = db.Urunler.Find(YeniSatis.UrunId);
        if (urun is null) return;

        if (YeniSatis.BirimFiyat <= 0) YeniSatis.BirimFiyat = urun.SatisFiyati;
        YeniSatis.ToplamTutar = YeniSatis.BirimFiyat * YeniSatis.Adet;

        db.Satislar.Add(YeniSatis);

        urun.StokAdedi = Math.Max(0, urun.StokAdedi - YeniSatis.Adet);
        db.Urunler.Update(urun);

        if (urun.FilamentId is int filamentId && urun.FilamentGram > 0)
        {
            var filament = db.Filamentler.Find(filamentId);
            if (filament is not null)
            {
                filament.KalanGram = Math.Max(0, filament.KalanGram - urun.FilamentGram * YeniSatis.Adet);
                db.Filamentler.Update(filament);
            }
        }

        db.SaveChanges();

        YeniSatis = YeniKayit();
        Yukle();
        _urunlerVm.Yukle();
        Bildir(nameof(TahminiKar));
    }

    private void Sil()
    {
        if (Secili is null) return;

        var sonuc = System.Windows.MessageBox.Show(
            "Bu satış kaydını silmek istediğinize emin misiniz? Bu işlem geri alınamaz.",
            "Satışı Sil", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question,
            System.Windows.MessageBoxResult.No);
        if (sonuc != System.Windows.MessageBoxResult.Yes) return;

        using var db = new AppDbContext();
        var kayit = db.Satislar.Find(Secili.Id);
        if (kayit is not null)
        {
            db.Satislar.Remove(kayit);
            db.SaveChanges();
        }
        Secili = null;
        Yukle();
    }
}
