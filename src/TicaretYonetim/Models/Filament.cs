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
}
