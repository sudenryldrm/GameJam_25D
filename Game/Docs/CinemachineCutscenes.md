# Cinemachine ve Timeline

Cinemachine 3.1.7, Unity 6000.3 için manifest'e eklendi. Timeline 1.8.10 zaten projede mevcut. İlk açılışta Package Manager bağımlılıkları indirir ve packages-lock.json dosyasını günceller.

Her sahnede CutsceneDirector altında üç Cinemachine Camera var:

- CM_Gameplay: mevcut 2.5D takip ve bölüm kadrajı.
- CM_Cutscene_Wide: geniş giriş planı.
- CM_Cutscene_Close: karakter/yakın plan.

Main Camera üzerinde Cinemachine Brain bulunur. Sanal kameralar ayrıca görüntü render etmez. Kamera rig'i Main Camera'nın çocuğu değildir; böylece takip döngüsü oluşmaz. CameraFollow25D yalnızca oyun sanal kamerasını hareket ettirir. Kamera gövde/rotasyon pipeline'ı boş bırakılmıştır; sinematik kameralar Transform ve Lens üzerinden serbestçe düzenlenebilir.

## Timeline'da düzenleme

1. SampleScene, Scene2_WitchChase veya Scene3_TowerEscape sahnesini aç.
2. Hierarchy'den CutsceneDirector seç.
3. Window > Sequencing > Timeline aç. Alternatif: GameJam > Cutscenes > Open Current Scene Timeline.
4. Camera Shots track'inde Wide, Close ve Gameplay kliplerini düzenle. Süreleri kenarlarından uzat/kısalt. Geçiş için klipleri üst üste getir.
5. Hierarchy'den geniş veya yakın kamerayı seçerek konumunu ve Lens > Orthographic Size değerini değiştir. Timeline preview açıkken Game penceresinde görüntüyü kontrol et.
6. Hareket için kameraya Animation Track ekleyip kayıt yap. Yeni Animation veya Audio track ekleyerek karakter animasyonlarını ve sesleri bağlayabilirsin. Bu taslakta karakter animasyonu/diyalog eklenmedi.
7. Sahneyi ve Timeline asset'ini kaydet.

Assets/Timelines altında Intro01_Forest.playable, Intro02_Witch.playable ve Intro03_Prince.playable bulunur. Her taslak 5.2 saniye: geniş plan 0–2.4, yakın plan 2–4.4, oyun kamerası 4–5.2. Planlar arasında 0.4 saniye örtüşme bulunur. Asset'ler gerçek Timeline/Cinemachine track ve shot nesneleridir; çalışma anında yeniden üretilmezler. Kamera ve Brain bağları PlayableDirector üzerinde kayıtlıdır.

Girişler sahne başında otomatik oynar. Böylece cadı evinden 2. bölüme geçince cadı girişi, kule kapısından 3. bölüme geçince prens girişi görülür. E, Space veya Esc ile atlanabilir. Giriş sırasında oyuncu ve kovalamaca durur; Timeline ve kamera unscaled zamanda çalışır. Bitişte/atlamada oyun kontrolü ve zaman geri gelir. Üçüncü sahnede bu akışı mevcut TowerLevelController yönetir. İlk iki sahnede LevelIntroCutscene kullanılır. Kontrol noktası respawn'ları girişi yeniden başlatmaz. Yalnızca sahne yeniden açıldığında giriş tekrar oynar. İlk bölümde otomatik girişi kapatmak için LevelIntroCutscene > Play On Start işaretini kaldır.

## Doğrulama

Unity Editor bu ortamda kurulu değil; gerçek import, derleme, editör preview ve oynanış test edilmedi. YAML referansları, prefab Brain bağlantısı, exposed camera referansları, 3 planın zamanları ve sahne kapsamı programatik kontrol edilir. Unity'de her sahnede doğal bitişi, E/Space/Esc ile atlamayı, R ile yeniden başlamayı, kapılardan geçişi ve Tower checkpoint dönüşünü kontrol et. Sonrasında Timeline'a eklediğin aktör animasyonları mevcut hareket/animasyon sistemiyle ayrıca denenmelidir.

Referanslar: https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-safe/com-unity-cinemachine ve https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/concept-timeline.html
