using System.ComponentModel.DataAnnotations.Schema;

namespace TicaretYonetim.Models;

public class Filament
{
    public int Id { get; set; }

    public string Ad { get; set; } = string.Empty;

    public string? Renk { get; set; }

    public string? Marka { get; set; }

    public decimal ToplamGram { get; set; }

    public decimal KalanGram { get; set; }

    public decimal BirimFiyat { get; set; }

    public string? Tedarikci { get; set; }

    public DateTime Tarih { get; set; } = DateTime.Now;

    public bool Aktif { get; set; } = true;

    public List<Urun> Urunler { get; set; } = new();

    [NotMapped]
    public decimal HarcananGram => ToplamGram - KalanGram;

    [NotMapped]
    public decimal KullanimYuzde => ToplamGram > 0 ? (KalanGram / ToplamGram) * 100 : 0;

    [NotMapped]
    public System.Windows.Media.Brush? RenkFircasi
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Renk)) return null;
            var secenek = ViewModels.FilamentViewModel.RenkPaleti.FirstOrDefault(r =>
                string.Equals(r.Ad, Renk, StringComparison.OrdinalIgnoreCase));
            if (secenek is not null) return secenek.Firca;
            try
            {
                return new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Renk));
            }
            catch { return null; }
        }
    }
}
