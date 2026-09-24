﻿using System.ComponentModel.DataAnnotations.Schema;

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

    public string? SiparisNo { get; set; }

    public string? Not { get; set; }

    [NotMapped]
    public decimal BirimMaliyet => Urun?.BirimMaliyet ?? 0;

    [NotMapped]
    public decimal BirimKar => BirimFiyat - BirimMaliyet;

    [NotMapped]
    public decimal ToplamKar => BirimKar * Adet;

    [NotMapped]
    public decimal KarMarjiYuzde => BirimFiyat > 0 ? (BirimKar / BirimFiyat) * 100 : 0;
}
