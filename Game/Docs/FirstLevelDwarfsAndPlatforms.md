# 1. Bölüm — Yedi Geçici Cüce ve Üst Platformlar

- SevenDwarfs_Placeholders altında tam 7 kapsül bulunur. Aynı materyali kullanırlar. Gelecek model her kapsülün Art_Anchor noktasına eklenebilir; Capsule_Placeholder Mesh Renderer gizlenebilir.
- Kapsüller karakterin kaydedilen ayak izini izler; koşma, yön değiştirme ve platformlara zıplama hareketlerini sırayla takip ederler. Bu geçici sürüm kinematik yol takibidir, bağımsız düşman navigasyonu değildir.
- Oyuncuyu fiziksel olarak engellemezler; bu değişiklikte yakalanma/hasar cezası eklenmedi. Hasar davranışı daha sonra ayrı bağlanabilir.
- Düşüp başlangıca dönünce cüceler de başlangıç sırasına döner.
- Üst platformlar isteğe bağlı bir yan rotadır. Zemindeki düz geçiş yolu açık kalır. En alçak platformun altı Y=3.05; yerdeki karakterin başı Y=2.5 olduğundan koşarak altından geçilir.
- Maske üçüncü platformdadır: X=-8, Y=8, Z=-4. E ile ekrandaki açıklama açılır; okurken oyun ve takip durur. E/Esc ile devam edilir.
- Her platform kökü üst yüzeyin merkezinde, ölçek (1,1,1). Modeller Art_Anchor altına eklenir; Blockout_Collision BoxCollider korunur.
- Mantar ev ve oyuncunun başlangıç yerleri ile bölüm sonundaki cadı evi geçişi değiştirilmedi.

| Platform | Merkez X | Üst yüzey Y | Genişlik | Derinlik |
| --- | ---: | ---: | ---: | ---: |
| 1 | -29 | 3.7 | 7 | 6 |
| 2 | -19 | 5.8 | 6 | 6 |
| 3 — Maske | -8 | 7.6 | 7 | 6 |
| 4 | 4 | 6 | 6 | 6 |
| 5 | 17 | 4 | 7 | 6 |
| 6 | 29 | 5.2 | 5.5 | 6 |

Sahne bağlantıları ve zıplama mesafeleri sayısal olarak kontrol edilir; Unity editöründe oynanış ayrıca denenmelidir.
