using System.Collections.ObjectModel;
using System.Windows.Media;
using TicaretYonetim.Data;
using TicaretYonetim.Models;

namespace TicaretYonetim.ViewModels;

public class RenkSecenegi
{
    public string Ad { get; set; } = string.Empty;
    public string Hex { get; set; } = "#FFFFFF";

    public Brush Firca
    {
        get
        {
            try { return new SolidColorBrush((Color)ColorConverter.ConvertFromString(Hex)); }
            catch { return Brushes.White; }
        }
    }
}

public class FilamentViewModel : ViewModelBase
{
    public static readonly List<RenkSecenegi> RenkPaleti = new()
    {
        new RenkSecenegi { Ad = "Kırmızı", Hex = "#D32F2F" },
        new RenkSecenegi { Ad = "Bordo", Hex = "#880E4F" },
        new RenkSecenegi { Ad = "Turuncu", Hex = "#F57C00" },
        new RenkSecenegi { Ad = "Sarı", Hex = "#FBC02D" },
        new RenkSecenegi { Ad = "Altın", Hex = "#FFD54F" },
        new RenkSecenegi { Ad = "Yeşil", Hex = "#388E3C" },
        new RenkSecenegi { Ad = "Açık Yeşil", Hex = "#7CB342" },
        new RenkSecenegi { Ad = "Turkuaz", Hex = "#00897B" },
        new RenkSecenegi { Ad = "Mavi", Hex = "#1976D2" },
        new RenkSecenegi { Ad = "Açık Mavi", Hex = "#4FC3F7" },
        new RenkSecenegi { Ad = "Lacivert", Hex = "#0D47A1" },
        new RenkSecenegi { Ad = "Mor", Hex = "#7B1FA2" },
        new RenkSecenegi { Ad = "Pembe", Hex = "#E91E63" },
        new RenkSecenegi { Ad = "Kahverengi", Hex = "#5D4037" },
        new RenkSecenegi { Ad = "Bej", Hex = "#D7CCC8" },
        new RenkSecenegi { Ad = "Beyaz", Hex = "#FAFAFA" },
        new RenkSecenegi { Ad = "Gri", Hex = "#9E9E9E" },
        new RenkSecenegi { Ad = "Koyu Gri", Hex = "#424242" },
        new RenkSecenegi { Ad = "Gümüş", Hex = "#B0BEC5" },
        new RenkSecenegi { Ad = "Siyah", Hex = "#212121" }
    };

    public ObservableCollection<Filament> Filamentler { get; } = new();

    private Filament? _secili;
    public Filament? Secili
    {
        get => _secili;
        set
        {
            if (SetField(ref _secili, value) && value is not null)
                Duzenlenen = Klonla(value);
        }
    }

    private Filament _duzenlenen = YeniKayit();
    public Filament Duzenlenen
    {
        get => _duzenlenen;
        set => SetField(ref _duzenlenen, value);
    }

    private decimal _toplamGram;
    public decimal ToplamGram { get => _toplamGram; set => SetField(ref _toplamGram, value); }

    private decimal _kalanGram;
    public decimal KalanGram { get => _kalanGram; set => SetField(ref _kalanGram, value); }

    private decimal _harcananGram;
    public decimal HarcananGram { get => _harcananGram; set => SetField(ref _harcananGram, value); }

    private decimal _kullanimYuzde;
    public decimal KullanimYuzde { get => _kullanimYuzde; set => SetField(ref _kullanimYuzde, value); }

    public RelayCommand YeniCommand { get; }
    public RelayCommand KaydetCommand { get; }
    public RelayCommand SilCommand { get; }
    public RelayCommand RenkSecCommand { get; }

    public FilamentViewModel()
    {
        YeniCommand = new RelayCommand(_ => { Duzenlenen = YeniKayit(); Secili = null; });
        KaydetCommand = new RelayCommand(_ => Kaydet());
        SilCommand = new RelayCommand(_ => Sil(), _ => Secili is not null);
        RenkSecCommand = new RelayCommand(p =>
        {
            if (p is RenkSecenegi renk)
            {
                Duzenlenen.Renk = renk.Ad;
                Bildir(nameof(Duzenlenen));
            }
        });
    }

    private static Filament YeniKayit() => new() { Ad = string.Empty, Tarih = DateTime.Now };

    private static Filament Klonla(Filament f) => new()
    {
        Id = f.Id,
        Ad = f.Ad,
        Renk = f.Renk,
        Marka = f.Marka,
        ToplamGram = f.ToplamGram,
        KalanGram = f.KalanGram,
        BirimFiyat = f.BirimFiyat,
        Tedarikci = f.Tedarikci,
        Tarih = f.Tarih,
        Aktif = f.Aktif
    };

    public void Yukle()
    {
        using var db = new AppDbContext();
        var liste = db.Filamentler.OrderByDescending(f => f.Tarih).ToList();
        Filamentler.Clear();
        foreach (var f in liste) Filamentler.Add(f);

        ToplamGram = liste.Sum(f => f.ToplamGram);
        KalanGram = liste.Sum(f => f.KalanGram);
        HarcananGram = liste.Sum(f => f.HarcananGram);
        KullanimYuzde = ToplamGram > 0 ? (HarcananGram / ToplamGram) * 100 : 0;
    }

    private void Kaydet()
    {
        if (string.IsNullOrWhiteSpace(Duzenlenen.Ad)) return;

        using var db = new AppDbContext();
        if (Duzenlenen.Id == 0)
        {
            Duzenlenen.KalanGram = Duzenlenen.ToplamGram;
            db.Filamentler.Add(Duzenlenen);
        }
        else
        {
            var kayit = db.Filamentler.Find(Duzenlenen.Id);
            if (kayit is null) return;
            var fark = Duzenlenen.ToplamGram - kayit.ToplamGram;
            kayit.Ad = Duzenlenen.Ad;
            kayit.Renk = Duzenlenen.Renk;
            kayit.Marka = Duzenlenen.Marka;
            kayit.ToplamGram = Duzenlenen.ToplamGram;
            kayit.KalanGram = Math.Max(0, kayit.KalanGram + fark);
            kayit.BirimFiyat = Duzenlenen.BirimFiyat;
            kayit.Tedarikci = Duzenlenen.Tedarikci;
            kayit.Tarih = Duzenlenen.Tarih;
            db.Filamentler.Update(kayit);
        }
        db.SaveChanges();

        Duzenlenen = YeniKayit();
        Secili = null;
        Yukle();
    }

    private void Sil()
    {
        if (Secili is null) return;

        var sonuc = System.Windows.MessageBox.Show(
            $"\"{Secili.Ad}\" filamentini silmek istediğinize emin misiniz? Bu işlem geri alınamaz.",
            "Filamenti Sil", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question,
            System.Windows.MessageBoxResult.No);
        if (sonuc != System.Windows.MessageBoxResult.Yes) return;

        using var db = new AppDbContext();
        var kayit = db.Filamentler.Find(Secili.Id);
        if (kayit is not null)
        {
            db.Filamentler.Remove(kayit);
            db.SaveChanges();
        }
        Secili = null;
        Duzenlenen = YeniKayit();
        Yukle();
    }
}
