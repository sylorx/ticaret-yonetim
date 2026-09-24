using System.Collections.ObjectModel;
using System.Globalization;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using TicaretYonetim.Data;
using TicaretYonetim.Models;

namespace TicaretYonetim.ViewModels;

public class SonHareket
{
    public string Tur { get; set; } = string.Empty;
    public DateTime Tarih { get; set; }
    public string UrunAdi { get; set; } = string.Empty;
    public decimal Tutar { get; set; }
    public string Detay { get; set; } = string.Empty;
}

public class KategoriOzet
{
    public string Kategori { get; set; } = string.Empty;
    public decimal ToplamSatis { get; set; }
    public int Adet { get; set; }
}

public class DashboardViewModel : ViewModelBase
{
    private decimal _toplamSatisTutari;
    public decimal ToplamSatisTutari { get => _toplamSatisTutari; set => SetField(ref _toplamSatisTutari, value); }

    private decimal _toplamAlisTutari;
    public decimal ToplamAlisTutari { get => _toplamAlisTutari; set => SetField(ref _toplamAlisTutari, value); }

    private decimal _netKar;
    public decimal NetKar { get => _netKar; set => SetField(ref _netKar, value); }

    private int _toplamSatisAdedi;
    public int ToplamSatisAdedi { get => _toplamSatisAdedi; set => SetField(ref _toplamSatisAdedi, value); }

    private int _urunSayisi;
    public int UrunSayisi { get => _urunSayisi; set => SetField(ref _urunSayisi, value); }

    private decimal _buAySatis;
    public decimal BuAySatis { get => _buAySatis; set => SetField(ref _buAySatis, value); }

    private decimal _ortalamaKarMarji;
    public decimal OrtalamaKarMarji { get => _ortalamaKarMarji; set => SetField(ref _ortalamaKarMarji, value); }

    public ObservableCollection<SonHareket> SonHareketler { get; } = new();
    public ObservableCollection<KategoriOzet> KanalOzeti { get; } = new();

    private ISeries[] _kazancSeries = Array.Empty<ISeries>();
    public ISeries[] KazancSeries { get => _kazancSeries; set => SetField(ref _kazancSeries, value); }

    private Axis[] _kazancXEksen = Array.Empty<Axis>();
    public Axis[] KazancXEksen { get => _kazancXEksen; set => SetField(ref _kazancXEksen, value); }

    public void Yukle()
    {
        using var db = new AppDbContext();

        var satislar = db.Satislar.Include(s => s.Urun).ToList();
        var alislar = db.Alislar.ToList();

        ToplamSatisTutari = satislar.Sum(s => s.ToplamTutar);
        ToplamAlisTutari = alislar.Sum(a => a.ToplamTutar);
        var satisMaliyeti = satislar.Sum(s => (s.Urun?.BirimMaliyet ?? 0) * s.Adet);
        NetKar = ToplamSatisTutari - ToplamAlisTutari;
        ToplamSatisAdedi = satislar.Sum(s => s.Adet);
        UrunSayisi = db.Urunler.Count(u => u.Aktif);
        var urunKari = ToplamSatisTutari - satisMaliyeti;
        OrtalamaKarMarji = ToplamSatisTutari > 0 ? (urunKari / ToplamSatisTutari) * 100 : 0;

        var ayBaslangic = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        BuAySatis = satislar.Where(s => s.Tarih >= ayBaslangic).Sum(s => s.ToplamTutar);

        SonHareketler.Clear();
        var hareketler = new List<SonHareket>();
        hareketler.AddRange(satislar.Select(s => new SonHareket
        {
            Tur = "Satış",
            Tarih = s.Tarih,
            UrunAdi = s.Urun?.Ad ?? "-",
            Tutar = s.ToplamTutar,
            Detay = $"{s.Adet} adet - {s.Kanal}"
        }));
        hareketler.AddRange(alislar.Select(a => new SonHareket
        {
            Tur = "Alış",
            Tarih = a.Tarih,
            UrunAdi = a.Aciklama,
            Tutar = a.ToplamTutar,
            Detay = a.Tedarikci ?? "-"
        }));
        foreach (var h in hareketler.OrderByDescending(h => h.Tarih).Take(15))
            SonHareketler.Add(h);

        KanalOzeti.Clear();
        var gruplar = satislar.GroupBy(s => s.Kanal.ToString())
            .Select(g => new KategoriOzet { Kategori = g.Key, ToplamSatis = g.Sum(s => s.ToplamTutar), Adet = g.Sum(s => s.Adet) })
            .OrderByDescending(g => g.ToplamSatis);
        foreach (var g in gruplar)
            KanalOzeti.Add(g);

        var simdi = DateTime.Now;
        var aylar = Enumerable.Range(0, 6)
            .Select(i => new DateTime(simdi.Year, simdi.Month, 1).AddMonths(-5 + i))
            .ToList();

        var ciroListesi = new List<double>();
        var karListesi = new List<double>();
        var etiketler = new List<string>();
        var trTakvim = new CultureInfo("tr-TR");

        foreach (var ay in aylar)
        {
            var ayBitis = ay.AddMonths(1);
            var ayinSatislari = satislar.Where(s => s.Tarih >= ay && s.Tarih < ayBitis).ToList();
            var ciro = ayinSatislari.Sum(s => s.ToplamTutar);
            var maliyet = ayinSatislari.Sum(s => (s.Urun?.BirimMaliyet ?? 0) * s.Adet);

            ciroListesi.Add((double)ciro);
            karListesi.Add((double)(ciro - maliyet));
            etiketler.Add(ay.ToString("MMM yy", trTakvim));
        }

        KazancSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = ciroListesi,
                Name = "Ciro",
                Fill = null,
                GeometrySize = 6,
                Stroke = new SolidColorPaint(SKColor.Parse("#2E7D32")) { StrokeThickness = 3 }
            },
            new LineSeries<double>
            {
                Values = karListesi,
                Name = "Kâr",
                Fill = null,
                GeometrySize = 6,
                Stroke = new SolidColorPaint(SKColor.Parse("#1565C0")) { StrokeThickness = 3 }
            }
        };

        KazancXEksen = new Axis[]
        {
            new Axis { Labels = etiketler }
        };
    }
}
