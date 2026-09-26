using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicaretYonetim.Data;
using TicaretYonetim.Models;
using TicaretYonetim.Services;

namespace TicaretYonetim.ViewModels;

public class AsistanViewModel : ViewModelBase
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(5) };
    private const string OllamaAdres = "http://localhost:11434";

    public ObservableCollection<SohbetMesaji> Mesajlar { get; } = new();
    public ObservableCollection<string> Modeller { get; } = new();

    public OllamaAyarlari Ayar { get; } = OllamaAyarlari.Yukle();

    public string[] GpuModuSecenekleri => OllamaAyarlari.GpuModuSecenekleri;
    public int[] ContextSecenekleri => OllamaAyarlari.ContextSecenekleri;
    public string[] KvCacheSecenekleri => OllamaAyarlari.KvCacheSecenekleri;
    public int[] ParalelSecenekleri => OllamaAyarlari.ParalelSecenekleri;
    public int[] MaxModelSecenekleri => OllamaAyarlari.MaxModelSecenekleri;
    public string[] KeepAliveSecenekleri => OllamaAyarlari.KeepAliveSecenekleri;

    private string _ayarDurumu = string.Empty;
    public string AyarDurumu
    {
        get => _ayarDurumu;
        set => SetField(ref _ayarDurumu, value);
    }

    private string _seciliModel = string.Empty;
    public string SeciliModel
    {
        get => _seciliModel;
        set => SetField(ref _seciliModel, value);
    }

    private string _girilenMesaj = string.Empty;
    public string GirilenMesaj
    {
        get => _girilenMesaj;
        set => SetField(ref _girilenMesaj, value);
    }

    private bool _veriBaglamiDahil = true;
    public bool VeriBaglamiDahil
    {
        get => _veriBaglamiDahil;
        set => SetField(ref _veriBaglamiDahil, value);
    }

    private int _contextMesajSayisi = 20;
    public int ContextMesajSayisi
    {
        get => _contextMesajSayisi;
        set => SetField(ref _contextMesajSayisi, value);
    }

    private bool _mesajGonderiliyor;
    public bool MesajGonderiliyor
    {
        get => _mesajGonderiliyor;
        set => SetField(ref _mesajGonderiliyor, value);
    }

    private string _durum = "Ollama'ya bağlanılıyor...";
    public string Durum
    {
        get => _durum;
        set => SetField(ref _durum, value);
    }

    private bool _ollamaBagli;
    public bool OllamaBagli
    {
        get => _ollamaBagli;
        set => SetField(ref _ollamaBagli, value);
    }

    public RelayCommand GonderCommand { get; }
    public RelayCommand TemizleCommand { get; }
    public RelayCommand ModelleriYenileCommand { get; }
    public RelayCommand AyarKaydetCommand { get; }

    public AsistanViewModel()
    {
        GonderCommand = new RelayCommand(async _ => await Gonder(), _ => !MesajGonderiliyor);
        TemizleCommand = new RelayCommand(_ => Temizle());
        ModelleriYenileCommand = new RelayCommand(async _ => await ModelleriYukle());
        AyarKaydetCommand = new RelayCommand(async _ => await AyarKaydet());
    }

    private async Task AyarKaydet()
    {
        Ayar.Kaydet();
        Ayar.Uygula();
        AyarDurumu = "Ayarlar kaydedildi. Ollama yeniden başlatılıyor...";
        await Task.Run(OllamaAyarlari.OllamaYenidenBaslat);
        await Task.Delay(3000);
        await ModelleriYukle();
        AyarDurumu = $"Ayarlar uygulandı ve Ollama yeniden başlatıldı. ({Ayar.GpuModu}, bağlam {Ayar.ContextLength}, {Ayar.KvCacheTipi})";
    }

    public async Task ModelleriYukle()
    {
        try
        {
            Durum = "Ollama'ya bağlanılıyor...";
            var yanit = await Http.GetStringAsync($"{OllamaAdres}/api/tags");
            using var doc = JsonDocument.Parse(yanit);
            Modeller.Clear();
            foreach (var m in doc.RootElement.GetProperty("models").EnumerateArray())
            {
                var ad = m.GetProperty("name").GetString();
                if (!string.IsNullOrWhiteSpace(ad)) Modeller.Add(ad);
            }
            if (Modeller.Count == 0)
            {
                Durum = "Hiç model bulunamadı. Ollama'ya model indirin (örn: ollama pull phi3).";
                OllamaBagli = false;
                return;
            }
            if (!Modeller.Contains(SeciliModel))
                SeciliModel = Modeller.FirstOrDefault(m => m.Contains("phi3") || m.Contains("gemma")) ?? Modeller[0];
            OllamaBagli = true;
            Durum = $"Bağlı: {Modeller.Count} model bulundu. Seçili: {SeciliModel}";
        }
        catch (Exception ex)
        {
            OllamaBagli = false;
            Durum = $"Ollama'ya bağlanılamadı: {ex.Message}";
        }
    }

    public void Yukle()
    {
        using var db = new AppDbContext();
        Mesajlar.Clear();
        foreach (var m in db.SohbetMesajlari.OrderBy(m => m.Tarih).ToList())
            Mesajlar.Add(m);
    }

    private async Task Gonder()
    {
        var metin = GirilenMesaj?.Trim();
        if (string.IsNullOrWhiteSpace(metin) || MesajGonderiliyor) return;

        if (!OllamaBagli)
        {
            await ModelleriYukle();
            if (!OllamaBagli) return;
        }

        GirilenMesaj = string.Empty;
        MesajGonderiliyor = true;
        Bildir(nameof(GonderCommand));

        var kullaniciMesaji = new SohbetMesaji { Rol = "user", Icerik = metin, Tarih = DateTime.Now };
        Mesajlar.Add(kullaniciMesaji);
        Kaydet(kullaniciMesaji);

        try
        {
            Durum = $"{SeciliModel} düşünüyor...";
            var cevap = await OllamaSoru(metin);

            var (kaydedildi, sonuc) = VeriGirisi.KomutuDene(cevap);
            if (kaydedildi)
            {
                cevap = sonuc + "\n\n(Bilgiyi programa ben kaydettim — ilgili sayfada görebilirsiniz.)";
            }

            var asistanMesaji = new SohbetMesaji { Rol = "assistant", Icerik = cevap, Tarih = DateTime.Now };
            Mesajlar.Add(asistanMesaji);
            Kaydet(asistanMesaji);
            Durum = $"Yanıt hazır ({SeciliModel})";
        }
        catch (Exception ex)
        {
            var hata = new SohbetMesaji { Rol = "assistant", Icerik = $"⚠️ Hata: {ex.Message}", Tarih = DateTime.Now };
            Mesajlar.Add(hata);
            Durum = "Hata oluştu";
        }
        finally
        {
            MesajGonderiliyor = false;
            Bildir(nameof(GonderCommand));
        }
    }

    private async Task<string> OllamaSoru(string soru)
    {
        var mesajlar = new List<object>();

        if (VeriBaglamiDahil)
        {
            mesajlar.Add(new { role = "system", content = VeriOzetiOlustur() });
        }

        var gecmis = Mesajlar
            .TakeLast(Math.Max(ContextMesajSayisi, 1))
            .ToList();

        foreach (var m in gecmis)
        {
            if (m.Rol == "user" || m.Rol == "assistant")
                mesajlar.Add(new { role = m.Rol, content = m.Icerik });
        }

        var istek = new
        {
            model = SeciliModel,
            messages = mesajlar,
            stream = false
        };

        var json = JsonSerializer.Serialize(istek);
        using var icerik = new StringContent(json, Encoding.UTF8, "application/json");
        using var yanit = await Http.PostAsync($"{OllamaAdres}/api/chat", icerik);
        yanit.EnsureSuccessStatusCode();

        var yanitJson = await yanit.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(yanitJson);
        return doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? "(boş yanıt)";
    }

    private static void Kaydet(SohbetMesaji mesaj)
    {
        using var db = new AppDbContext();
        db.SohbetMesajlari.Add(mesaj);
        db.SaveChanges();
    }

    private void Temizle()
    {
        var sonuc = System.Windows.MessageBox.Show(
            "Tüm sohbet geçmişi silinecek. Emin misiniz?",
            "Sohbeti Temizle", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question,
            System.Windows.MessageBoxResult.No);
        if (sonuc != System.Windows.MessageBoxResult.Yes) return;

        using var db = new AppDbContext();
        db.SohbetMesajlari.RemoveRange(db.SohbetMesajlari);
        db.SaveChanges();
        Mesajlar.Clear();
        Durum = "Sohbet geçmişi temizlendi.";
    }

    private static string VeriOzetiOlustur()
    {
        using var db = new AppDbContext();
        var satislar = db.Satislar.Include(s => s.Urun).ToList();
        var alislar = db.Alislar.ToList();
        var urunler = db.Urunler.ToList();
        var filamentler = db.Filamentler.ToList();

        var toplamCiro = satislar.Sum(s => s.ToplamTutar);
        var toplamGider = alislar.Sum(a => a.ToplamTutar);
        var netKar = toplamCiro - toplamGider;
        var buAy = DateTime.Now;
        var buAySatis = satislar.Where(s => s.Tarih.Year == buAy.Year && s.Tarih.Month == buAy.Month).Sum(s => s.ToplamTutar);
        var marj = toplamCiro > 0 ? (netKar / toplamCiro) * 100 : 0;

        var sb = new StringBuilder();
        sb.AppendLine("Sen 'Ticaret Yönetim' adlı 3D baskı işletme takip programının yapay zeka asistanısın.");
        sb.AppendLine("Kullanıcı Türkçe konuşur, sen de Türkçe yanıt ver. Kısa, net ve sayılarla destekli cevaplar ver.");
        sb.AppendLine();
        sb.AppendLine("ÖNEMLİ - KAYIT PROTOKOLÜ: Senden bir bilgiyi programa GİRMEN istendiğinde (ör. 'filament aldım', 'ürün ekle', 'satış yaptım', 'alış kaydet'), sen kaydedemezsin; bunun yerine programın kaydetmesi için SONA tek bir JSON komutu ekle. Biçim:");
        sb.AppendLine("Filament: {\"islem\":\"filament_ekle\",\"ad\":\"PLA Siyah\",\"renk\":\"Siyah\",\"marka\":\"\",\"adet\":3,\"gram\":1000,\"toplam_tutar\":400,\"tedarikci\":\"\"}");
        sb.AppendLine("Ürün: {\"islem\":\"urun_ekle\",\"ad\":\"Kupa\",\"aciklama\":\"\",\"kategori\":\"\",\"tur\":\"Baski3D\",\"maliyet\":100,\"satis\":250,\"stok\":10,\"filament_gram\":50,\"filament\":\"PLA Siyah\"}");
        sb.AppendLine("Satış: {\"islem\":\"satis_ekle\",\"urun_ad\":\"Kupa\",\"adet\":2,\"birim_fiyat\":250,\"kanal\":\"Website\",\"musteri\":\"\"}");
        sb.AppendLine("Alış: {\"islem\":\"alis_ekle\",\"aciklama\":\"Malzeme\",\"tutar\":100,\"tedarikci\":\"\"}");
        sb.AppendLine("Kural: Sadece kullanıcı AÇIKÇA 'gir', 'kaydet', 'ekle', 'yaz' gibi bir kayıt emri verdiğinde JSON üret. Sadece soru sorduysa JSON ÜRETME, veriye bakıp cevapla. JSON'da sayıları tırnaksız yaz. Kullanıcı '3 tane ... 400 tl' dediyse: adet=3, toplam_tutar=400. JSON'u mesajın en sonuna tek satır olarak koy, öncesinde 1-2 cümle Türkçe açıklama yazabilirsin.");
        sb.AppendLine("Aşağıdaki veriler programın veritabanından alınmış GÜNCEL gerçek verilerdir. Soruları bu verilere göre cevapla:");
        sb.AppendLine();
        sb.AppendLine($"- Toplam satış cirosu: {toplamCiro:N2} ₺");
        sb.AppendLine($"- Toplam alış gideri: {toplamGider:N2} ₺");
        sb.AppendLine($"- Net kâr: {netKar:N2} ₺");
        sb.AppendLine($"- Bu ay ({buAy:MMMM yyyy}) satış: {buAySatis:N2} ₺");
        sb.AppendLine($"- Ortalama kâr marjı: %{marj:N1}");
        sb.AppendLine($"- Toplam satılan adet: {satislar.Sum(s => s.Adet)}");
        sb.AppendLine($"- Aktif ürün sayısı: {urunler.Count(u => u.Aktif)}");
        sb.AppendLine($"- Filament: toplam {filamentler.Sum(f => f.ToplamGram):N0} g, kalan {filamentler.Sum(f => f.KalanGram):N0} g, harcanan {filamentler.Sum(f => f.HarcananGram):N0} g");

        if (urunler.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Ürünler:");
            foreach (var u in urunler.Take(15))
                sb.AppendLine($"  - {u.Ad} (maliyet {u.BirimMaliyet:N2} ₺, satış {u.SatisFiyati:N2} ₺, stok {u.StokAdedi}, filament {u.FilamentGram:N0} g/adet)");
        }

        if (satislar.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Son satışlar:");
            foreach (var s in satislar.OrderByDescending(s => s.Tarih).Take(10))
                sb.AppendLine($"  - {s.Tarih:dd.MM.yyyy}: {s.Urun?.Ad ?? "?"} x{s.Adet} = {s.ToplamTutar:N2} ₺ ({s.Kanal})");
        }

        sb.AppendLine();
        sb.AppendLine("Kullanıcı 'ne kadar kar ettik', 'bu ay ne sattık', 'filament ne kadar kaldı' gibi sorular sorabilir. Veriler yoksa dürüstçe 'henüz veri yok' de.");
        return sb.ToString();
    }
}
