using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using TicaretYonetim.Data;

namespace TicaretYonetim.ViewModels;

public class UrunSatisRaporu
{
    public string UrunAdi { get; set; } = string.Empty;
    public int SatilanAdet { get; set; }
    public decimal ToplamCiro { get; set; }
    public decimal ToplamMaliyet { get; set; }
    public decimal Kar => ToplamCiro - ToplamMaliyet;
}

public class RaporlarViewModel : ViewModelBase
{
    private DateTime? _baslangic = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
    public DateTime? Baslangic { get => _baslangic; set { if (SetField(ref _baslangic, value)) Yukle(); } }

    private DateTime? _bitis;
    public DateTime? Bitis { get => _bitis; set { if (SetField(ref _bitis, value)) Yukle(); } }

    public ObservableCollection<UrunSatisRaporu> UrunRaporu { get; } = new();

    private decimal _toplamCiro;
    public decimal ToplamCiro { get => _toplamCiro; set => SetField(ref _toplamCiro, value); }

    private decimal _toplamMaliyet;
    public decimal ToplamMaliyet { get => _toplamMaliyet; set => SetField(ref _toplamMaliyet, value); }

    private decimal _toplamKar;
    public decimal ToplamKar { get => _toplamKar; set => SetField(ref _toplamKar, value); }

    public RelayCommand DisaAktarCommand { get; }

    public RaporlarViewModel()
    {
        DisaAktarCommand = new RelayCommand(_ => DisaAktar());
    }

    public void Yukle()
    {
        using var db = new AppDbContext();
        var sorgu = db.Satislar.Include(s => s.Urun).AsQueryable();
        if (Baslangic.HasValue) sorgu = sorgu.Where(s => s.Tarih >= Baslangic.Value);
        if (Bitis.HasValue) sorgu = sorgu.Where(s => s.Tarih <= Bitis.Value.AddDays(1).AddTicks(-1));

        var satislar = sorgu.ToList();

        UrunRaporu.Clear();
        var gruplar = satislar.GroupBy(s => s.Urun?.Ad ?? "-")
            .Select(g => new UrunSatisRaporu
            {
                UrunAdi = g.Key,
                SatilanAdet = g.Sum(s => s.Adet),
                ToplamCiro = g.Sum(s => s.ToplamTutar),
                ToplamMaliyet = g.Sum(s => (s.Urun?.BirimMaliyet ?? 0) * s.Adet)
            })
            .OrderByDescending(g => g.ToplamCiro);

        foreach (var g in gruplar) UrunRaporu.Add(g);

        ToplamCiro = UrunRaporu.Sum(u => u.ToplamCiro);
        ToplamMaliyet = UrunRaporu.Sum(u => u.ToplamMaliyet);
        ToplamKar = ToplamCiro - ToplamMaliyet;
    }

    private void DisaAktar()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "CSV Dosyası|*.csv",
            FileName = $"satis_raporu_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };
        if (dlg.ShowDialog() != true) return;

        var sb = new StringBuilder();
        sb.AppendLine("Ürün;Satılan Adet;Toplam Ciro;Toplam Maliyet;Kâr");
        foreach (var r in UrunRaporu)
        {
            sb.AppendLine($"{r.UrunAdi};{r.SatilanAdet};{r.ToplamCiro.ToString(CultureInfo.InvariantCulture)};{r.ToplamMaliyet.ToString(CultureInfo.InvariantCulture)};{r.Kar.ToString(CultureInfo.InvariantCulture)}");
        }
        File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
    }
}
