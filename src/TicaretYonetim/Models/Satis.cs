namespace TicaretYonetim.Models;

public class Satis
{
    public int Id { get; set; }

    public DateTime Tarih { get; set; } = DateTime.Now;

    public int UrunId { get; set; }

    public Urun? Urun { get; set; }

    public string? Musteri { get; set; }

    public int Adet { get; set; } = 1;

    public decimal BirimFiyat { get; set; }

    public decimal ToplamTutar { get; set; }

    public SatisKanali Kanal { get; set; } = SatisKanali.Website;

    public string? Not { get; set; }
}
