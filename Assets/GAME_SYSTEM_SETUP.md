# Yeni 3 Butonlu Oyun Sistemi - Kurulum Rehberi

## Genel Bakış
Oyun artık 3 buton üzerinden çalışıyor:
1. **Roll Dice** - Zar atma
2. **Select A Pawn** - Hamle yapabilecek pawnlar arasında seçim yapma
3. **Make A Move** - Seçili pawnı hareket ettirme

## GameManager Inspector Ayarları

### UI Bölümü
- **Roll Dice Button**: "Roll Dice" butonunu buraya assign edin
- **Select Pawn Button**: "Select A Pawn" butonunu buraya assign edin
- **Make A Move Button**: "Make A Move" butonunu buraya assign edin
- **Roll Text**: Zar sonucunu gösteren TextMeshProUGUI
- **Turn Text**: "Turn: Blue" gibi sıra bilgisini gösteren TextMeshProUGUI
- **Turn Indicator Dot**: Turn Text'in yanındaki nokta (RectTransform) - Sıra değiştiğinde büyüyecek

### Markers Bölümü
Pawn seçildiğinde üstünde görünecek marker prefabları:
- **Pawn Marker Prefab Blue**: Mavi marker prefab
- **Pawn Marker Prefab Green**: Yeşil marker prefab
- **Pawn Marker Prefab Yellow**: Sarı marker prefab
- **Pawn Marker Prefab Red**: Kırmızı marker prefab

Her bir marker prefab, ilgili renkteki pawnların üstünde spawn olacak.

### Dice Bölümü
- **Dice 3D**: Zar modeli (Transform)
- **Dice Rotation Duration**: Zarın dönme animasyon süresi (varsayılan: 0.5 saniye)

## Zar Yüz Konumlandırması
Zarınızı şu şekilde konumlandırmalısınız (face -> yön):
- **1**: Arka (back) - Z negatif yöne bakacak (0,0,0 rotasyon)
- **2**: Sağ (X+) - Z negatif yöne dönecek (-90° Y ekseni etrafında)
- **3**: Alt - Z negatif yöne dönecek (90° X ekseni etrafında)
- **4**: Üst - Z negatif yöne dönecek (-90° X ekseni etrafında)
- **5**: Sol (X-) - Z negatif yöne dönecek (90° Y ekseni etrafında)
- **6**: Ön - Z negatif yöne dönecek (180° Y ekseni etrafında)

Zar atıldığında ilgili yüz Z negatif yöne (kameraya) smooth şekilde dönecek.

## Turn Indicator Dot (Nokta Göstergesi)
"Turn: Blue" yazısının yanına bir TextMeshProUGUI "." koyun ve bunun RectTransform'unu `turnIndicatorDot` alanına assign edin. Sıra değiştiğinde bu nokta büyüyecek (2.5x scale).

## Marker Prefab Oluşturma
1. Scene'de bir üçgen veya marker objesi oluşturun (örn: Cone veya özel model)
2. Pawn rengiyle eşleşen materyal verin (mavi, yeşil, sarı, kırmızı)
3. Prefab yapın: `PawnMarkerBlue`, `PawnMarkerGreen`, `PawnMarkerYellow`, `PawnMarkerRed`
4. GameManager'daki ilgili alanlara assign edin

## Oyun Akışı
1. **Roll Dice** butonuna tıkla → Zar atılır, marker temizlenir
2. **Select A Pawn** butonuna tıkla → Hamle yapabilecek pawnlar arasında döngü ile seçim, marker spawn olur
3. **Make A Move** butonuna tıkla → Seçili pawn hareket eder, marker silinir

### Buton Durumları
- **Roll Dice**: Sadece currentRoll == 0 ise aktif (zar atılmamışsa)
- **Select A Pawn**: Hamle yapabilecek pawn varsa aktif
- **Make A Move**: Bir pawn seçilmişse aktif

## Eski Sistemden Farklılıklar
- Pawn'lara tıklama artık devre dışı (OnPawnClicked şimdi sadece bilgi mesajı veriyor)
- Oyuncular "Select A Pawn" butonu ile seçim yapmalı
- Marker sistemi sayesinde hangi pawn seçili görülebilir
- Zar 3D modeli ile görsel geribildirim
- Turn indicator dot ile kimin sırası olduğu daha net

## Test Etme
1. Oyunu başlat
2. "Roll Dice" butonuna bas
3. "Select A Pawn" ile pawn seç (marker üstte görünmeli)
4. "Make A Move" ile hareketi yap
5. Zar 6 gelirse aynı oyuncu tekrar atar

## Notlar
- Marker spawn pozisyonu: pawn.position + Vector3.up * 1.5f (1.5 birim yukarıda)
- Marker pawn'a parent olur, pawn hareket edince marker de hareket eder
- Hareket tamamlanınca marker otomatik silinir
