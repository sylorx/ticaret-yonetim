namespace TicaretYonetim.Models;

public class Alis
{
    public int Id { get; set; }

    public DateTime Tarih { get; set; } = DateTime.Now;

    public int? UrunId { get; set; }

    public Urun? Urun { get; set; }

    public string Aciklama { get; set; } = string.Empty;

    public string? Tedarikci { get; set; }

    public decimal Miktar { get; set; } = 1;

    public string? Birim { get; set; }

    public decimal ToplamTutar { get; set; }

    public string? Not { get; set; }
}
