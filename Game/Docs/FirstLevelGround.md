# 1. Bölüm — Ground.fbx Entegrasyonu

Ground.fbx değiştirilmeden Models klasörüne eklendi. Dosyada 13 taş örgü mesh'i bulunur; gömülü materyal veya doku yoktur. Şimdilik URP uyumlu nötr taş materyali atanır.

Ground_Art altında aynı modelden 3 parça yan yana yerleştirilir. GroundModelSetup bileşeni editörde ve oyunda Unity'nin gerçek import edilmiş bounds değerlerini kullanarak parçaları eski Ground BoxCollider alanına hizalar: X=-50..50, Z=-13..13, üst sınır Y=0.5. Parçalar birleşim yerlerinde 0.04 birim örtüşür. Y ölçeği 1 kalır; genişlik ve derinlik sahneye göre ayarlanır.

Eski Ground küpünün Mesh Renderer'ı gizlendi, BoxCollider ve hareket yüzeyi korundu. Prenses, cüceler, platformlar, maske ve bölüm geçişi konumları değişmedi.

Gelecekte zeminin ölçüsü değişirse Ground_Art üzerindeki GroundModelSetup bileşeninin Fit Ground Model menüsü kullanılabilir. Dokular geldiğinde taş materyali güncellenir. Gerçek taş yüzeyi hafif düzensizdir; yürüme çarpışması prototipte düz tutulur.

Dosya bütünlüğü, FBX geometrisi, sahne referansları ve kaplama ölçüleri kontrol edilir. Unity'de görsel hizalama ayrıca denenmelidir.
