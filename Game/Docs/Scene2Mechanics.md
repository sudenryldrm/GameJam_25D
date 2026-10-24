# 2. Bölüm — Modeller Gelmeden Oynanabilir Mekanikler

- Mor kapsül: uçan cadı. Yavaş ilerleyen oyuncuya yaklaşır; hızlanarak arayı açmak mümkündür.
- Kırmızı küre: zehirli elma. Cadı 3.5 saniyelik başlangıç süresinden sonra 2.5–4 saniye aralıklarla oyuncunun atış anındaki konumuna atar.
- Elma zemine/duvara çarpınca 2.5 saniye kalan yeşil bir zehir alanı bırakır.
- Elma veya zehir oyuncuya değince başlangıca dönülür; eski elmalar/bulutlar temizlenir, cadı başlangıç mesafesine döner.
- Her dönüşte 2.5 saniye korunma vardır. Boşluğa düşmek de aynı sıfırlamayı kullanır.
- Kurbağa yer tutucusunun yanında E ile hikaye açılır; okurken fizik ve kovalama durur. E/Esc ile devam edilir.
- Bölüm sonundaki kule kapısından geçince kısa kararmayla Scene3_TowerEscape açılır. Gerçek kule içi cutscene için yeni sahnede PlayableDirector alanı ayrılmıştır.
- R ile bölüm yeniden başlatılır. Shift koşma, Space zıplama/çift zıplama.

## Artist için

Platform ölçüleri değişmedi. Cadı için WitchBoss/Witch_ArtAnchor kullanın; geçici mor Mesh Renderer'ı model hazır olunca gizlenebilir. Apple_ThrowPoint elmanın çıkış noktasıdır. PoisonApple prefabındaki kırmızı küre elma modeliyle değiştirilebilir; Rigidbody, SphereCollider ve PoisonApple bileşenlerini koruyun. PoisonCloud şimdilik basit bir yeşil şekildir; daha sonra VFX ile değiştirilebilir. Hikaye arayüzü de geçici IMGUI'dir.

Kurbağanın mevcut collider/InteractableObject bağlantıları yalnız bu sahnede devre dışı bırakıldı; hikayeyi Scene2ChaseController yönetir. Birinci sahnenin maskesi etkilenmez.

## Doğrulama sınırı

Atış yörüngesi sayısal olarak ve sahne/prefab bağlantıları dosya üzerinden kontrol edilir. Unity oyun içi test ayrıca gereklidir.
