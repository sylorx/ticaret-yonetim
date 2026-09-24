﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace TicaretYonetim.Models;

public class Urun : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Bildir([CallerMemberName] string? adi = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(adi));

    public int Id { get; set; }

    [Required]
    public string Ad { get; set; } = string.Empty;

    public string? Aciklama { get; set; }

    public UrunTuru Tur { get; set; } = UrunTuru.Baski3D;

    public string? Kategori { get; set; }

    private decimal _birimMaliyet;
    public decimal BirimMaliyet
    {
        get => _birimMaliyet;
        set
        {
            if (_birimMaliyet == value) return;
            _birimMaliyet = value;
            Bildir();
            Bildir(nameof(KarTutari));
            Bildir(nameof(KarMarjiYuzde));
        }
    }

    private decimal _satisFiyati;
    public decimal SatisFiyati
    {
        get => _satisFiyati;
        set
        {
            if (_satisFiyati == value) return;
            _satisFiyati = value;
            Bildir();
            Bildir(nameof(KarTutari));
            Bildir(nameof(KarMarjiYuzde));
        }
    }

    public int StokAdedi { get; set; }

    public decimal FilamentGram { get; set; }

    public int? FilamentId { get; set; }

    public Filament? Filament { get; set; }

    public string? ResimYolu { get; set; }

    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    public bool Aktif { get; set; } = true;

    public List<Satis> Satislar { get; set; } = new();

    public List<Alis> Alislar { get; set; } = new();

    [NotMapped]
    public decimal KarTutari => SatisFiyati - BirimMaliyet;

    [NotMapped]
    public decimal KarMarjiYuzde => SatisFiyati > 0 ? (KarTutari / SatisFiyati) * 100 : 0;
}
