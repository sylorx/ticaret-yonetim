# Ticaret Yönetim

Web sitesi satışı, dijital katalog ve 3D baskı işlerinizi tek yerden takip eden **Windows masaüstü uygulaması**.

Bu program; ne aldığınızı, ne sattığınızı, kaça sattığınızı, hangi tarihte ne olduğunu resimli ürün kartlarıyla kayıt altına alır ve kâr/zarar durumunuzu anlık gösterir.

---

## 📌 Nedir?

Ticaretle uğraşanlar için geliştirilmiş, internet bağlantısı gerektirmeyen, verileri bilgisayarınızda saklayan bir kayıt programıdır:

- **Web sitesi satışı** yapıyorsanız → satışlarınızı kanal olarak "Web sitesi" ile işaretleyin
- **Dijital katalog** hazırlıyorsanız → ürünlerinizi resimli katalog olarak tanımlayın
- **3D yazıcınız** varsa → bastığınız ürünleri, filament/malzeme alışlarını ve baskı siparişlerini ayrı ayrı takip edin

Üç iş kolunuz da tek ekranda, tek veritabanında birleşir.

---

## ✨ Özellikler

| Modül | Ne Yapar? |
|---|---|
| **📊 Panel** | Toplam satış cirosu, toplam alış gideri, net kâr, bu ay satışı, satılan adet, aktif ürün sayısı, son hareketler listesi, satış kanalı dağılımı |
| **📦 Ürünler / Katalog** | Ürün ekleme, düzenleme, silme, **resim yükleme**, ürün türü (Fiziksel / Dijital / 3D Baskı), kategori, birim maliyet, satış fiyatı, stok adedi, arama |
| **💰 Satışlar** | Hangi ürünü, kaç adet, hangi fiyata, hangi kanaldan (Web sitesi / Dijital katalog / 3D baskı siparişi / Diğer) sattığınızı tarihli kayıt; satış yapınca stok otomatik düşer |
| **🧾 Alışlar** | Malzeme, filament, hammadde, tedarikçi giderlerinin kaydı; istenirse ürüne bağlanır |
| **📈 Raporlar** | Tarih aralığına göre ürün bazlı ciro / maliyet / kâr raporu, **CSV dışa aktarma** |
| **🔎 Tarih Filtreleri** | Satış ve alış listelerinde başlangıç–bitiş tarihi filtreleme |

---

## 🖥️ Nasıl Çalışır?

### Teknolojiler

- **.NET 8 / WPF** — Windows masaüstü arayüzü
- **MVVM** mimarisi — arayüz ile veri katmanı ayrı
- **SQLite (Entity Framework Core)** — veritabanı, kurulum gerektirmez
- **ModernWpfUI** — modern Windows 11 görünümlü arayüz teması

### Veriler Nerede Saklanır?

Tüm verileriniz bilgisayarınızda, uygulama veri klasöründe tutulur:

```
%APPDATA%\TicaretYonetim\
├── ticaret.db          ← veritabanı (ürünler, satışlar, alışlar)
└── Resimler\           ← ürün resimleri
```

> **Yedekleme:** Bu klasörü kopyalamanız yeterlidir. (Başlat çalıştıra `%APPDATA%\TicaretYonetim` yazıp Enter'a basın.)

### Kurulum ve Çalıştırma

Gereksinim: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```bash
# Geliştirici modunda çalıştırma
dotnet restore
dotnet run
```

```bash
# Tek dosyalık yayın sürümü oluşturma
dotnet publish -c Release -r win-x64 --self-contained true -o publish
```

`publish\TicaretYonetim.exe` dosyasını çift tıklayarak kullanabilirsiniz. (Bu sürüm .NET kurulumu gerektirmez.)

### Kullanım Akışı

1. **Ürünler** sekmesinden ürünlerinizi resimleriyle ekleyin (maliyet + satış fiyatı + stok)
2. **Satışlar** sekmesinden her satışı kaydedin → stok otomatik düşer, ciro hesaplanır
3. **Alışlar** sekmesinden malzeme/filament giderlerinizi girin
4. **Panel** ekranından kâr/zarar durumunuzu anlık görün
5. **Raporlar** sekmesinden dönem raporu alın veya CSV olarak dışa aktarın

---

## 🗺️ Neler Yapılacak? (Yol Haritası)

Aşağıdaki özellikler planlanmıştır / geliştirilecektir:

- [ ] **Tek tıkla kurulum paketi** — installer ile kurulum (Inno Setup / MSIX)
- [ ] **Otomatik yedekleme** — her kapanışta veya günlük otomatik yedek
- [ ] **Kâr grafikleri** — aylık ciro/kâr grafikleri (Panel ve Raporlar)
- [ ] **Müşteri / tedarikçi rehberi** — ayrı kişi kayıtları ve geçmişleri
- [ ] **Fatura / fiş numarası** — satış kayıtlarına belge numarası ekleme
- [ ] **Excel dışa aktarma** — CSV'ye ek olarak .xlsx desteği
- [ ] **Karanlık / aydınlık tema seçimi** — kullanıcı tercihi
- [ ] **Çoklu para birimi** — dövizli satış desteği
- [ ] **Veri içe aktarma** — eski kayıtların toplu yüklenmesi
- [ ] **Android / iOS uygulaması** — mobilde de kayıt girişi

---

## 📁 Proje Yapısı

```
src/TicaretYonetim/
├── App.xaml / MainWindow.xaml   ← uygulama girişi ve ana pencere (sol menü)
├── Models/                      ← veri modelleri (Ürün, Satış, Alış, enumlar)
├── Data/AppDbContext.cs         ← SQLite bağlantısı ve tablo tanımları
├── Services/ResimServisi.cs     ← ürün resmi kopyalama/silme
├── ViewModels/                  ← MVVM görünüm modelleri (iş mantığı)
└── Views/                       ← ekranlar (Panel, Ürünler, Satışlar, Alışlar, Raporlar)
```

---

## 📄 Lisans

Bu proje kişisel kullanım içindir. İzinsiz dağıtılamaz.
