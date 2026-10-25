# 1. Bölüm — Yedi Cüce ve Üst Platformlar

- SevenDwarfs altında tam 7 cüce vardır. Hepsi aynı GnomeRun modelini ve Gnome materyalini kullanır. Model, diffuse ve normal haritası Assets/Resources/Dwarfs içindedir.
- DwarfModelVisual, modeli Art_Anchor altında editörde ve oyunda otomatik gösterir. Görsel boyu 1.8 birimdir; ayakları Art_Anchor üzerinde kalır. Mevcut 1.3 birimlik takip kapsülü korunur ve eski kapsül görseli gizlenir. Üretilen görseller sahneye kaydedilmez; asıl FBX ve materyal ortak kullanılır.
- Slow Run animasyonu FBX dosyasındaki gerçek kare aralığından içe aktarılıp döngüye alınır. Hareket ederken oynar; beklerken durur. Root motion kapalıdır, konumu takip kodu yönetir.
- Takip yolu oyuncunun yatay hareketini kaydeder ve yalnızca sahnedeki Ground collider yüzeyine yansıtır. Oyuncu zıpladığında veya üst platforma çıktığında cüceler alttaki zeminden yürür; zıplama yüksekliğini veya platform rotasının yüksekliğini kopyalamazlar. Bu ilk bölümün kesintisiz düz zemini içindir.
- Oyuncuyu fiziksel olarak engellemezler. Baltaya temas etmek oyuncuyu başlangıca döndürür.
- Düşüp başlangıca dönünce cüceler de başlangıç sırasına döner.
- Oyun açıkken script yenilenirse veya grup kapatılıp açılırsa takip bağlantıları tekrar kurulur. Grup yedi takipçiye kendisini ve sıra numaralarını bağlar; Rigidbody ve görsel referansları yeniden bulunur. Başlangıç sırası Awake/Start çağrı sırasına bağlı değildir.
- Oyun sırasında grubun bileşen menüsündeki **Log Chase Status**, hedefi, takip durumunu, duraklama durumunu, takipçi sayısını ve yol mesafesini Console’a yazar.
- Üst platformlar 6'dan 4'e azaltılmış isteğe bağlı bir yan rotadır. Son iki platform, onlara ait çarpışmalar, model ve süslemeler kaldırılmıştır; ilk üç platform maskeye çıkar, dördüncü platform iniş sağlar. Zemindeki düz geçiş yolu açık kalır. En alçak platformun altı Y=3.05; yerdeki karakterin başı Y=2.5 olduğundan koşarak altından geçilir.
- Maske üçüncü platformdadır: X=-8, Y=8, Z=-4. E ile ekrandaki açıklama açılır; okurken oyun ve takip durur. E/Esc ile devam edilir.
- Her platform kökü üst yüzeyin merkezinde, ölçek (1,1,1). Modeller Art_Anchor altına eklenir; Blockout_Collision BoxCollider korunur.
- Mantar ev ve oyuncunun başlangıç yerleri ile bölüm sonundaki cadı evi geçişi değiştirilmedi.

| Platform | Merkez X | Üst yüzey Y | Genişlik | Derinlik |
| --- | ---: | ---: | ---: | ---: |
| 1 | -29 | 3.7 | 7 | 6 |
| 2 | -19 | 5.8 | 6 | 6 |
| 3 — Maske | -8 | 7.6 | 7 | 6 |
| 4 | 4 | 6 | 6 | 6 |

Sahne bağlantıları ve zıplama mesafeleri sayısal olarak kontrol edilir; Unity editöründe oynanış ayrıca denenmelidir.

## Balta ve görünürlük düzeltmesi

- Yedi cücenin sahnede kaydedilen boyu 1.8 birimdir. Boyut animasyon başlamadan önce kaynak mesh sınırlarının sekiz köşesinden ölçülür; kamera ve BakeMesh ölçek davranışından bağımsızdır. Ayak/ayak parmağı kemikleri taban hizalamasını sağlar.
- Her kare kayıtlı yerel başlangıç konumundan hizalanır; kamera dışında eski renderer bounds değerleriyle biriken dikey kaydırma kaldırılmıştır. Görseller kamera dışında da güncellenir. Kalça kemiğinin yatay animasyon hareketi sabitlenir; görseli takip noktasından uzaklaştıramaz. Koşu klibi eksikse içe aktarılan model görünür kalır ve onarım uyarısı gösterilir.
- Baltalar cücenin baktığı X yönünde yerden 0.4 birim yüksekte, 7.5 birim/saniye hızla düz gider. Oyuncunun konumu, hızı veya zıplaması hedef hesabına girmez. İlk atış 4 saniye sonra, devamı 3.5–4.5 saniye aralıklarla yapılır.
- Hasar yarıçapı 0.22 birimdir. Eski 0.62 birimlik yakınlık hasarı kaldırıldı. Fizik adımları arasındaki geçiş küçük bir kapsülle taranır; üstünden atlanan baltalar hasar vermez.
- Başlangıca dönüşte eski baltalar temizlenir ve atış süresi sıfırlanır. Giriş videosunda, açıklama okurken ve sahne geçişinde hasar verilmez.
- Elma hareketi ve ağaç kütüğü görünürlük düzenlemeleri korunur.

Doğrulama: bütün oynanış C# dosyaları sözdizimi kontrolünden geçti. Sahnedeki yedi cüce kökü ve bağlantıları korundu; sahnede yalnızca yedi boy alanı değişti. Üç model/kaplama kaynağı manifest SHA-256 değerleriyle doğrulandı. Unity 6000.3.10f1 bu ortamda kurulu olmadığından derleme ve Play Mode kontrolü burada yapılamadı.
