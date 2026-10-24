# 3. Bölüm — Kule İçinde Prensten Kaçış

## Akış

2. bölümün sonundaki kapı artık kule girişidir. Kapıya girince cadı ve eski tehlikeler durur; kısa kararmadan sonra Scene3_TowerEscape açılır.

Kulede bir cutscene yeri vardır. Gerçek Timeline henüz atanmadığı için E veya Space ile kaçışı başlatan bir geçici panel gösterilir. Timeline hazır olduğunda Tower_LevelController üzerindeki Intro Cutscene alanına PlayableDirector atanır; bu director oyun dururken UnscaledGameTime ile oynar ve bitince kaçış başlar.

## Kule ölçüleri ve artist düzeni

- Oynanış X/Y düzleminde, Z=0. Kule duvarları yaklaşık 44 birim yüksekliğinde ve 24 birim genişliğindedir.
- Başlangıç üst yüzeyi Y=0.5; çıkış katı Y=36.5.
- Altı merdiven kolu ve toplam 96 düzenlenebilir basamak vardır. Her kolda 16 basamak bulunur.
- İlk kol X=-8.5 → 6, sonraki kollar dönüşümlü olarak X=5 → -6 ve X=-5 → 6 gider. Her kol 3.5 birim yükselir.
- Dönüş sahanlıkları X=±9'da, bir önceki kolun ucundan 3 birim yukarıdadır. Sahanlığa ve ardından sonraki merdivene atlanır.
- İki kontrol noktası Y=13.5 ve Y=26.5 sahanlıklarındadır. Altına çok düşmek veya hasar almak son kontrol noktasına döndürür.
- Her Flight nesnesi altında Art_Anchor vardır. Step nesneleri geçici görsellerdir; artist değiştirebilir.
- StairRamp_Collision görünmez eğimli BoxCollider'dır. Basamaklarda takılmadan yürümeyi sağlar; çarpışma desteğini koruyun.
- Kamera yatayda kule merkezinde sabit kalır, yükselirken oyuncuyu dikey takip eder.

| Kol | Başlangıç Y | Bitiş Y | Sonraki sahanlık Y |
| --- | ---: | ---: | ---: |
| 1 | 0.5 | 4 | 7 |
| 2 | 7 | 10.5 | 13.5 |
| 3 | 13.5 | 17 | 20 |
| 4 | 20 | 23.5 | 26.5 |
| 5 | 26.5 | 30 | 33 |
| 6 | 33 | 36.5 | Çıkış |

## Prens ve öpücükler

Mavi kapsül geçici prenstir. Prince_ArtAnchor gelecekteki model içindir. Prens kuleye özel hazırlanmış Prince_Route güzergâhında yürür/koşar ve dönüş boşluklarında yay çizerek atlar. Kinematik hareket kullanır; serbest NavMesh yapay zekâsı değildir. Güzergâh merdivenler değişirse güncellenmelidir.

Prens 2–3 saniyede bir oyuncunun o anki konumuna pembe kalp şeklinde öpücük gönderir. Mermiler hafif dalgalanır; duvara veya merdivene değince kaybolur. Prense temas etmek veya öpücüğe çarpmak son kontrol noktasına döndürür. Dönüşte mermiler temizlenir ve kısa korunma verilir.

Tepede cam ayakkabı yer tutucusu E ile incelenebilir; okurken oyun durur. Son kapı bölüm tamamlandı panelini açar. R mevcut bölümü yeniden başlatır.

## Test sınırı

Sahne/prefab/GUID bağlantıları, kule açıklıkları, merdiven eğimleri ve atlama mesafeleri dosya ve sayısal kontrollerle doğrulanır. Unity editöründe derleme, fizik, kamera, GUI ve oynanış dengesi ayrıca denenmelidir.
