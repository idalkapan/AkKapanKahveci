# Backend yetkilendirme — 1. adım

Bu adım kalıcı roller, merkezi politikalar, güvenli SQL yükseltmesi ve ilk Admin kurulumunu içerir. Personel yönetimi/son Admin koruması 2. adımda; sipariş servisi, durum geçişleri, iptal, snapshot ve barista API'si 3. adımda uygulanacak. Katalog, kampanya, stok ve içerik CRUD'u bu aşamada eklenmez.

## Veritabanı ve yedek

Bağlantı halen `backend/Infrastructure/ServiceRegistration.cs` içindeki `Data Source=data/coffee.db` değeridir. Çalışma klasörüne göre çözülür; normal çalıştırmada `backend/data/coffee.db` kullanılır. Bağlantıyı config'e ve sabit bir içerik köküne taşıma işi veritabanı aşamasına bırakılmıştır.

`SqlSchemaUpgrade` startup sırasında `BEGIN IMMEDIATE` transaction'ı ile eşzamanlı yükseltmeleri sıraya alır. Mevcut sürüm SQLite `PRAGMA user_version` alanında tek tamsayı olarak saklanır; ayrıca migration tablosu oluşturulmaz. Bu uygulamanın sürümü `1`'dir. Daha yeni sürüm varsa startup durur.

Sürüm 0 ise herhangi bir tablo/alan değişmeden önce SQLite Backup API ile tutarlı yedek alınır. Bu yöntem WAL içindeki commit edilmiş verileri de kapsar. Dosya `data/backups/coffee-YYYYMMDD-HHMMSS-fffffff.db` biçiminde, UTC zaman damgasıyla oluşturulur. Önceki yedekler üzerine yazılmaz. `integrity_check` başarılı olmadan yükseltme yapılmaz. Yedekleme veya doğrulama başarısız olursa startup durur; şema/veri değişmez. Başarısız bir yedek denemesi eksik bir dosya bırakabilir; yalnızca başarıyla tamamlanmış/doğrulanmış yedekler geri yükleme için kullanılmalıdır.

Yükseltme ve sürüm yazımı tek transaction'dır; hata durumunda tamamı geri alınır. Sürüm 1 yeniden başlatmada atlanır, yeni yedek veya rol ataması yapılmaz. Yeni/boş kurulumda da önce yedek alınır ve mevcut temel tablolar aynı transaction içinde oluşturulur. Eski `EnsureCreated` ve kontrolsüz başlangıç SQL'i kaldırılmıştır.

Değişiklikler:

- `UserRoles(UserId, Role)`: birleşik primary key, `Users.Id` foreign key, izinli rol değerleri için CHECK.
- Mevcut hesaplara `Customer` atanır. `Account` entity adı ve `Users` tablo adı korunur.
- `Orders.PreparationStatus`: eski kayıtlar `Delivered`.
- `Orders.PaymentStatus`: eski kayıtlar `DemoPaid`.
- `Orders.UpdatedAt`: eski kaydın `Created` değeri.
- `Orders.Version`: eski kayıtlar `1`, EF concurrency token.

Eski siparişlerde `Delivered` tarihsel teslimatın doğrulanması değil, eski kayıtların ileride hazırlık kuyruğuna girmemesi için onaylanan eşlemedir. Yeni siparişler `Received` / `DemoPaid` / `Version = 1` oluşturulur. Durum değiştirme servisi ve sürüm artırma 3. adımda eklenecektir.

Yalnızca yükseltmeyi çalıştırmak ve HTTP sunucusu/bootstrap başlatmadan çıkmak için:

```sh
cd backend
dotnet build
dotnet run --no-build -- --Database:UpgradeOnly=true
```

## SQL yükseltmesini kapatma ve gelecekte EF migration

```sh
export Database__SqlUpgrades__Enabled=false
```

Sürüm 1 olan veritabanı normal açılır. Sürüm 0 ise uygulama sessizce eski şemayla çalışmak yerine durur. Bu seçenek bir yükseltmeyi geri almaz. Geri yüklemeden önce uygulama kapatılmalı ve doğru/doğrulanmış yedek seçilmelidir.

İleride EF migration'a geçerken mevcut şemaya karşılık gelen bir baseline hazırlanacak, mevcut veritabanları için baseline kontrollü biçimde işaretlenecek ve bu SQL yükseltme çağrısı EF akışıyla değiştirilecek. Mevcut dolu veritabanına tabloları yeniden oluşturan bir initial migration doğrudan uygulanmamalı. `user_version = 1` geçişin kaynak sürümü olarak korunabilir. Şu anda EF migration dosyası veya `dotnet ef database update` akışı yoktur.

## İlk Admin: mevcut hesabı açıkça yetkilendirme

Bu seçenek yalnızca henüz hiç Admin yokken çalışır. E-posta mevcut hesabın e-postası olmalıdır; yeni hesap oluşturmaz ve şifreyi değiştirmez. Normal API üzerinden bootstrap endpoint'i bulunmaz.

Backend'i durdurun, ardından terminalde:

```sh
cd backend
export BootstrapAdmin__Enabled=true
export BootstrapAdmin__UseExistingAccount=true
export BootstrapAdmin__Email="hesabin@example.com"
dotnet run --no-build
```

Başarılı kurulumu belirten mesajdan sonra uygulamayı Ctrl+C ile durdurun, bootstrap değişkenlerini kaldırın ve normal başlatın:

```sh
unset BootstrapAdmin__Enabled BootstrapAdmin__UseExistingAccount BootstrapAdmin__Email
dotnet run --no-build
```

