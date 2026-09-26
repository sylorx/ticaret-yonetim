using System.Text.Json;
using TicaretYonetim.Data;
using TicaretYonetim.Models;

namespace TicaretYonetim.Services;

public static class VeriGirisi
{
    public static (bool kaydedildi, string sonuc) KomutuDene(string cevap)
    {
        try
        {
            var bas = cevap.IndexOf('{');
            var son = cevap.LastIndexOf('}');
            if (bas < 0 || son <= bas) return (false, string.Empty);
            var json = cevap.Substring(bas, son - bas + 1);
            using var doc = JsonDocument.Parse(json);
            var v = doc.RootElement;
            var islem = Str(v, "islem", "").ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(islem)) return (false, string.Empty);
            var uygulamaSonucu = Uygula(v, islem);
            return (uygulamaSonucu.Length > 0, uygulamaSonucu);
        }
        catch
        {
            return (false, string.Empty);
        }
    }

    public static string Uygula(JsonElement v, string islem)
    {
        try
        {
            if (islem.Contains("filament"))
                return FilamentEkle(v);
            if (islem.Contains("urun"))
                return UrunEkle(v);
            if (islem.Contains("satis"))
                return SatisEkle(v);
            if (islem.Contains("alis"))
                return AlisEkle(v);
            return "";
        }
        catch (Exception ex)
        {
            return $"⚠️ Kayıt yapılamadı: {ex.Message}";
        }
    }

    private static string FilamentEkle(JsonElement v)
    {
        var ad = Str(v, "ad", "Filament");
        var renk = Str(v, "renk");
        var marka = Str(v, "marka");
        var tedarikci = Str(v, "tedarikci");
        var adet = Math.Max(1, (int)Dec(v, "adet", 1));
        var gram = Dec(v, "gram", 1000);
        if (gram <= 0) gram = Dec(v, "miktar", 1000);
        var toplamTutar = DecAlias(v, "toplam_tutar", "toplam_takip", "toplam", "tutar", "fiyat", "odeme", "toplam_fiyat");
        if (toplamTutar <= 0) toplamTutar = Dec(v, "fiyat", 0);
        var birimFiyat = toplamTutar / adet;

        using var db = new AppDbContext();
        for (var i = 0; i < adet; i++)
        {
            db.Filamentler.Add(new Filament
            {
                Ad = ad,
                Renk = string.IsNullOrWhiteSpace(renk) ? null : renk,
                Marka = string.IsNullOrWhiteSpace(marka) ? null : marka,
                Tedarikci = string.IsNullOrWhiteSpace(tedarikci) ? null : tedarikci,
                ToplamGram = gram,
                KalanGram = gram,
                BirimFiyat = birimFiyat,
                Tarih = DateTime.Now,
                Aktif = true
            });
        }
        db.SaveChanges();
        return $"✅ Kaydedildi: {adet} makara '{ad}'{(string.IsNullOrWhiteSpace(renk) ? "" : $" ({renk})")} — her biri {gram:N0} g, toplam {toplamTutar:N2} ₺. Filamentler sayfasında görebilirsiniz.";
    }

    private static string UrunEkle(JsonElement v)
    {
        var ad = Str(v, "ad", "Ürün");
        using var db = new AppDbContext();
        var urun = new Urun
        {
            Ad = ad,
            Aciklama = Str(v, "aciklama"),
            Kategori = Str(v, "kategori"),
            Tur = Enum.TryParse<UrunTuru>(Str(v, "tur", "Baski3D"), true, out var tur) ? tur : UrunTuru.Baski3D,
            BirimMaliyet = DecAlias(v, "maliyet", "birim_maliyet", "malzeme_maliyeti"),
            SatisFiyati = DecAlias(v, "satis", "satis_fiyati", "fiyat"),
            StokAdedi = (int)Dec(v, "stok", 0),
            FilamentGram = DecAlias(v, "filament_gram", "gram", "filament"),
            OlusturmaTarihi = DateTime.Now,
            Aktif = true
        };
        var filamentAdi = Str(v, "filament");
        if (!string.IsNullOrWhiteSpace(filamentAdi))
        {
            var f = db.Filamentler.FirstOrDefault(f => f.Aktif && f.Ad.ToLower().Contains(filamentAdi.ToLower()));
            if (f is not null) urun.FilamentId = f.Id;
        }
        db.Urunler.Add(urun);
        db.SaveChanges();
        return $"✅ Kaydedildi: '{ad}' ürünü eklendi (maliyet {urun.BirimMaliyet:N2} ₺, satış {urun.SatisFiyati:N2} ₺, stok {urun.StokAdedi}). Ürünler sayfasında görebilirsiniz.";
    }

    private static string SatisEkle(JsonElement v)
    {
        var urunAdi = Str(v, "urun_ad", Str(v, "urun", ""));
        var adet = Math.Max(1, (int)Dec(v, "adet", 1));
        var kanal = Str(v, "kanal", "Website");
        var musteri = Str(v, "musteri");

        using var db = new AppDbContext();
        var urun = db.Urunler.FirstOrDefault(u => u.Aktif && u.Ad.ToLower().Contains(urunAdi.ToLower()));
        if (urun is null)
            return $"⚠️ '{urunAdi}' adında bir ürün bulunamadı. Önce Ürünler sayfasından ekleyin.";

        var birimFiyat = DecAlias(v, "birim_fiyat", "fiyat", "satis");
        if (birimFiyat <= 0) birimFiyat = urun.SatisFiyati;

        db.Satislar.Add(new Satis
        {
            UrunId = urun.Id,
            Adet = adet,
            BirimFiyat = birimFiyat,
            ToplamTutar = birimFiyat * adet,
            Kanal = Enum.TryParse<SatisKanali>(kanal, true, out var k) ? k : SatisKanali.Website,
            Musteri = string.IsNullOrWhiteSpace(musteri) ? null : musteri,
            Tarih = DateTime.Now
        });

        urun.StokAdedi = Math.Max(0, urun.StokAdedi - adet);
        db.Urunler.Update(urun);

        if (urun.FilamentId is int filamentId && urun.FilamentGram > 0)
        {
            var filament = db.Filamentler.Find(filamentId);
            if (filament is not null)
            {
                filament.KalanGram = Math.Max(0, filament.KalanGram - urun.FilamentGram * adet);
                db.Filamentler.Update(filament);
            }
        }
        db.SaveChanges();
        return $"✅ Kaydedildi: {urun.Ad} x{adet} = {birimFiyat * adet:N2} ₺ satış girildi ({kanal}). Stok ve filament düşüldü.";
    }

    private static string AlisEkle(JsonElement v)
    {
        var aciklama = Str(v, "aciklama", "Alış");
        var tutar = DecAlias(v, "tutar", "toplam_tutar", "fiyat", "toplam");
        var tedarikci = Str(v, "tedarikci");

        using var db = new AppDbContext();
        db.Alislar.Add(new Alis
        {
            Aciklama = aciklama,
            ToplamTutar = tutar,
            Tedarikci = string.IsNullOrWhiteSpace(tedarikci) ? null : tedarikci,
            Miktar = 1,
            Tarih = DateTime.Now
        });
        db.SaveChanges();
        return $"✅ Kaydedildi: '{aciklama}' alış kaydı ({tutar:N2} ₺) eklendi. Alışlar sayfasında görebilirsiniz.";
    }

    private static string Str(JsonElement v, string ad, string varsayilan = "")
    {
        if (v.TryGetProperty(ad, out var e) && e.ValueKind == JsonValueKind.String)
            return e.GetString() ?? varsayilan;
        return varsayilan;
    }

    private static decimal Dec(JsonElement v, string ad, decimal varsayilan = 0)
    {
        if (!v.TryGetProperty(ad, out var e)) return varsayilan;
        if (e.ValueKind == JsonValueKind.Number && e.TryGetDecimal(out var d)) return d;
        if (e.ValueKind == JsonValueKind.String && decimal.TryParse(e.GetString(),
                System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var s)) return s;
        return varsayilan;
    }

    private static decimal DecAlias(JsonElement v, params string[] adlar)
    {
        foreach (var ad in adlar)
        {
            var val = Dec(v, ad, -1m);
            if (val >= 0) return val;
        }
        return 0;
    }
}
