# Ticaret Yönetim

Web sitesi satışı, dijital katalog ve 3D baskı işlerinizi tek yerden takip eden Windows masaüstü uygulaması.

## Özellikler

- **Panel (Dashboard):** Toplam satış cirosu, alış gideri, net kâr, bu ay satışı, son hareketler ve satış kanalı dağılımı
- **Ürünler / Katalog:** Ürün ekleme, düzenleme, silme, **resim yükleme**, ürün türü (Fiziksel / Dijital / 3D Baskı), maliyet, satış fiyatı ve stok takibi
- **Satışlar:** Hangi ürünü, kaç adet, hangi fiyata, hangi kanaldan (Web sitesi / Dijital katalog / 3D baskı siparişi) sattığınızı tarihli kayıt
- **Alışlar:** Malzeme, filament, hammadde ve tedarikçi giderlerinin kaydı
- **Raporlar:** Tarih aralığına göre ürün bazlı ciro / maliyet / kâr raporu ve CSV dışa aktarma
- **Tarih filtreleri:** Tüm listelerde başlangıç-bitiş tarihi filtreleme

## Teknolojiler

- .NET 8 / WPF (Windows)
- MVVM mimarisi
- SQLite (Entity Framework Core) — veriler `%APPDATA%\TicaretYonetim\` klasöründe saklanır
- ModernWpfUI arayüz teması

## Kurulum ve Çalıştırma

Gereksinim: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```bash
dotnet restore
dotnet run
```

Yayın sürümü oluşturmak için:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -o publish
```

`publish\TicaretYonetim.exe` dosyasını çift tıklayarak kullanabilirsiniz.

## Veri Nerede Saklanıyor?

- Veritabanı: `%APPDATA%\TicaretYonetim\ticaret.db`
- Ürün resimleri: `%APPDATA%\TicaretYonetim\Resimler\`

Yedek almak için bu klasörü kopyalamanız yeterlidir.
