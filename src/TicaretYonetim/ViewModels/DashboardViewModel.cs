using System.Collections.ObjectModel;
using System.Globalization;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
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

    private SolidColorPaint? _etiketPaint;
    public SolidColorPaint? EtiketPaint { get => _etiketPaint; set => SetField(ref _etiketPaint, value); }

    private SolidColorPaint? _tooltipZeminPaint;
    public SolidColorPaint? TooltipZeminPaint { get => _tooltipZeminPaint; set => SetField(ref _tooltipZeminPaint, value); }

    private SolidColorPaint? _tooltipMetinPaint;
    public SolidColorPaint? TooltipMetinPaint { get => _tooltipMetinPaint; set => SetField(ref _tooltipMetinPaint, value); }

    private static SKColor EtiketRengi() =>
        ModernWpf.ThemeManager.Current.ApplicationTheme == ModernWpf.ApplicationTheme.Dark
            ? SKColor.Parse("#9AA5B4") : SKColor.Parse("#374151");

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
                Fill = new LinearGradientPaint(
                    new[] { SKColor.Parse("#81C784").WithAlpha(235), ciroRenk.WithAlpha(225), SKColor.Parse("#1B5E20").WithAlpha(205) },
                    new SKPoint(0, 0), new SKPoint(0, 1)),
                Stroke = null,
                MaxBarWidth = 22,
                Rx = 7,
                Ry = 7,
                YToolTipLabelFormatter = p => p.Coordinate.PrimaryValue.ToString("N0") + " ₺"
            },
            new LineSeries<double>
            {
                Values = karListesi,
                Name = "Kâr",
                Fill = new LinearGradientPaint(
                    new[] { karRenk.WithAlpha(80), karRenk.WithAlpha(0) },
                    new SKPoint(0.5f, 0), new SKPoint(0.5f, 1)),
                GeometrySize = 9,
                GeometryStroke = new SolidColorPaint(karRenk) { StrokeThickness = 2 },
                GeometryFill = new SolidColorPaint(SKColor.Parse("#FFFFFF")),
                Stroke = new SolidColorPaint(karRenk) { StrokeThickness = 3 },
                LineSmoothness = 1
            },
            new LineSeries<double>
            {
                Values = adetListesi,
                Name = "Satış Adedi",
                Fill = null,
                GeometrySize = 6,
                GeometryStroke = new SolidColorPaint(adetRenk) { StrokeThickness = 2 },
                GeometryFill = new SolidColorPaint(SKColor.Parse("#FFFFFF")),
                Stroke = new SolidColorPaint(adetRenk)
                {
                    StrokeThickness = 2,
                    PathEffect = new DashEffect(new[] { 7f, 5f }, 0)
                },
                LineSmoothness = 1
            }
        };

        var koyuMu = ModernWpf.ThemeManager.Current.ApplicationTheme == ModernWpf.ApplicationTheme.Dark;
        var tTipZemin = koyuMu ? SKColor.Parse("#262E3A") : SKColor.Parse("#FFFFFF");
        var tTipMetin = koyuMu ? SKColor.Parse("#F3F5F8") : SKColor.Parse("#111827");
        TooltipZeminPaint = new SolidColorPaint(tTipZemin.WithAlpha(235)) { StrokeThickness = 0 };
        TooltipMetinPaint = new SolidColorPaint(tTipMetin);

        KazancXEksen = new Axis[]
        {
            new Axis
            {
                Labels = etiketler,
                LabelsRotation = -35,
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(EtiketRengi()),
                CrosshairPaint = new SolidColorPaint(SKColor.Parse("#552E7D32")) { StrokeThickness = 26 },
                CrosshairSnapEnabled = true
            }
        };

        KazancYEksen = new Axis[]
        {
            new Axis
            {
                Labeler = v => v.ToString("N0") + " ₺",
                TextSize = 11,
                MinStep = 1,
                LabelsPaint = new SolidColorPaint(EtiketRengi()),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#22808080"))
                {
                    StrokeThickness = 1,
                    PathEffect = new DashEffect(new[] { 4f, 4f }, 0)
                }
            }
        };

        EtiketPaint = new SolidColorPaint(EtiketRengi());

        var kanalRenkleri = new[]
        {
            (SKColor.Parse("#43A047"), SKColor.Parse("#1B5E20")),
            (SKColor.Parse("#42A5F5"), SKColor.Parse("#1565C0")),
            (SKColor.Parse("#FFCA28"), SKColor.Parse("#EF6C00")),
            (SKColor.Parse("#AB47BC"), SKColor.Parse("#6A1B9A"))
        };
        var kanalListesi = gruplar.ToList();
        KanalSeries = kanalListesi.Select((g, i) => new PieSeries<double>
        {
            Values = new[] { (double)g.ToplamSatis },
            Name = $"{g.Kategori} ({g.ToplamSatis:N0} ₺)",
            Fill = new RadialGradientPaint(
                kanalRenkleri[i % kanalRenkleri.Length].Item1,
                kanalRenkleri[i % kanalRenkleri.Length].Item2),
            InnerRadius = 62,
            CornerRadius = 8,
            Pushout = 14,
            HoverPushout = 20,
            Stroke = new SolidColorPaint(tTipZemin) { StrokeThickness = 2 },
            ToolTipLabelFormatter = p => p.Coordinate.PrimaryValue.ToString("N0") + " ₺"
        }).ToArray();
    }
}
