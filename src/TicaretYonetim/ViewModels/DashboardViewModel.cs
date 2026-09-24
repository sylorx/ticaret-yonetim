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

    private decimal _filamentToplamGram;
    public decimal FilamentToplamGram { get => _filamentToplamGram; set => SetField(ref _filamentToplamGram, value); }

    private decimal _filamentKalanGram;
    public decimal FilamentKalanGram { get => _filamentKalanGram; set => SetField(ref _filamentKalanGram, value); }

    private decimal _filamentHarcananGram;
    public decimal FilamentHarcananGram { get => _filamentHarcananGram; set => SetField(ref _filamentHarcananGram, value); }

    private decimal _filamentKullanimYuzde;
    public decimal FilamentKullanimYuzde { get => _filamentKullanimYuzde; set => SetField(ref _filamentKullanimYuzde, value); }

    private int _filamentCesidi;
    public int FilamentCesidi { get => _filamentCesidi; set => SetField(ref _filamentCesidi, value); }

    public ObservableCollection<SonHareket> SonHareketler { get; } = new();
    public ObservableCollection<KategoriOzet> KanalOzeti { get; } = new();

    private ISeries[] _kazancSeries = Array.Empty<ISeries>();
    public ISeries[] KazancSeries { get => _kazancSeries; set => SetField(ref _kazancSeries, value); }

    private Axis[] _kazancXEksen = Array.Empty<Axis>();
    public Axis[] KazancXEksen { get => _kazancXEksen; set => SetField(ref _kazancXEksen, value); }

    private Axis[] _kazancYEksen = Array.Empty<Axis>();
    public Axis[] KazancYEksen { get => _kazancYEksen; set => SetField(ref _kazancYEksen, value); }

    private ISeries[] _kanalSeries = Array.Empty<ISeries>();
    public ISeries[] KanalSeries { get => _kanalSeries; set => SetField(ref _kanalSeries, value); }

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

        var filamentler = db.Filamentler.ToList();
        FilamentToplamGram = filamentler.Sum(f => f.ToplamGram);
        FilamentKalanGram = filamentler.Sum(f => f.KalanGram);
        FilamentHarcananGram = filamentler.Sum(f => f.HarcananGram);
        FilamentKullanimYuzde = FilamentToplamGram > 0 ? (FilamentHarcananGram / FilamentToplamGram) * 100 : 0;
        FilamentCesidi = filamentler.Count(f => f.Aktif);

        var simdi = DateTime.Now;
        var aylar = Enumerable.Range(0, 12)
            .Select(i => new DateTime(simdi.Year, simdi.Month, 1).AddMonths(-11 + i))
            .ToList();

        var ciroListesi = new List<double>();
        var karListesi = new List<double>();
        var adetListesi = new List<double>();
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
            adetListesi.Add(ayinSatislari.Sum(s => s.Adet));
            etiketler.Add(ay.ToString("MMM yy", trTakvim));
        }

        var ciroRenk = SKColor.Parse("#2E7D32");
        var karRenk = SKColor.Parse("#1565C0");
        var adetRenk = SKColor.Parse("#F9A825");

        KazancSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = ciroListesi,
                Name = "Ciro",
                Fill = new SolidColorPaint(ciroRenk.WithAlpha(90)),
                Stroke = new SolidColorPaint(ciroRenk) { StrokeThickness = 1 },
                MaxBarWidth = 18,
                Rx = 4,
                Ry = 4
            },
            new LineSeries<double>
            {
                Values = karListesi,
                Name = "Kâr",
                Fill = new LinearGradientPaint(
                    new[] { karRenk.WithAlpha(70), karRenk.WithAlpha(0) },
                    new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                GeometrySize = 7,
                GeometryStroke = new SolidColorPaint(karRenk) { StrokeThickness = 2 },
                Stroke = new SolidColorPaint(karRenk) { StrokeThickness = 3 },
                LineSmoothness = 0.6
            },
            new LineSeries<double>
            {
                Values = adetListesi,
                Name = "Satış Adedi",
                Fill = null,
                GeometrySize = 5,
                Stroke = new SolidColorPaint(adetRenk) { StrokeThickness = 2 },
                LineSmoothness = 0.6
            }
        };

        KazancXEksen = new Axis[]
        {
            new Axis
            {
                Labels = etiketler,
                LabelsRotation = -35,
                TextSize = 11
            }
        };

        KazancYEksen = new Axis[]
        {
            new Axis
            {
                Labeler = v => v.ToString("N0") + " ₺",
                TextSize = 11,
                MinStep = 1
            }
        };

        var kanalRenkleri = new[]
        {
            SKColor.Parse("#2E7D32"),
            SKColor.Parse("#1565C0"),
            SKColor.Parse("#F9A825"),
            SKColor.Parse("#8E24AA")
        };
        var kanalListesi = gruplar.ToList();
        KanalSeries = kanalListesi.Select((g, i) => new PieSeries<double>
        {
            Values = new[] { (double)g.ToplamSatis },
            Name = $"{g.Kategori} ({g.ToplamSatis:N0} ₺)",
            Fill = new SolidColorPaint(kanalRenkleri[i % kanalRenkleri.Length]),
            InnerRadius = 55
        }).ToArray();
    }
}
