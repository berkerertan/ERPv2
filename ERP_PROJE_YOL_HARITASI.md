# ERP Projesi - 6 Adimli Yol Haritasi

Bu dokuman, ERP projesini kontrollu ve hafiza-verimli sekilde ilerletmek icin hazirlanmistir.

## Teknik Yaklasim

- Platform: `.NET Web API` (tercihen guncel stabil surum)
- Mimari: `Clean Architecture`
- Uygulama deseni: `MediatR + CQRS`
- Guvenlik: `JWT (Access + Refresh Token)`
- Veri erisimi: `Repository Pattern` (+ Unit of Work ihtiyaca gore)
- Hata yonetimi: `Global Exception Handling` + `ProblemDetails`
- API dokumantasyonu/test: `Swagger / OpenAPI`
- Diger temel basliklar: `Validation`, `Logging`, `Audit`, `Pagination`, `Filtering`

## Ilk Surum Temel Moduller

- Kimlik ve yetkilendirme
- Master data (sirket, sube, depo, urun)
- **Cari Modulu** (musteri/tedarikci kartlari, cari hareketler, bakiye takibi)
- Stok ve envanter
- Satin alma ve satis
- Temel finans ve raporlama

## Hafiza Yonetimi Kurali (Zorunlu)

- Ayni anda sadece `aktif adim` ve `bir sonraki adim` detayda tutulur.
- Tamamlanan adimlar "Tamamlandi Ozeti"ne 5-10 madde ile kisaca tasinir.
- Tamamlanan adima ait uzun notlar/ara kararlar aktif hafizadan kaldirilir.
- Her adim sonunda su soru sorulur: "Sonraki adim icin gerekli minimum bilgi ne?"

---

## Adim 1 - Cekirdek Altyapi ve Mimari Iskelet

**Hedef:** Cozum yapisini kurmak ve katmanlari netlestirmek.

- Solution yapisi: `Domain`, `Application`, `Infrastructure`, `API`
- Temel bagimliliklar: MediatR, FluentValidation, JWT paketleri
- Ortak yapilar: `BaseEntity`, `Result`/`Response` modeli, `Pagination` modeli
- Dependency Injection duzeni
- Swagger/OpenAPI kurulumu (gelistirme ortaminda aktif)
- Ortam ayarlari (appsettings, secrets, environment ayrimi)

**Cikti:** Calisan bos API + clean architecture klasor/katman duzeni + Swagger test ekrani.

---

## Adim 2 - Kimlik Dogrulama ve Yetkilendirme

**Hedef:** Guvenli giris ve rol bazli yetki altyapisini tamamlamak.

- Kullanici, rol, yetki temel domain modelleri
- Register/Login/RefreshToken endpointleri
- JWT olusturma/dogrulama
- Swagger uzerinden Bearer token ile authorize testi
- Role-based authorization policy yapisi
- Temel audit alanlari: `CreatedBy`, `UpdatedBy`

**Cikti:** Guvenli kimlik sistemi ve rol bazli erisim kontrolu.

---

## Adim 3 - Temel ERP Ana Verileri (Master Data + Cari)

**Hedef:** ERP'nin temel referans verilerini ve cari yapisini yonetmek.

- Sirket, sube, depo
- Cari hesap karti (musteri/tedarikci)
- Cari tip, risk limiti, vade ve temel cari kurallari
- Urun, urun kategori, birim
- CRUD endpointleri + CQRS komut/sorgu ayrimi
- Validation ve is kurali kontrolleri

**Cikti:** Temel ERP varliklari ve cari kart yapisi yonetilebilir hale gelir.

---

## Adim 4 - Stok ve Envanter Yonetimi

**Hedef:** Stok hareketlerini ve depo durumunu takip etmek.

- Stok kartlari ve depo bazli stok miktari
- Stok giris/cikis/transfer islemleri
- Kritik stok seviyesi kurali
- Hareket gecmisi (transaction log)
- Stok sorgulari (urun/depo bazli)

**Cikti:** Envanter kontrolu calisir, stok hareketleri izlenir.

---

## Adim 5 - Satin Alma, Satis ve Cari Hareket Surecleri

**Hedef:** Temel ticari akislari aktif etmek.

- Satin alma siparisi (PO) olusturma ve durum takibi
- Satis siparisi (SO) olusturma ve durum takibi
- Irsaliye/fatura icin temel veri akisi hazirligi
- Siparisten stok dusum/artis entegrasyonu
- Cari borc/alacak hareket olusumu (satis/satin alma kaynakli)
- CQRS ile raporlanabilir sorgu modelleri

**Cikti:** Satin alma-satis dongusu ve cari hareket akisinin temeli tamamlanir.

---

## Adim 6 - Finans Temeli, Raporlama ve Operasyonel Saglamlastirma

**Hedef:** Ilk surum icin isletmeye alinabilir temel seviye.

- Tahsilat/odeme kayitlari (temel finans hareketi)
- Cari yaslandirma ve bakiye ozeti
- Basit gelir-gider ozeti
- Temel rapor endpointleri (stok, satis, satin alma, cari bakiye)
- Global exception handling sonlandirma
- Logging, health check, temel testler

**Cikti:** MVP seviyesinde temel ERP API surumu.

---

## Adim Sonu Standarti (Her Adimda Uygulanacak)

1. Yapilanlari 5-10 madde ile "Tamamlandi Ozeti"ne yaz.
2. Karar kayitlarini 3-5 maddeye indir.
3. Gereksiz gecmis notlari aktif hafizadan kaldir.
4. Sonraki adim icin net backlog olustur (maksimum 10 madde).
5. "Hazirlik tamam" kontrolu yapip bir sonraki adima gec.

## Sonraki Genisleme Alanlari (Simdilik Beklemede)

- Uretim (BOM, is emri, rota)
- IK (personel, izin, bordro)
- Gelismis muhasebe (fis, mizan, defter)
- Bildirim altyapisi (mail/sms/in-app)
- Dosya yonetimi ve e-imza/e-fatura entegrasyonlari
