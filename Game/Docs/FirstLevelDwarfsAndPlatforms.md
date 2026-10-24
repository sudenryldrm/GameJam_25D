# 1. Bölüm — Yedi Cüce ve Üst Platformlar

- SevenDwarfs altında tam 7 cüce vardır. Hepsi aynı GnomeRun modelini ve Gnome materyalini kullanır. Model, diffuse ve normal haritası Assets/Resources/Dwarfs içindedir.
- DwarfModelVisual, modeli Art_Anchor altında editörde ve oyunda otomatik gösterir. Görsel boyu 1.6 birimdir; ayakları Art_Anchor üzerinde kalır. Mevcut 1.3 birimlik takip kapsülü korunur ve eski kapsül görseli gizlenir. Üretilen görseller sahneye kaydedilmez; asıl FBX ve materyal ortak kullanılır.
- Slow Run animasyonu FBX dosyasındaki gerçek kare aralığından içe aktarılıp döngüye alınır. Hareket ederken oynar; beklerken durur. Root motion kapalıdır, konumu takip kodu yönetir.
- Takip yolu oyuncunun yatay hareketini kaydeder ve yalnızca sahnedeki Ground collider yüzeyine yansıtır. Oyuncu zıpladığında veya üst platforma çıktığında cüceler alttaki zeminden yürür; zıplama yüksekliğini veya platform rotasının yüksekliğini kopyalamazlar. Bu ilk bölümün kesintisiz düz zemini içindir.
- Oyuncuyu fiziksel olarak engellemezler; bu değişiklikte yakalanma/hasar cezası eklenmedi. Hasar davranışı daha sonra ayrı bağlanabilir.
- Düşüp başlangıca dönünce cüceler de başlangıç sırasına döner.
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

Görünürlük güncellemesi: yedi cücenin her birinde visualHeight=1.6. Önceki
1.3 birimlik görsele göre yaklaşık %23 büyüktür; takip sırası, aralıkları,
zeminde yürüme ve koşu animasyonu kodu değişmez. Unity oynanış kontrolü bekleniyor.
