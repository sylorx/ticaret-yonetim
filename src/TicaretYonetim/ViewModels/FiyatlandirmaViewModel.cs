using System.Collections.ObjectModel;
using TicaretYonetim.Data;
using TicaretYonetim.Models;

namespace TicaretYonetim.ViewModels;

public class FiyatlandirmaViewModel : ViewModelBase
{
    public ObservableCollection<Urun> UrunSecenekleri { get; } = new();

    private Urun? _seciliUrun;
    public Urun? SeciliUrun
    {
        get => _seciliUrun;
        set
        {
            if (SetField(ref _seciliUrun, value) && value is not null)
            {
                FilamentGram = value.FilamentGram;
                Hesapla();
            }
        }
    }

    private decimal _filamentGram = 100;
    public decimal FilamentGram { get => _filamentGram; set { if (SetField(ref _filamentGram, value)) Hesapla(); } }

    private decimal _filamentFiyatKg = 600;
    public decimal FilamentFiyatKg { get => _filamentFiyatKg; set { if (SetField(ref _filamentFiyatKg, value)) Hesapla(); } }

    private decimal _baskiSuresi = 4;
    public decimal BaskiSuresi { get => _baskiSuresi; set { if (SetField(ref _baskiSuresi, value)) Hesapla(); } }

    private decimal _yaziciGucu = 65;
    public decimal YaziciGucu { get => _yaziciGucu; set { if (SetField(ref _yaziciGucu, value)) Hesapla(); } }

    private decimal _bilgisayarGucu = 100;
    public decimal BilgisayarGucu { get => _bilgisayarGucu; set { if (SetField(ref _bilgisayarGucu, value)) Hesapla(); } }

    private decimal _elektrikBirimFiyat = 3.5m;
    public decimal ElektrikBirimFiyat { get => _elektrikBirimFiyat; set { if (SetField(ref _elektrikBirimFiyat, value)) Hesapla(); } }

    private decimal _isciSaatUcreti = 100;
    public decimal IsciSaatUcreti { get => _isciSaatUcreti; set { if (SetField(ref _isciSaatUcreti, value)) Hesapla(); } }

    private decimal _digerGiderler;
    public decimal DigerGiderler { get => _digerGiderler; set { if (SetField(ref _digerGiderler, value)) Hesapla(); } }

    private decimal _karMarji = 30;
    public decimal KarMarji { get => _karMarji; set { if (SetField(ref _karMarji, value)) Hesapla(); } }

    private decimal _filamentMaliyeti;
    public decimal FilamentMaliyeti { get => _filamentMaliyeti; set => SetField(ref _filamentMaliyeti, value); }

    private decimal _elektrikMaliyeti;
    public decimal ElektrikMaliyeti { get => _elektrikMaliyeti; set => SetField(ref _elektrikMaliyeti, value); }

    private decimal _isciMaliyeti;
    public decimal IsciMaliyeti { get => _isciMaliyeti; set => SetField(ref _isciMaliyeti, value); }

    private decimal _toplamGider;
    public decimal ToplamGider { get => _toplamGider; set => SetField(ref _toplamGider, value); }

    private decimal _onerilenFiyat;
    public decimal OnerilenFiyat { get => _onerilenFiyat; set => SetField(ref _onerilenFiyat, value); }

    private decimal _karTutari;
    public decimal KarTutari { get => _karTutari; set => SetField(ref _karTutari, value); }

    private decimal _karYuzde;
    public decimal KarYuzde { get => _karYuzde; set => SetField(ref _karYuzde, value); }

    public FiyatlandirmaViewModel()
    {
        Hesapla();
    }

    public void Yukle()
    {
        using var db = new AppDbContext();
        UrunSecenekleri.Clear();
        foreach (var u in db.Urunler.Where(u => u.Aktif).OrderBy(u => u.Ad).ToList())
            UrunSecenekleri.Add(u);
    }

    private void Hesapla()
    {
        FilamentMaliyeti = FilamentGram * (FilamentFiyatKg / 1000);
        ElektrikMaliyeti = ((YaziciGucu + BilgisayarGucu) / 1000) * BaskiSuresi * ElektrikBirimFiyat;
        IsciMaliyeti = BaskiSuresi * IsciSaatUcreti;
        ToplamGider = FilamentMaliyeti + ElektrikMaliyeti + IsciMaliyeti + DigerGiderler;
        OnerilenFiyat = ToplamGider * (1 + KarMarji / 100);
        KarTutari = OnerilenFiyat - ToplamGider;
        KarYuzde = ToplamGider > 0 ? (KarTutari / ToplamGider) * 100 : 0;
    }
}
