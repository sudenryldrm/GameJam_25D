# 2. Bölüm — Platform Blokajı

Referanstaki düzen iki kez yan yana tekrarlandı. Oynanış X/Y düzleminde, karakterin hareket hattı Z=0. Unity birimleri kullanılır.

- Sahne: `Assets/Scene2_WitchChase.unity`.
- Hiyerarşi: `Scene2_Blockout/Module_01` ve `Module_02`.
- İkinci modül birincinin aynısıdır; X ekseninde +32 birim ötelenmiştir.
- Toplam 10 platform; yatay sınırlar X=-5 ile X=59 (64 birim).
- Her platform kökü, üst yüzeyin merkezindedir ve ölçeği (1,1,1)'dir.
- `Blockout_Collision`: oynanışı belirleyen küp ve BoxCollider.
- `Surface_Guide`: üst yüzeyi gösteren renkli kılavuz; çarpışması yoktur.
- `Art_Anchor`: modelinizi burada, platformun üstüne oturtun; görünümü tamamlayınca iki blokaj Mesh Renderer'ını gizleyebilirsiniz. BoxCollider'ı koruyun.
- Alt boşluklar 4 birimdir. Modüller X=27'de düz bir zeminle birleşir.
- Cutscene ve kovalamaca bu değişikliğe eklenmedi; mevcut WitchBoss nesnesi daha sonra kullanılmak üzere kapalı tutuldu.
- `Future_Cutscene_Anchor` ve `Future_Exit_Anchor` yalnızca yer işaretidir; sahne yüklemez.
- Düşünce mevcut karakter kodu başlangıca döndürür; boşluklarda ek zarar sistemi yoktur.

## Bir modülün ölçüleri

X koordinatları Module_01 için verilmiştir. Module_02'de X'e 32 ekleyin.

| Platform | Üst yüzey merkez X | Üst yüzey Y | Genişlik X | Kalınlık Y | Derinlik Z |
| --- | ---: | ---: | ---: | ---: | ---: |
| 01_StartIsland | 0.5 | 0.5 | 11 | 3.5 | 6 |
| 02_UpperPlatform | 9.75 | 4.7 | 6.5 | 0.8 | 6 |
| 03_MiddleIsland | 13 | 0.5 | 6 | 3.5 | 6 |
| 04_LowerPlatform | 19 | 2.1 | 4 | 0.7 | 6 |
| 05_ExitIsland | 23.5 | 0.5 | 7 | 3.5 | 6 |

## Kontrol

SampleScene'deki ev geçişinden bu sahneye gelinir. Başlangıç X=-2, Y=2, Z=0'dır. A/D ile hareket, Shift ile koşma, Space ile zıplama / çift zıplama mevcut karakter ayarlarını kullanır.

Bu yerleşimde çarpışma hacimleri ve sıçrama mesafeleri sayısal olarak kontrol edilmiştir. Unity editöründe oynanabilirlik ve kamera görünümü ayrıca doğrulanmalıdır.
