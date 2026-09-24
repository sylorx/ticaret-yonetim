using System.ComponentModel.DataAnnotations;

namespace TicaretYonetim.Models;

public class Urun
{
    public int Id { get; set; }

    [Required]
    public string Ad { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    public UrunTuru Tur { get; set; } = UrunTuru.Baski3D;

    public string? Kategori { get; set; }

    public decimal BirimMaliyet { get; set; }

    public decimal SatisFiyati { get; set; }

    public int StokAdedi { get; set; }

    public string? ResimYolu { get; set; }

    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    public bool Aktif { get; set; } = true;

    public List<Satis> Satislar { get; set; } = new();

    public List<Alis> Alislar { get; set; } = new();
}
