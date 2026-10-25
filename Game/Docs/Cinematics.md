# Açılış ve bölüm konuşmaları

## Akış

Yeni oyun ilk orman sahnesinde başladığında 15 saniyelik harita turu oynar:

- 0–4 sn: birinci ormanda göz hizasında first person ilerleyiş.
- 4–8 sn: cadının ormanında köprü ve platformlar boyunca first person geçiş.
- 8–12 sn: kulenin içinde merdivenler boyunca first person yükseliş.
- 12–15 sn: gerçek oyuncunun başlangıç noktasına dönüş.

Ardından öndeki cüceyle yakın çekim konuşması açılır. İkinci bölümde cadı, üçüncü bölümde prens konuşması doğrudan başlar; harita turu bölüm geçişlerinde tekrarlanmaz. R ile bölümü yeniden yüklemek aynı oyun oturumunda harita turunu tekrar başlatmaz.

## İleride Start düğmesi

İlk bölümdeki LevelIntroCutscene üzerinde Play On Start kapatılıp düğmenin OnClick olayına StartGameOpening bağlanabilir. Şimdilik düğme eklenmedi; akış Play ile otomatik başlar.

## Kontroller

- Açılışta Esc: turu geçip konuşmaya dön.
- Konuşmada E / Space / tıklama: önce repliği tamamla, sonra sıradaki repliğe geç.
- Konuşmada Esc: konuşmayı geç; kamera oyun kadrajına döner.

Oyun ve saldırılar sinematik boyunca durur. Kamera gerçek zamanla ilerler. Kamera kontrolü bitince normal Cinemachine beyni ve takip kadrajı geri yüklenir.

## Harita önizlemeleri

Resources/MapPreviews içindeki üç prefab yalnızca statik dekor içerir; diğer bölümlerin oyuncuları, düşmanları, kameraları veya oyun mantığı açılış için yüklenmez. Harita yerleşimi değişirse Tools > Story > Bake Opening Map Previews komutuyla önizlemeler yeniden hazırlanmalıdır.

## Karakterler

Konuşmadan önce oyuncu ve boss zemine oturtulur. Fizik enterpolasyonu geçici olarak kapatılır; yön, iskeletin kol ekseninden ölçülerek karşı karaktere çevrilir. Konuşma sonunda boss başlangıç rotasına ve önceki enterpolasyon ayarına döner. Prensin eski 3,5 saniyelik beklemesi kaldırılmıştır; oyun devam ettiğinde takip hemen çalışır.
