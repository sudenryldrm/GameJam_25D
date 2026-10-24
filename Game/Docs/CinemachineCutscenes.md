# İlk bölüm kamera girişi

SampleScene başlangıcındaki giriş toplam 15 saniye sürer. CM_Intro_LeftZoom soldaki başlangıç kadrajından sağa doğru hafifçe kayar ve yavaşça yakınlaşır. Ortografik boyut 7'den 5.8'e iner. Konum (-37.5, 5.5, -20) → (-34, 4.4, -20). Hareket yumuşak başlar ve biter. Son 1 saniyede CM_Gameplay kamerasına karışarak 15. saniyede normal 2.5D kamera, hareket ve takip düzeni geri gelir. E / Space / Esc ile erken atlanabilir.

Düzenlemek için SampleScene > CutsceneDirector seç ve Window > Sequencing > Timeline aç. Intro01_Forest.playable içinde:

- Camera Shots: hareketli giriş 0–15 saniye; oyuna dönüş 14–15 saniye.
- Pan and Zoom: Soldan yavaş zoom klibini seç. Inspector'dan Start Position / End Position, Start Size / End Size ve Motion Ease eğrisini değiştir. Start Size büyüdükçe geniş açı, küçüldükçe yakın plan elde edilir. Hareket gerçek Timeline klibinden hesaplanır, editör scrubbing/preview sırasında da değerlendirilir. Preview kapanınca veya giriş atlanınca kamera ayarları geri alınır.

İlk bölümde CM_Gameplay ve ileride kullanılabilecek CM_Cutscene_Close da kalır. Şu an yakın plan kamerasını etkinleştiren klip yoktur.

2. ve 3. bölümün giriş Timeline asset'leri, PlayableDirector bileşenleri ve sinematik kameraları kaldırıldı. Bölümler ve mekanikleri korunur. GameplayCameraRig altında yalnızca oyun sanal kamerası bulunur. 3. bölüm artık giriş paneli beklemeden doğrudan oynanışa başlar. İleride TowerLevelController.introCutscene alanına yeni Director bağlanabilir.

Unity editörü bu ortamda yok. Dosya referansları, 15 saniyelik süre, hareket eğrisi, son kamera, korunan fizik/nesneler ve diğer sahnelerden kaldırılan bağlantılar programatik kontrol edilir. Unity'de kamera hareketini, Timeline preview'ı, doğal bitişi, atlamayı ve sonraki bölümlerin doğrudan başlamasını ayrıca kontrol et.
