using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TicaretYonetim.Data;
using TicaretYonetim.Models;

namespace TicaretYonetim.Services;

public static class VeriGirisi
{
    public static (bool kaydedildi, string sonuc) KomutuDene(string cevap, string kullaniciMesaji = "")
    {
        var sonuclar = new List<string>();
        foreach (var parca in JsonParcalariBul(cevap))
        {
            try
            {
                using var doc = JsonDocument.Parse(parca);
                var v = doc.RootElement;
                if (v.ValueKind != JsonValueKind.Object) continue;
                var islem = IslemAdi(v);
                if (string.IsNullOrWhiteSpace(islem)) continue;
                var r = Uygula(v, islem, kullaniciMesaji);
                if (!string.IsNullOrEmpty(r)) sonuclar.Add(r);
            }
            catch { }
        }
        return (sonuclar.Count > 0, string.Join("\n", sonuclar));
    }

    private static List<string> JsonParcalariBul(string metin)
    {
        var liste = new List<string>();
        var derinlik = 0;
        var bas = -1;
        var metinIci = false;
        var kacis = false;
        for (var i = 0; i < metin.Length; i++)
        {
            var c = metin[i];
            if (metinIci)
            {
                if (kacis) kacis = false;
                else if (c == '\\') kacis = true;
                else if (c == '"') metinIci = false;
                continue;
            }
            if (c == '"') { metinIci = true; continue; }
            if (c == '{')
            {
                if (derinlik == 0) bas = i;
                derinlik++;
            }
            else if (c == '}' && derinlik > 0)
            {
                derinlik--;
                if (derinlik == 0 && bas >= 0)
                {
                    liste.Add(metin.Substring(bas, i - bas + 1));
                    bas = -1;
                }
            }
        }
        return liste;
    }

    private static string Normallestir(string s) => s
        .ToLowerInvariant()
        .Replace('ı', 'i').Replace('ş', 's').Replace('ğ', 'g')
        .Replace('ü', 'u').Replace('ö', 'o').Replace('ç', 'c');

    private static string IslemAdi(JsonElement v)
    {
        foreach (var ad in new[] { "islem", "komut", "eylem", "action", "islem_turu", "veri_turu" })
        {
            var s = Str(v, ad, "");
            if (!string.IsNullOrWhiteSpace(s)) return Normallestir(s);
        }
        return "";
    }

