using System.IO;
using System.IO.Compression;
using Microsoft.Data.Sqlite;
using TicaretYonetim.Data;

namespace TicaretYonetim.Services;

public static class YedeklemeServisi
{
    public static string YedekKlasoru { get; } = Path.Combine(AppDbContext.VeriKlasoru, "Yedekler");

    private const int MaxYedekSayisi = 10;

    public static string YedekOlustur()
    {
        Directory.CreateDirectory(YedekKlasoru);
        var ad = $"yedek_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.db";
        var yol = Path.Combine(YedekKlasoru, ad);
        VeritabaniKopyala(AppDbContext.VeriTabaniYolu, yol);
        EskiYedekleriTemizle();
        return yol;
    }

    public static void VeritabaniKopyala(string kaynak, string hedef)
    {
        using var src = new SqliteConnection($"Data Source={kaynak};Pooling=False");
        src.Open();
        using var dst = new SqliteConnection($"Data Source={hedef};Pooling=False");
        dst.Open();
        src.BackupDatabase(dst);
    }

    public static List<string> YedekleriListele()
    {
        if (!Directory.Exists(YedekKlasoru)) return new List<string>();
        return Directory.GetFiles(YedekKlasoru, "*.db")
            .OrderByDescending(f => File.GetLastWriteTime(f))
            .ToList();
    }

    public static void EskiYedekleriTemizle()
    {
        var yedekler = YedekleriListele();
        foreach (var eski in yedekler.Skip(MaxYedekSayisi))
        {
            try { File.Delete(eski); } catch { }
        }
    }

    public static string DisariAktar(string hedefZip)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(hedefZip)!);
        using var zip = ZipFile.Open(hedefZip, ZipArchiveMode.Create);
        zip.CreateEntryFromFile(AppDbContext.VeriTabaniYolu, "ticaret.db");
        if (Directory.Exists(AppDbContext.ResimKlasoru))
        {
            foreach (var f in Directory.GetFiles(AppDbContext.ResimKlasoru))
                zip.CreateEntryFromFile(f, Path.Combine("Resimler", Path.GetFileName(f)));
        }
        return hedefZip;
    }

    public static void IceriAktar(string zipYol)
    {
        YedekOlustur();
        SqliteConnection.ClearAllPools();
        using var zip = ZipFile.OpenRead(zipYol);
        var dbEntry = zip.GetEntry("ticaret.db")
            ?? throw new InvalidOperationException("Geçersiz yedek dosyası: içinde ticaret.db bulunamadı.");
        dbEntry.ExtractToFile(AppDbContext.VeriTabaniYolu, overwrite: true);
        foreach (var e in zip.Entries)
        {
            if (e.FullName.StartsWith("Resimler/", StringComparison.OrdinalIgnoreCase) && !e.FullName.EndsWith("/"))
            {
                var hedef = Path.Combine(AppDbContext.ResimKlasoru, Path.GetFileName(e.FullName));
                Directory.CreateDirectory(AppDbContext.ResimKlasoru);
                e.ExtractToFile(hedef, overwrite: true);
            }
        }
    }

    public static void YedektenGeriYukle(string yedekYolu)
    {
        if (!File.Exists(yedekYolu)) throw new FileNotFoundException("Yedek dosyası bulunamadı.", yedekYolu);
        YedekOlustur();
        SqliteConnection.ClearAllPools();
        File.Copy(yedekYolu, AppDbContext.VeriTabaniYolu, overwrite: true);
    }

    public static void YedekSil(string yedekYolu)
    {
        try { if (File.Exists(yedekYolu)) File.Delete(yedekYolu); } catch { }
    }
}
