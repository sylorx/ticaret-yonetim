using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore;
using TicaretYonetim.Data;
using TicaretYonetim.Models;

namespace TicaretYonetim.ViewModels;

public class AlislarViewModel : ViewModelBase
{
    public ObservableCollection<Alis> Alislar { get; } = new();
    public ObservableCollection<Urun> UrunSecenekleri { get; } = new();

    private DateTime? _baslangic;
    public DateTime? Baslangic { get => _baslangic; set { if (SetField(ref _baslangic, value)) Yukle(); } }

    private DateTime? _bitis;
    public DateTime? Bitis { get => _bitis; set { if (SetField(ref _bitis, value)) Yukle(); } }

    private Alis _yeniAlis = YeniKayit();
    public Alis YeniAlis { get => _yeniAlis; set => SetField(ref _yeniAlis, value); }

    private Alis? _secili;
    public Alis? Secili { get => _secili; set => SetField(ref _secili, value); }

    public RelayCommand EkleCommand { get; }
    public RelayCommand SilCommand { get; }

    public AlislarViewModel()
    {
        EkleCommand = new RelayCommand(_ => Ekle());
        SilCommand = new RelayCommand(_ => Sil(), _ => Secili is not null);
    }

    private static Alis YeniKayit() => new() { Tarih = DateTime.Now, Miktar = 1 };

    public void Yukle()
    {
        using var db = new AppDbContext();

        UrunSecenekleri.Clear();
        foreach (var u in db.Urunler.Where(u => u.Aktif).OrderBy(u => u.Ad).ToList())
            UrunSecenekleri.Add(u);

        var sorgu = db.Alislar.Include(a => a.Urun).AsQueryable();
        if (Baslangic.HasValue) sorgu = sorgu.Where(a => a.Tarih >= Baslangic.Value);
        if (Bitis.HasValue) sorgu = sorgu.Where(a => a.Tarih <= Bitis.Value.AddDays(1).AddTicks(-1));

        Alislar.Clear();
        foreach (var a in sorgu.OrderByDescending(a => a.Tarih).ToList())
            Alislar.Add(a);
    }

    private void Ekle()
    {
        if (string.IsNullOrWhiteSpace(YeniAlis.Aciklama)) return;

        using var db = new AppDbContext();
        if (YeniAlis.UrunId == 0) YeniAlis.UrunId = null;
        db.Alislar.Add(YeniAlis);
        db.SaveChanges();

        YeniAlis = YeniKayit();
        Yukle();
    }

    private void Sil()
    {
        if (Secili is null) return;
        using var db = new AppDbContext();
        var kayit = db.Alislar.Find(Secili.Id);
        if (kayit is not null)
        {
            db.Alislar.Remove(kayit);
            db.SaveChanges();
        }
        Secili = null;
        Yukle();
    }
}