    public static string Uygula(JsonElement v, string islem, string kullaniciMesaji = "")
    {
        try
        {
            var i = Normallestir(islem);
            var stokIslemi = i.Contains("stok") && !i.Contains("urun_sil");
            var silme = !stokIslemi && (i.Contains("sil") || i.Contains("kaldir") || i.Contains("delete") || i.Contains("cikar"));
            var guncelleme = !silme && (i.Contains("guncelle") || i.Contains("update") || i.Contains("duzenle") || i.Contains("degistir"));

            if (silme)
            {
                var kullaniyorMu = Normallestir(kullaniciMesaji);
                var izinli = kullaniyorMu.Contains("sil") || kullaniyorMu.Contains("kaldir") ||
                             kullaniyorMu.Contains("cikar") || kullaniyorMu.Contains("iptal") ||
                             kullaniyorMu.Contains("geri") || string.IsNullOrEmpty(kullaniciMesaji);
                if (!izinli)
                    return "⚠️ Silme işlemi için lütfen açıkça 'sil' veya 'kaldır' yazın. Bir şey silinmedi.";
            }

            var urunIleIlgili = i.Contains("urun") || i.Contains("fiyat") || i.Contains("stok");
            if (i.Contains("filament"))
                return silme ? FilamentSil(v) : guncelleme ? FilamentGuncelle(v) : FilamentEkle(v);
            if (i.Contains("satis") && !i.Contains("fiyat") && !i.Contains("urun"))
                return silme ? SatisSil(v) : guncelleme ? "⚠️ Satış kaydı güncellenemiyor; silip yeniden ekleyebilirsiniz." : SatisEkle(v);
            if (i.Contains("alis"))
                return silme ? AlisSil(v) : guncelleme ? "⚠️ Alış kaydı güncellenemiyor; silip yeniden ekleyebilirsiniz." : AlisEkle(v);
            if (urunIleIlgili)
                return silme ? UrunSil(v)
                    : guncelleme || i.Contains("stok") ? UrunGuncelle(v, i)
                    : UrunEkle(v);
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

    private static string UrunBulAd(JsonElement v)
    {
        foreach (var ad in new[] { "urun_ad", "urun_adi", "ad", "isim", "urun", "urun_adlari" })
        {
            var s = Str(v, ad, "");
            if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
        }
        return "";
    }

    private static string UrunSil(JsonElement v)
    {
        var ad = UrunBulAd(v);
        using var db = new AppDbContext();
        var urun = db.Urunler.FirstOrDefault(u => u.Aktif && u.Ad.ToLower().Contains(ad.ToLower()));
        if (urun is null) return $"⚠️ '{ad}' adında aktif bir ürün bulunamadı.";
        urun.Aktif = false;
        db.Urunler.Update(urun);
        db.SaveChanges();
        return $"✅ Kaldırıldı: '{urun.Ad}' ürünü artık listede görünmeyecek (geçmiş satışları korunur). İsterseniz Ürünler sayfasından geri getirebilirsiniz.";
    }

    private static string UrunGuncelle(JsonElement v, string islem = "")
    {
        var ad = UrunBulAd(v);
        using var db = new AppDbContext();
        var urun = db.Urunler.Where(u => u.Ad.ToLower().Contains(ad.ToLower()))
            .OrderByDescending(u => u.Aktif).FirstOrDefault();
        if (urun is null) return $"⚠️ '{ad}' adında bir ürün bulunamadı.";

        var degisen = new List<string>();
        var maliyet = DecAlias(v, "maliyet", "birim_maliyet");
        if (maliyet > 0) { urun.BirimMaliyet = maliyet; degisen.Add($"maliyet {maliyet:N2} ₺"); }
        var satis = DecAlias(v, "satis_fiyati", "fiyat", "satis");
        if (satis > 0) { urun.SatisFiyati = satis; degisen.Add($"satış fiyatı {satis:N2} ₺"); }
        var stok = Dec(v, "stok", -1);
        if (stok >= 0 && v.TryGetProperty("stok", out _))
        {
            if (islem.Contains("ekle") || islem.Contains("cikar") || islem.Contains("kaldir"))
            {
                var isaret = islem.Contains("cikar") || islem.Contains("kaldir") ? -1 : 1;
                urun.StokAdedi = Math.Max(0, urun.StokAdedi + (int)stok * isaret);
                degisen.Add($"stok {urun.StokAdedi}");
            }
            else { urun.StokAdedi = (int)stok; degisen.Add($"stok {urun.StokAdedi}"); }
        }
        var stokDegisim = Dec(v, "stok_degisim", 0);
        if (stokDegisim != 0) { urun.StokAdedi = Math.Max(0, urun.StokAdedi + (int)stokDegisim); degisen.Add($"stok {urun.StokAdedi}'e güncellendi"); }
        var gram = DecAlias(v, "filament_gram", "gram");
        if (gram > 0) { urun.FilamentGram = gram; degisen.Add($"filament {gram:N0} g"); }
        var kategori = Str(v, "kategori");
        if (!string.IsNullOrWhiteSpace(kategori)) { urun.Kategori = kategori; degisen.Add($"kategori {kategori}"); }
        var aciklama = Str(v, "aciklama");
        if (!string.IsNullOrWhiteSpace(aciklama)) { urun.Aciklama = aciklama; degisen.Add("açıklama güncellendi"); }

        if (degisen.Count == 0) return $"⚠️ '{urun.Ad}' için güncellenecek alan bulunamadı.";
        db.Urunler.Update(urun);
        db.SaveChanges();
        return $"✅ Güncellendi: '{urun.Ad}' — {string.Join(", ", degisen)}.";
    }

    private static string FilamentSil(JsonElement v)
    {
        var ad = UrunBulAd(v);
        using var db = new AppDbContext();
        var q = db.Filamentler.Where(f => f.Aktif);
        Filament? filament = null;
        if (!string.IsNullOrWhiteSpace(ad))
            filament = q.FirstOrDefault(f => f.Ad.ToLower().Contains(ad.ToLower()) ||
                                             (f.Renk != null && f.Renk.ToLower().Contains(ad.ToLower())));
        filament ??= q.OrderByDescending(f => f.Id).FirstOrDefault();
        if (filament is null) return "⚠️ Silinecek aktif filament bulunamadı.";
        filament.Aktif = false;
        db.Filamentler.Update(filament);
        db.SaveChanges();
        return $"✅ Kaldırıldı: '{filament.Ad}'{(string.IsNullOrWhiteSpace(filament.Renk) ? "" : $" ({filament.Renk})")} filamenti listeden çıkarıldı.";
    }

    private static string FilamentGuncelle(JsonElement v)
    {
        var ad = UrunBulAd(v);
        using var db = new AppDbContext();
        var filament = db.Filamentler.FirstOrDefault(f => f.Aktif && (f.Ad.ToLower().Contains(ad.ToLower()) ||
                                     (f.Renk != null && f.Renk.ToLower().Contains(ad.ToLower()))))
            ?? db.Filamentler.Where(f => f.Aktif).OrderByDescending(f => f.Id).FirstOrDefault();
        if (filament is null) return "⚠️ Güncellenecek filament bulunamadı.";

        var degisen = new List<string>();
        if (v.TryGetProperty("kalan_gram", out _))
        {
            var kg = Dec(v, "kalan_gram");
            filament.KalanGram = kg;
            degisen.Add($"kalan {kg:N0} g");
        }
        var eksilen = Dec(v, "kullanim", 0);
        if (eksilen > 0) { filament.KalanGram = Math.Max(0, filament.KalanGram - eksilen); degisen.Add($"{eksilen:N0} g düşüldü, kalan {filament.KalanGram:N0} g"); }
        var renk = Str(v, "renk");
        if (!string.IsNullOrWhiteSpace(renk)) { filament.Renk = renk; degisen.Add($"renk {renk}"); }
        var birim = DecAlias(v, "birim_fiyat", "fiyat");
        if (birim > 0) { filament.BirimFiyat = birim; degisen.Add($"birim fiyat {birim:N2} ₺"); }

        if (degisen.Count == 0) return $"⚠️ '{filament.Ad}' için güncellenecek alan bulunamadı (kalan_gram, kullanim, renk, birim_fiyat verebilirsiniz).";
        db.Filamentler.Update(filament);
        db.SaveChanges();
        return $"✅ Güncellendi: '{filament.Ad}' — {string.Join(", ", degisen)}.";
    }

    private static string SatisSil(JsonElement v)
    {
        var ad = UrunBulAd(v);
        using var db = new AppDbContext();
        var q = db.Satislar.Include(s => s.Urun).AsEnumerable();
        var satis = string.IsNullOrWhiteSpace(ad)
            ? q.OrderByDescending(s => s.Tarih).FirstOrDefault()
            : q.OrderByDescending(s => s.Tarih).FirstOrDefault(s => s.Urun?.Ad.ToLower().Contains(ad.ToLower()) == true);
        if (satis is null) return $"⚠️ '{ad}' için satış kaydı bulunamadı.";
        var urun = db.Urunler.Find(satis.UrunId);
        if (urun is not null) { urun.StokAdedi += satis.Adet; db.Urunler.Update(urun); }
        db.Satislar.Remove(satis);
        db.SaveChanges();
        return $"✅ Silindi: {satis.Tarih:dd.MM.yyyy} tarihli '{satis.Urun?.Ad}' x{satis.Adet} ({satis.ToplamTutar:N2} ₺) satışı silindi, stok {satis.Adet} adet geri eklendi.";
    }

    private static string AlisSil(JsonElement v)
    {
        var ad = UrunBulAd(v);
        var tutar = Dec(v, "tutar", 0);
        using var db = new AppDbContext();
        var q = db.Alislar.OrderByDescending(a => a.Tarih).AsEnumerable();
        Alis? alis = null;
        if (tutar > 0) alis = q.FirstOrDefault(a => a.ToplamTutar == tutar);
        if (alis is null && !string.IsNullOrWhiteSpace(ad) && ad != "Alış")
            alis = q.FirstOrDefault(a => a.Aciklama.ToLower().Contains(ad.ToLower()) ||
                                         (a.Tedarikci?.ToLower().Contains(ad.ToLower()) ?? false));
        alis ??= q.FirstOrDefault();
        if (alis is null) return "⚠️ Silinecek alış kaydı bulunamadı.";
        db.Alislar.Remove(alis);
        db.SaveChanges();
        return $"✅ Silindi: '{alis.Aciklama}' ({alis.ToplamTutar:N2} ₺) alış kaydı silindi.";
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