Mevcut şifrenizle giriş yapın. `GET /api/auth/me` yanıtında `Customer` ve `Admin` rollerini görebilirsiniz. Mevcut açık oturumda da güncel roller bir sonraki istekte yüklenir.

## İlk Admin: yeni hesap oluşturma

Aşağıdaki şifre okuma komutu projenin kullandığı zsh içindir; şifre ekrana veya komut geçmişine yazılmaz:

```sh
cd backend
export BootstrapAdmin__Enabled=true
export BootstrapAdmin__UseExistingAccount=false
export BootstrapAdmin__Name="Yönetici"
export BootstrapAdmin__Email="admin@example.com"
read -rs 'BootstrapAdmin__Password?Yeni Admin şifresi: '
printf '\n'
export BootstrapAdmin__Password
dotnet run --no-build
```

Şifre 10–128 karakter, ad 2–80 karakter ve e-posta geçerli olmalı. Aynı e-posta zaten varsa, `UseExistingAccount=false` ile startup hata verir; hesabı kendiliğinden yükseltmez. Yeni hesap `Customer` ve `Admin` rollerini alır.

Kurulumdan sonra Ctrl+C ile durdurup değişkenleri kaldırın:

```sh
unset BootstrapAdmin__Enabled BootstrapAdmin__UseExistingAccount BootstrapAdmin__Name BootstrapAdmin__Email BootstrapAdmin__Password
dotnet run --no-build
```

Admin varsa bootstrap değişiklik yapmaz; başka bir hesabı yönetici yapmaz, şifre değiştirmez. Kontrol ve oluşturma tek transaction'dır. Loglara şifre, hash, bootstrap e-postası veya adı yazılmaz. Yeni Admin için gerçek kimlik bilgileri bu geliştirme çalışmasında tanımlanmaz.

## Yetki davranışı

`PermissionPolicies` rol–politika eşleştirmesinin merkezidir. Checkout, kişisel sipariş geçmişi ve öğrenci başvurusu Customer/Admin; belge inceleme Admin gerektirir. Gelecek personel/barista/katalog/içerik politikaları tanımlıdır fakat bu modüllerin API'leri henüz yoktur. Menü anonim okunabilir.

Girişsiz korumalı istekler JSON `401`; rolü yetersiz girişli kullanıcılar JSON `403` alır. `X-Admin-Key` hiçbir yetki vermez. Kayıt gövdesindeki rol benzeri alanlar dikkate alınmaz; hesap ve Customer rolü atomik oluşturulur. Cookie'deki eski rol claim'leri her istekte veritabanındaki güncel rollerle değiştirilir. Silinmiş hesabın cookie'si reddedilir.

Siparişin `Session` alanı girişli akışta `Account.Id` içerir. Anonim cookie fallback'i kaldırılmıştır. Sahiplik filtresinin servise taşınması 3. adımın parçasıdır; mevcut kişisel liste filtresi korunur. Eski anonim kayıtlar hesaplara aktarılmaz.

## Testler

```sh
dotnet test tests/CoffeeApi.Tests/CoffeeApi.Tests.csproj
python3 tests/roles.py
python3 tests/smoke.py
```

.NET testleri tüm politika/rol matrisini, çoklu rol principal'ını, yükseltme/backup/idempotence, backup başarısızlığında durma, transaction rollback ve yükseltmeyi kapatma davranışını kontrol eder. Python entegrasyon testleri geçici SQLite veritabanları kullanır; kişisel verilerin ayrılığı, rol yükseltme saldırısı, 401/403, bootstrap senaryoları, canlı rol yenileme ve yeniden başlatma sonrası kalıcılığı doğrular. Testlerde rol değişiklikleri fixture SQL'iyle yapılır; personel yönetimi endpoint'i henüz uygulanmadı.

`.gitignore`, veritabanı/yedek/belgeleri dışarıda tutarken `backend/data` altındaki C# kaynaklarının sürümlenebilmesine izin verir.

## Dosya dökümü

Eklenenler:

- `backend/Authorization/Permissions.cs`, `PermissionPolicies.cs`, `AccountPrincipal.cs`, `AccountCookieEvents.cs`
- `backend/Domain/UserRole.cs`, `OrderPreparationStatus.cs`, `PaymentStatus.cs`
- `backend/Services/AuthService.cs`
- `backend/Infrastructure/AdminBootstrap.cs`
- `backend/data/SqlSchemaUpgrade.cs`
- `backend/Properties/AssemblyInfo.cs`
- `tests/CoffeeApi.Tests/CoffeeApi.Tests.csproj`, `AuthorizationTests.cs`, `SchemaUpgradeTests.cs`
- `tests/roles.py`
- `docs/backend-authorization.md`

Güncellenenler:

- `backend/Domain/Account.cs`, `Order.cs`
- `backend/data/Store.cs`, `DatabaseInitialization.cs`
- `backend/Infrastructure/ServiceRegistration.cs`, `RequestPipeline.cs`
- `backend/Endpoints/AuthEndpoints.cs`, `EndpointIdentity.cs`, `AdminEndpoints.cs`, `OrdersEndpoints.cs`, `CheckoutEndpoints.cs`, `StudentEndpoints.cs`
- `backend/Program.cs`
- `tests/smoke.py`, `README.md`, `.gitignore`
- Yerel `backend/data/coffee.db` (onaylanan sürüm 1); yükseltme öncesi yedek `backend/data/backups/` altında.
