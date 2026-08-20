---
name: Industrial Flow Intelligence
colors:
  surface: '#f7f9fb'
  surface-dim: '#d8dadc'
  surface-bright: '#f7f9fb'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f2f4f6'
  surface-container: '#eceef0'
  surface-container-high: '#e6e8ea'
  surface-container-highest: '#e0e3e5'
  on-surface: '#191c1e'
  on-surface-variant: '#42474f'
  inverse-surface: '#2d3133'
  inverse-on-surface: '#eff1f3'
  outline: '#727780'
  outline-variant: '#c2c7d1'
  surface-tint: '#2d6197'
  primary: '#00355f'
  on-primary: '#ffffff'
  primary-container: '#0f4c81'
  on-primary-container: '#8ebdf9'
  inverse-primary: '#a0c9ff'
  secondary: '#48626e'
  on-secondary: '#ffffff'
  secondary-container: '#cbe7f5'
  on-secondary-container: '#4e6874'
  tertiary: '#532800'
  on-tertiary: '#ffffff'
  tertiary-container: '#743b00'
  on-tertiary-container: '#f9a767'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#d2e4ff'
  primary-fixed-dim: '#a0c9ff'
  on-primary-fixed: '#001c37'
  on-primary-fixed-variant: '#07497d'
  secondary-fixed: '#cbe7f5'
  secondary-fixed-dim: '#afcbd8'
  on-secondary-fixed: '#021f29'
  on-secondary-fixed-variant: '#304a55'
  tertiary-fixed: '#ffdcc4'
  tertiary-fixed-dim: '#ffb780'
  on-tertiary-fixed: '#2f1400'
  on-tertiary-fixed-variant: '#6f3800'
  background: '#f7f9fb'
  on-background: '#191c1e'
  surface-variant: '#e0e3e5'
typography:
  display-lg:
    fontFamily: Inter
    fontSize: 32px
    fontWeight: '700'
    lineHeight: 40px
  headline-md:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
  title-sm:
    fontFamily: Inter
    fontSize: 18px
    fontWeight: '600'
    lineHeight: 24px
  body-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  data-tabular:
    fontFamily: JetBrains Mono
    fontSize: 13px
    fontWeight: '500'
    lineHeight: 16px
  label-caps:
    fontFamily: Inter
    fontSize: 11px
    fontWeight: '700'
    lineHeight: 16px
    letterSpacing: 0.05em
rounded:
  sm: 0.125rem
  DEFAULT: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  xl: 0.75rem
  full: 9999px
spacing:
  base: 4px
  container-padding: 24px
  gutter: 16px
  stack-sm: 8px
  stack-md: 16px
  stack-lg: 32px
---

## Brand & Style

Sistem desain ini difokuskan pada presisi, reliabilitas, dan efisiensi operasional untuk sektor distribusi energi. Karakter visualnya mencerminkan stabilitas industri dengan pendekatan **Corporate / Modern** yang mengutamakan kepadatan data tanpa mengorbankan keterbacaan.

Target audiens utamanya adalah operator logistik dan manajer fasilitas yang membutuhkan visibilitas real-time terhadap stok bahan bakar. Antarmuka harus memberikan kesan otoriter namun intuitif, menggunakan ruang negatif secara strategis untuk memisahkan metrik-metrik kritis. Estetika yang diusung bersifat utilitarian, bersih, dan berstandar enterprise.

## Colors

Palet warna disusun berdasarkan hierarki industri. **Primary Blue** (#0F4C81) digunakan untuk elemen navigasi dan aksi utama untuk memberikan kesan kokoh. Warna netral berbasis Slate/Grey digunakan sebagai latar belakang area kerja untuk mengurangi kelelahan mata selama pemantauan jangka panjang.

Warna semantik sangat krusial dalam sistem ini:
- **Success (Green):** Menunjukkan kapasitas stok aman (>60%).
- **Warning (Yellow):** Menunjukkan ambang batas pengisian ulang (20% - 60%).
- **Danger (Red):** Menunjukkan kondisi kritis atau tangki kosong (<20%).
- **Info (Blue):** Digunakan untuk status teknis atau pemeliharaan terjadwal.

## Typography

Sistem tipografi menggunakan **Inter** sebagai fondasi utama karena kejelasan bentuk hurufnya pada ukuran kecil di layar resolusi tinggi. Untuk nilai numerik dan data sensor pada tabel, digunakan **JetBrains Mono** guna memastikan perataan angka yang presisi (tabular figures), memudahkan operator membandingkan volume liter secara vertikal.

Gunakan `label-caps` untuk header kolom tabel dan kategori kecil. Gunakan `display-lg` hanya untuk ringkasan total volume stok pada level dashboard utama.

## Layout & Spacing

Sistem ini menggunakan **Fixed Grid** 12-kolom untuk dashboard desktop dengan lebar konten maksimal 1440px. Layout disusun secara modular menggunakan "Card-based UI" untuk membungkus setiap widget data.

Rhythm spasial berbasis kelipatan 4px. Margin antar card ditetapkan sebesar 16px (gutter) untuk memaksimalkan kepadatan informasi tanpa terlihat sesak. Area kerja utama memiliki padding luar sebesar 24px untuk memberikan ruang napas visual terhadap sidebar navigasi.

## Elevation & Depth

Kedalaman visual diatur melalui **Tonal Layers** dan border yang sangat halus. Tidak disarankan menggunakan shadow yang berat; gunakan shadow dengan blur luas dan opasitas rendah (2-4%) hanya untuk membedakan card yang bersifat interaktif.

- **Level 0 (Base):** Latar belakang aplikasi menggunakan warna `neutral` (#F8FAFC).
- **Level 1 (Surface):** Card dan panel menggunakan warna putih murni (#FFFFFF) dengan border 1px solid (#E2E8F0).
- **Level 2 (Overlay):** Dropdown, modal, dan popover menggunakan bayangan halus untuk memisahkan elemen dari konten di bawahnya.

## Shapes

Bentuk elemen menggunakan pendekatan **Soft** (radius 4px). Sudut yang sedikit membulat memberikan kesan modern namun tetap mempertahankan karakteristik industrial yang kaku dan profesional. Radius yang lebih besar (8px-12px) hanya diperbolehkan untuk komponen berukuran besar seperti Modal atau Main Card Container.

## Components

### Buttons
- **Primary:** Background `primary_color_hex`, teks putih. Tanpa gradien.
- **Secondary:** Outline 1px dengan warna `primary_color_hex`.
- **Status-based:** Tombol aksi darurat menggunakan warna `danger`.

### Data Cards & Progress Bars
Card harus menampilkan metrik utama secara menonjol. Progress bar untuk kapasitas stok menggunakan tinggi 8px dengan latar belakang abu-abu terang. Warna bar harus berubah secara dinamis mengikuti logika warna semantik (Hijau/Kuning/Merah) berdasarkan persentase isi tangki.

### Status Badges
Gunakan gaya "Pill-shaped" dengan latar belakang opasitas 10% dari warna semantik dan teks dengan warna semantik penuh. Contoh: Badge "Stok Aman" menggunakan background hijau muda 10% dengan teks hijau gelap.

### Data Tables
Baris tabel harus memiliki tinggi minimal 48px dengan border bawah tipis. Gunakan zebra-striping (selang-seling warna) sangat tipis untuk tabel dengan lebih dari 20 baris guna membantu penelusuran mata.

### Interactive Charts
Grafik garis (line chart) untuk tren distribusi harus menggunakan ketebalan garis 2px dengan area fill transparan di bawahnya. Sumbu X dan Y menggunakan font `data-tabular`.