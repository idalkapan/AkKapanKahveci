# Ak Kapan Coffee

Next.js / React / TypeScript arayüz ve .NET 8 Minimal API + SQLite backend.

## Çalıştırma

Tek komutla frontend ve backend'i birlikte başlatın:

```sh
./scripts/dev.sh
```

http://localhost:3000/login adresinden hesap oluşturup şifrenizle giriş yapın. Şifreler PBKDF2-SHA256 ile tuzlanıp özetlenir; giriş HttpOnly cookie ile korunur. Siparişler ve öğrenci başvuruları hesabınıza bağlıdır; çıkıştan sonra yeniden giriş yaptığınızda korunur. Sipariş, belge ve sipariş geçmişi API'leri giriş gerektirir. `/api/orders` yalnızca giriş yapan kullanıcının siparişlerini döndürür. Giriş/kayıt denemeleri IP başına dakikada 15 istekle sınırlıdır. Eski anonim siparişler veritabanında korunur; yeni hesaplara otomatik aktarılmaz.

İsterseniz iki terminalde:

```sh
cd backend
dotnet restore
dotnet run
```

```sh
cd frontend
npm ci
npm run dev
```

Arayüz: http://localhost:3000 — API: http://localhost:5050. Next.js `/api` isteklerini backend'e yönlendirir. Başka API adresi için frontend `.env.local` içinde `API_URL` tanımlayın. Üretimde iki uygulama için ayrı Node.js ve .NET barındırması gerekir.

## Özellikler

- 10 kahve, 4 tatlı; sunucuda tanımlanan fiyatlar.
- Candy Club, Green Escape ve Sunset Mood konseptleri.
- Miktar değiştirme, silme, tarayıcıda sepet saklama.
- Kahvelere 45 TL ek ücretli kişisel bardak; 3 renk ve 24 karakter yazı.
- Öğrenci belgesi: PDF/PNG/JPEG, içerik imzası kontrolü, 5 MB sınırı, özel disk alanında saklama.
- Öğrenci belgesi manuel onaylanınca bir yıl geçerli %10 indirim. Bardak ücreti indirim dışındadır.
- SQLite'ta sipariş ve başvuru saklama. Oturum HttpOnly cookie ile ilişkilidir.
- Sunucuda fiyat, ürün, adet ve bardak doğrulaması. Bozuk JSON ve boş ürün girdileri 400 döndürür.
- Kullanıcı başına dakikada 20 sipariş ve 5 belge yükleme denemesi. Sınır aşımında JSON hata mesajıyla 429 ve `Retry-After: 60` döner. Sayaçlar uygulama belleğindedir; çoklu sunucuda ortak sayaç gerekir.
- Belge boyutu en fazla 5 MB; multipart istek için 64 KB ek alan ayrılır. Boyut aşımı 413, bozuk form 400 döndürür.

## Roller ve ilk yönetici kurulumu

Cookie ile kimlik doğrulama ve merkezi rol politikaları kullanılır. Herkese açık kayıt her zaman `Customer` oluşturur. `Barista` ve `Admin` rolleri kalıcı `UserRoles` tablosunda saklanır; roller her istekte veritabanından yenilenir. `X-Admin-Key` artık yetki sağlamaz.

İlk Admin kurulumu, yedekli SQL yükseltmesi, ortam değişkenleri ve testler: [Backend yetkilendirme notları](docs/backend-authorization.md).

Öğrenci belgesi inceleme uçları Admin girişi gerektirir:

- `GET /api/admin/documents`
- `GET /api/admin/documents/{id}/file`
- `POST /api/admin/documents/{id}/review`: `{"status":"approved"}` veya `{"status":"rejected"}`.

Bu aşamada personel yönetimi ve barista sipariş API'si henüz eklenmedi; sonraki onaylı adımlarda uygulanacak.

## Ödeme

Bu sürüm **demo ödeme** içerir. Kart verisi toplamaz veya gerçek para çekmez. Sipariş demo olarak kaydedilir. `Payments:Mode` başka bir değere ayarlanırsa ödeme 503 döner; canlı ödeme gibi davranmaz.

Gerçek tahsilat için iyzico/PayTR/Stripe gibi bir sağlayıcı hesabı ve gizli anahtarlar gerekir. Üretim entegrasyonunda sağlayıcının barındırdığı ödeme sayfası, doğrulanmış webhook, tekrar eden istek koruması ve ödeme onayından sonra sipariş durumunu değiştirme eklenmelidir. Mevcut sürüm gerçek ödeme kabul etmek için hazır değildir.

## Kontroller

```sh
cd frontend
npm run build
```

```sh
cd backend
dotnet build
```

Üretim öncesi HTTPS, yönetici kimlik doğrulama, belge zararlı yazılım taraması ve yedekleme yapılandırılmalıdır. Belge yüklemesi kimlik/öğrencilik doğrulaması yerine geçmez; onay gerekir.

Next.js kurulum referansı: https://nextjs.org/docs/app/getting-started/installation

API entegrasyon testleri (geçici SQLite veritabanı, 5052 portu): `python3 tests/smoke.py`. Menü, fiyatlar, özel bardak, geçersiz sipariş, belge yükleme, yönetici yetkisi ve onay/ret indirim durumlarını kontrol eder. Ayrıca kullanıcılar arası veri ayrımı, bozuk JSON/null sepet, 5 MB dosya sınırı ve kullanıcı bazlı hız sınırlarını doğrular.
