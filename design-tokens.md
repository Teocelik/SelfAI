# SelfAI Design Tokens

> **Authoritative reference** — Frontend redesign sprint'i boyunca tüm yeni UI
> çalışmaları bu dosyaya uyumlu olmalı. CLAUDE.md'deki "Liquid Glass" tasarım
> direction'ı ile bu dosya birlikte okunmalı.
>
> Sonraki frontend görevlerinde (F.0, F.2, F.4 …) yeterli talimat:
> *"design-tokens.md PART 2'ye göre liquid glass'la yap."* (bkz. PART 2 sonu →
> Page Architecture)

## Tasarım Felsefesi

**Liquid Glass** — yarı şeffaf cam yüzeyler, refraktif kenarlar, soft glow,
layered depth. Dark background varsayılan, **#00CED1** (turkuaz) accent rengi
cam üzerinden parlar. Reference: Apple Liquid Glass (WWDC 2025), modern
glassmorphism.

İlke: **solid/flat yüzeyler → translucent cam yüzeylere** kademeli geçer.
Mevcut UI bozulmadan, component bazında transition yapılır. Tipografi ve
spacing büyük ölçüde korunur; değişen şey yüzey malzemesidir (surface
material): solid `#1E1E1E` yerine `rgba(255,255,255,0.04)` + `backdrop-filter`.

---

# PART 1 — Current State (Mevcut UI Tokens)

> Mevcut solid/flat UI'da gerçekten kullanılan değerler (tarama sonucu, yeni
> renk üretilmedi). Yeni tasarımlar Liquid Glass tokens'ı kullanır (PART 2).
> Bu bölüm referans + cleanup içindir.

## ⚠️ Kritik Bulgu: İki (aslında üç) paralel renk sistemi

Tarama, kod tabanında **birbirinden habersiz çalışan iki ayrı palet** ortaya
çıkardı. Bu, redesign'ın çözmesi gereken en büyük tutarsızlık:

1. **Template-leftover paleti (indigo/violet/cyan)** — `@dhiwise` / `rocket.new`
   şablonundan miras. **Accent = indigo `#6366f1`**. Şurada yaşıyor:
   `tailwind.config.js`, `tailwind.css` (`:root` değişkenleri), `main.css`
   (scrollbar'lar, model/flux panel selected state'leri), `image-controls.js`,
   `flux-image-styles.js` (slider). Tailwind utility'leri buna bağlı:
   `bg-primary`, `text-primary`, `bg-surface`, `border-border`, `bg-background`.

2. **Ürün/marka paleti (turkuaz)** — gerçek SelfAI markası.
   **Accent = `#00CED1`** (hover `#00B8BC`). Şurada yaşıyor: `Pricing/Index`,
   `RenderNet/Index` floating panel'ler (Face Lock, Character, Pose Lock),
   `PaymentSuccess`, `main.css`'in Face/Pose Lock blokları. Hardcoded
   `bg-[#00CED1]`, `bg-[#1E1E1E]`, `border-[#333]` olarak yazılmış.

3. **Home/Index (Bootstrap dünyası)** — tamamen ayrı. Kendi CDN Bootstrap'ı,
   light navbar (`bg-light`), mor-mavi gradient buton (`#794fff → #2cb0ff`),
   dark modal (`#333` / `#555`). Diğer iki paletle hiç örtüşmüyor.

> **Sonuç:** PART 2 tek bir accent (`#00CED1`) etrafında birleşir; indigo
> `#6366f1` deprecate edilir.

## Mevcut Renkler

### Backgrounds / Surfaces (solid)
| Değer | Kullanım | Kaynak |
|---|---|---|
| `#0A0A0A` | Sayfa background (en koyu) | `Pricing/Index` |
| `#1a1a1a` | Tailwind `bg-background` (body) | `tailwind.config.js`, `tailwind.css` |
| `#1E1E1E` | Kart / panel yüzeyi | Pricing, RenderNet panel'ler, Payment* |
| `#2a2a2a` / `#2A2A2A` | Tailwind `surface`, ikincil yüzey | config + RenderNet, Pricing |
| `#2C2C2C` | Slider track | `main.css`, `flux-image-styles.js` |
| `#333` / `#333333` | Tailwind `surface-elevated`, border, buton | config + her yer |
| `#444` | Border (toggle, thumbnail, scrollbar) | RenderNet panel'ler, `main.css` |
| `#555` | Border (modal form) | Home/Index |

### Accent
| Değer | Kullanım |
|---|---|
| `#00CED1` | **Ürün accent (turkuaz)** — buton, link, aktif state, glow |
| `#00B8BC` | Accent hover (koyu turkuaz) |
| `#6366f1` | **Template accent (indigo)** — slider, scrollbar hover, model/flux selected ⚠️ deprecate |
| `#8b5cf6` | Template secondary (violet) — scrollbar/gradient ⚠️ deprecate |
| `#06b6d4` | Template accent (cyan, config'de tanımlı, fiilen kullanılmıyor) |
| `#794fff` → `#2cb0ff` | Home/Index gradient buton (izole) |

### Text
| Değer | Kullanım |
|---|---|
| `#f8fafc` / `#FAFAFA` / `white` | Primary text |
| `#94a3b8` (`text-secondary`) / `gray-400` | Secondary text |
| `#64748b` (`text-muted`) / `gray-500` / `#888` | Muted text |
| `#aaaaaa` / `#ccc` / `#d1d1d1` | Home/Index muted (izole) |

### Status
| Değer | Kullanım |
|---|---|
| `#10B981` / `#10b981` (emerald) | Success (toast, config) |
| `#EF4444` / `#ef4444` / `#DC2626` (red) | Error / danger (toast, remove butonları) |
| `#F59E0B` / `#f59e0b` (amber) | Warning (toast, config) |
| `#3B82F6` (blue) | Info (toast) |

## Mevcut Tipografi

- **Font family:** `Inter` (sans, ana), `JetBrains Mono` (mono). Google Fonts'tan
  `@import` (`Inter:wght@400;500;600`, `JetBrains Mono:wght@400`).
- **Base font-size:** `site.css` → 14px (mobil) / 16px (≥768px). `html font-size`.
- **Size scale (tailwind.config.js):** `xs 12px / sm 14px / base 16px / lg 18px /
  xl 20px / 2xl 24px / 3xl 30px / 4xl 36px`. Her biri tanımlı line-height ile.
- **Weight kullanımı:** 400 (normal), 500 (`font-medium`), 600 (`font-semibold`),
  700 (`font-bold`). Inter yalnızca 400/500/600 import edildiği için **700
  (`font-bold`) fiilen 600'e düşüyor** — Pricing/Payment başlıkları `font-bold`
  kullanıyor (bkz. Tutarsızlık #3 değil ama not).
- **line-height (body):** 1.6 (`tailwind.css`).

## Mevcut Spacing

- **8px base grid.** Tailwind custom spacing: `xs 4 / sm 8 / md 12 / lg 16 /
  xl 24 / 2xl 32 / 3xl 48 / 4xl 64` (px).
- **En sık padding:** `p-6` (24px — kartlar, formlar, canvas alanı), `p-4` (16px —
  panel section'ları), `px-4 py-3` (panel header/body).
- **En sık gap:** `gap-6` (grid), `gap-3` / `space-y-3`, `space-x-2/3/4`.
- **Container max-width:** `max-w-7xl` (Pricing), `max-w-md` (Payment kartları),
  sağ sidebar `w-96` (384px), floating panel'ler `w-[260px]` / `320px` / `440px` /
  `550px`. _Layout `<main>` max-width koymuyor (kasıtlı, bkz. _Layout yorumu).

## Mevcut Border / Radius / Shadow

- **Border:** `1px solid` — renkler `#333` (en sık), `#444`, `rgba(255,255,255,0.1)`
  (tailwind `border` token). Dashed upload area: `2px dashed #00CED1/60`.
- **Radius (tailwind.config.js):** `sm 4 / md 6 / lg 8 / xl 12 / 2xl 16` (px).
  Fiili kullanım: `rounded-lg` (8px butonlar), `rounded-xl` (12px kartlar),
  `rounded-2xl` (16px Pricing/Payment kartları), `rounded-[10px]` (floating
  panel'ler — hardcoded), `rounded-full` (pill/badge/toggle).
- **Shadow (tailwind.config.js):** `sm/md/lg/xl` (siyah alpha), `floating`
  (`0 4px 12px rgba(0,0,0,0.3)`), `glow-primary` (`0 0 20px rgba(99,102,241,.3)`),
  `glow-accent` (`0 0 20px rgba(6,182,212,.3)`). Panel'lerde fiili:
  `shadow-2xl` ve `0 8px 32px rgba(0,0,0,0.4)`.
- **Mevcut glass denemeleri (önemli — PART 2'nin tohumu):**
  - `tailwind.config.js` plugin → `.glass-effect`: `rgba(42,42,42,0.8)` +
    `backdrop-filter: blur(8px)` + `1px solid rgba(255,255,255,0.1)`.
  - `toast.css` `.toast`: `rgba(30,30,46,0.95)` + `blur(20px)` +
    `1px solid rgba(255,255,255,0.08)` + `0 8px 32px rgba(0,0,0,0.4)`.
  - `backdropBlur` scale config'de: `xs 2 / sm 4 / md 8 / lg 12` (px).

## Mevcut Component Patterns

- **Button (primary):** İki varyant yarışıyor:
  - Template: `.btn-primary` = `bg-primary hover:bg-primary-600 text-white px-6
    py-3 rounded-lg shadow-floating` (indigo).
  - Ürün: `bg-[#00CED1] text-black hover:bg-[#00B8BC] py-3 rounded-lg
    font-semibold` (Pricing "Abone Ol", PaymentSuccess CTA).
  - Beyaz CTA: `bg-white text-black hover:bg-gray-200` (Pricing standart, PaymentFailed).
- **Button (secondary/disabled):** `bg-[#2A2A2A] text-gray-500 cursor-not-allowed`
  (Pricing Free), `bg-surface-elevated hover:bg-primary/10 border border-border`
  (RenderNet consistency butonları).
- **Card:** `.card` = `bg-surface border rounded-xl p-6 shadow-md` (template) **vs.**
  `bg-[#1E1E1E] border border-[#333] rounded-2xl p-6` (Pricing/Payment, fiili).
- **Input:** `.input-field` = `bg-surface border px-4 py-3 rounded-lg
  focus:border-primary focus:ring-2 focus:ring-primary/20` (template, indigo focus).
  Home modal: `bg-#333 border-#555` (Bootstrap).
- **Modal:** Yalnızca Home/Index'te (Bootstrap `.modal` + `bg-dark`). Liquid glass
  modal yok.
- **Toast:** `toast.css` — zaten cam görünümlü (yukarıda). Border-left renkli
  4px accent çizgi (success/error/warning/info).
- **Pill / Badge:** Credit balance: `px-3 py-1 bg-[#2A2A2A] border
  border-[#00CED1]/30 rounded-full text-[#00CED1]` (RenderNet header). "En
  Popüler" badge: `bg-[#00CED1] text-black rounded-full px-4 py-1` (Pricing).
- **Floating Panel:** `fixed bg-[#1E1E1E] border border-[#333] rounded-[10px]
  shadow-2xl` + `transform translate-x-full opacity-0 transition-all duration-300`
  + `.open` state (Face Lock / Character / Pose Lock — tutarlı bir mevcut dil).

## Tutarsızlıklar (Refactor Hedefleri)

> **En kritik 3 tutarsızlık** ve **kilitlenen hedef değerleri** (CLAUDE.md'de
> karara bağlandı). Yeni görevlerde fırsat buldukça migrate edilir.

### 🔴 #1 — İki accent rengi (indigo `#6366f1` vs turkuaz `#00CED1`)
Marka rengi `#00CED1`, ama template kalıntısı `#6366f1` hâlâ slider'larda,
scrollbar hover'larında, model/flux "selected" state'lerinde (`main.css`),
`image-controls.js` ve `flux-image-styles.js`'te aktif. Kullanıcı bir ekranda
turkuaz, başka ekranda indigo vurgu görüyor.
**Karar (kilitli):** Tek accent = **`#00CED1`**. İndigo `#6366f1` ve violet
`#8b5cf6` **tüm referanslardan kaldırılır**. `main.css`'teki scrollbar/model/flux
selected state'leri ve slider JS'leri turkuaza çevrilir (her dosyaya
dokunulduğunda fırsat buldukça).

### 🔴 #2 — Background değeri üç farklı (`#0A0A0A` / `#1a1a1a` / `#1E1E1E`)
`_Layout` body `bg-background` = **`#1a1a1a`** uygular, ama `Pricing/Index`
kendi wrapper'ında **`#0A0A0A`** ile override eder; kartlar **`#1E1E1E`**.
**Karar (kilitli):** Tek page background = **`#0A0A0A`** (PART 2 → Page-Level
Background gradient ile). **`_Layout` body `bg-background` (#1a1a1a) → `#0A0A0A`
migrate edilecek** ve tailwind `background` token'ı `#0A0A0A`'e hizalanacak.
Kart yüzeyleri solid `#1E1E1E` yerine cam yüzeylere (`--glass-surface-base`) geçer.

### 🔴 #3 — Border rengi tutarsız (`#333` vs `#444` vs `rgba(255,255,255,0.1)`)
Aynı görsel hiyerarşide bazen `border-[#333]`, bazen `border-[#444]`, bazen
tailwind `border` token'ı (`rgba(255,255,255,0.1)`) kullanılıyor — çoğu zaman
kasıt olmadan.
**Karar (kilitli):** Liquid Glass'ta border'lar **alpha-based refraktif kenarlara**
geçer (PART 2 → Glass Borders). Solid border gereken yerlerde **tek hedef değer:
`rgba(255, 255, 255, 0.12)`** (mevcut `#333`/`#444`/`rgba(...,0.1)` bununla
değiştirilir).

### Diğer (ikincil — hedefler)
- Card radius `rounded-xl` (12px) vs `rounded-2xl` (16px) karışık → **16px**.
- Floating panel radius `rounded-[10px]` magic number → **16px**'e hizala.
- **Inter weight 700 import edilir** (`font-bold` fiilen 700 çalışsın) — bkz.
  PART 2 → Tipografi.
- Home/Index tamamen ayrı Bootstrap dünyası → redesign'da Liquid Glass'a taşınmalı
  (login dedicated page F.2 kapsamı).
- `image-controls.js` slider gradient'i commented-out (#6366f1) — temizlenebilir.

---

# PART 2 — Target State (Liquid Glass Tokens)

> Yeni frontend görevlerinde **KULLANILACAK** tasarım dili. Mevcut UI bunlara
> kademeli transition yapar. Tüm CSS örnekleri copy-paste edilebilir, tüm
> değerler **kesin** (CLAUDE.md'de kilitlendi — artık "öneri" yok).
>
> **Uygulama notu:** Bu değişkenler `wwwroot/css/tailwind.css`'in `:root`
> bloğuna (mevcut değişkenlerin yanına) eklenecek; component class'ları ise
> `@layer components` içinde veya `main.css`'te tanımlanacak (CLAUDE.md frontend
> separation kuralı). Bu dosya yalnızca referans — kod henüz yazılmadı.

> **Tailwind token stratejisi (kilitli):** Yeni `glass-*` / `--lg-*` token'ları
> ve component class'ları **paralel** eklenir; mevcut `bg-primary` / `bg-surface`
> / `border-border` / `bg-background` token'larına dokunulmaz. Eski class'lar her
> yeni görevde, ilgili view'a dokunulduğunda **fırsat buldukça** Liquid Glass
> karşılıklarına migrate edilir (CLAUDE.md "yapıyı bozmadan eksikleri tamamla"
> ilkesi). Tek seferlik global remap YAPILMAZ.

## Renk Sistemi

Liquid Glass'ta renk iki katmandan oluşur: **(1) opak temel renkler** (page bg,
text, status) + **(2) translucent cam katmanı** (yüzeyler alpha + blur ile).

```css
:root {
    /* ─── Temel (opak) renkler ─── */
    --lg-bg: #0A0A0A;                  /* Sayfa zemini (en koyu) — TEK background base */
    --lg-bg-raised: #121212;           /* Hafif yükseltilmiş zemin (opsiyonel) */
    /* Page-level aurora mesh (bkz. Page-Level Background bölümü) — F.4.3b */
    --aurora-bg:
        radial-gradient(ellipse 80% 60% at 20% 30%, rgba(0, 206, 209, 0.15), transparent 60%),
        radial-gradient(ellipse 70% 50% at 80% 25%, rgba(56, 189, 248, 0.10), transparent 60%),
        radial-gradient(ellipse 100% 60% at 50% 90%, rgba(0, 206, 209, 0.08), transparent 70%),
        radial-gradient(ellipse 50% 40% at 95% 60%, rgba(34, 211, 238, 0.07), transparent 60%),
        #0A0A0A;

    --lg-accent: #00CED1;              /* Marka turkuaz — TEK accent */
    --lg-accent-hover: #00B8BC;        /* Koyu turkuaz (hover/pressed) */
    --lg-accent-bright: #2FE4E7;       /* Açık turkuaz (glow/highlight için) */

    /* ─── Metin ─── */
    --lg-text-primary: #FAFAFA;
    --lg-text-secondary: rgba(255, 255, 255, 0.65);
    --lg-text-muted: rgba(255, 255, 255, 0.45);

    /* ─── Status ─── */
    --lg-success: #10B981;
    --lg-error: #EF4444;
    --lg-warning: #F59E0B;
    --lg-info: #3B82F6;
}
```

> **Accent (kilitli):** Tek accent `#00CED1`. İndigo ve violet template
> renkleri Liquid Glass'tan **tamamen kaldırıldı** — bu paletten kullanılmaz.
> Cam üzerinde yalnızca turkuaz parlar.

## Glass Surface Variants

**Karar (kilitli): Nötr cam yüzeyler için BEYAZ ALPHA varsayılan.** Beyaz alpha,
yüzeyin altındaki içeriği nötr biçimde aydınlatır, her bağlamda tutarlıdır ve
accent rengini boğmaz (Apple Liquid Glass varsayılanı). **Turkuaz tint yalnızca
vurgulu/seçili/aktif öğelerde** ayrı bir token (`--glass-surface-accent`) ile
kullanılır — örn. seçili model kartı, aktif consistency butonu, seçili paket.
Böylece cam nötr kalır, accent gerçekten "parladığında" anlamlı olur. Her yüzeye
turkuaz tint UI'ı yeşilimsi yapar ve status renkleriyle (yeşil success) çakışır —
**bu yüzden yapılmaz.**

```css
:root {
    /* Nötr cam yüzeyler (beyaz alpha) — VARSAYILAN */
    --glass-surface-base: rgba(255, 255, 255, 0.04);     /* standart kart */
    --glass-surface-elevated: rgba(255, 255, 255, 0.08); /* modal/popover */
    --glass-surface-hover: rgba(255, 255, 255, 0.12);    /* hover */
    --glass-surface-active: rgba(255, 255, 255, 0.16);   /* pressed/active */

    /* Accent tint'li cam — yalnızca SEÇİLİ/AKTİF öğeler */
    --glass-surface-accent: rgba(0, 206, 209, 0.10);
    --glass-surface-accent-hover: rgba(0, 206, 209, 0.16);
}
```

> Not: Beyaz alpha yüzeyler `#0A0A0A` zemin üzerinde çok koyu durur ama
> `backdrop-filter: blur` arkadaki içeriği (gradient, görsel) yukarı çektiği için
> gerçek cam etkisi blur ile birlikte ortaya çıkar. Bu yüzden page-level
> background gradient (aşağıda) Liquid Glass'ın ayrılmaz parçasıdır.

## Backdrop Blur Scale

```css
:root {
    --blur-light: 8px;    /* küçük öğeler: pill, badge, küçük buton */
    --blur-medium: 16px;  /* kart, panel, input */
    --blur-heavy: 24px;   /* modal, full-screen overlay */
}
```

Kullanım her zaman `-webkit-` prefix'i ile birlikte (Safari):
```css
backdrop-filter: blur(var(--blur-medium));
-webkit-backdrop-filter: blur(var(--blur-medium));
```

## Glass Borders (refraktif kenarlar)

Cam kenarı ışığı yakalar — ince, açık renkli 1px border. Accent kenar yalnızca
vurgu/focus için.

```css
:root {
    --glass-border-subtle: 1px solid rgba(255, 255, 255, 0.08);
    --glass-border-medium: 1px solid rgba(255, 255, 255, 0.14);
    --glass-border-accent: 1px solid rgba(0, 206, 209, 0.40);
}
```

## Glass Highlights (top-edge light catch)

Camın üst kenarına vuran ışık efekti. İki teknik var: **`::before`
pseudo-element** yöntemi daha kontrollü (üst kenara ince bir parlama şeridi) —
**kart/modal/panel'lerde varsayılan budur**. **`inset box-shadow`** yöntemi tek
satırda hızlı çözüm — küçük öğelerde (buton, pill) kullanılır.

**Yöntem 1 — `::before` ile üst kenar gradient şeridi (varsayılan):**
```css
.glass-card {
    position: relative; /* ::before için şart */
}
.glass-card::before {
    content: "";
    position: absolute;
    inset: 0;
    border-radius: inherit;
    padding: 1px;                 /* kenar kalınlığı */
    background: linear-gradient(
        180deg,
        rgba(255, 255, 255, 0.18) 0%,   /* üst kenar parlak */
        rgba(255, 255, 255, 0.02) 35%,  /* hızla söner */
        transparent 100%
    );
    /* sadece kenarı boya, içi değil (mask trick) */
    -webkit-mask: linear-gradient(#000 0 0) content-box, linear-gradient(#000 0 0);
    -webkit-mask-composite: xor;
            mask-composite: exclude;
    pointer-events: none;
}
```

**Yöntem 2 — `inset box-shadow` ile (basit alternatif):**
```css
/* üstte ince beyaz çizgi + altta hafif gölge derinlik */
box-shadow:
    inset 0 1px 0 0 rgba(255, 255, 255, 0.15),
    inset 0 -1px 0 0 rgba(0, 0, 0, 0.20);
```

## Glow Effects

Accent rengiyle yumuşak ışıma (focus, hover, aktif state).

```css
:root {
    --glow-accent-soft: 0 0 24px rgba(0, 206, 209, 0.15);
    --glow-accent-medium: 0 0 40px rgba(0, 206, 209, 0.25);
    --glow-accent-intense: 0 0 60px rgba(0, 206, 209, 0.40); /* focus/active */
}
```

Derinlik gölgesiyle birlikte (cam havada duruyor hissi):
```css
box-shadow:
    0 8px 32px rgba(0, 0, 0, 0.40),   /* drop shadow (derinlik) */
    var(--glow-accent-soft);          /* accent ışıma */
```

## Gradient Overlays

Cam yüzey üzerinde üstten alta hafif aydınlanma — camın "hacmini" verir.

```css
:root {
    /* Yüzey iç gradient'i (kart background ile birlikte layered) */
    --glass-overlay-top: linear-gradient(
        180deg,
        rgba(255, 255, 255, 0.06) 0%,
        rgba(255, 255, 255, 0.00) 40%
    );
    /* Accent buton için cam + turkuaz birleşimi */
    --glass-gradient-accent: linear-gradient(
        135deg,
        rgba(0, 206, 209, 0.25) 0%,
        rgba(0, 206, 209, 0.10) 100%
    );
}
```

## Page-Level Background (kilitli — F.4.3b aurora mesh)

**Karar: `#0A0A0A` + çok katmanlı "aurora mesh" radial gradient (buz-tonu).**
Tek minimal radial gradient, glass yüzeylerin arkasında yeterli renk çeşitliliği
sağlamıyordu — `backdrop-filter: blur` boşa harcanıyor, oval kenarlardaki
refraksiyon (cam büyüteç etkisi) görünmüyordu. Çözüm: birden çok radial katmanı
farklı konum/boyutta üst üste bindiren bir mesh. Palette **yalnızca buz-tonu**:
turkuaz `#00CED1` + sky-blue `#38BDF8` + cyan `#22D3EE`. **İndigo/mor/sıcak ton
YOK.** Statik (animasyon yok, CPU dostu).

`--lg-bg` (`#0A0A0A`) base rengi olarak mesh'in en altında kalır. Tüm sayfalar
zemini **body'den** alır (tek doğruluk kaynağı): `_Layout` → `body.bg-background`,
`_AuthLayout` → `body.page-bg`. Page wrapper'ların (`.studio-page`, `.pricing-page`,
landing body bg, login `.page-bg`) kendi zemini **kaldırıldı** — body sağlar.

```css
:root {
    --aurora-bg:
        /* Sol-üst: ana turkuaz parlaması */
        radial-gradient(ellipse 80% 60% at 20% 30%, rgba(0, 206, 209, 0.15), transparent 60%),
        /* Sağ-üst: buz mavisi (sky-blue) */
        radial-gradient(ellipse 70% 50% at 80% 25%, rgba(56, 189, 248, 0.10), transparent 60%),
        /* Alt-orta: yumuşak turkuaz devamı */
        radial-gradient(ellipse 100% 60% at 50% 90%, rgba(0, 206, 209, 0.08), transparent 70%),
        /* Sağ-orta: derin cyan vurgu */
        radial-gradient(ellipse 50% 40% at 95% 60%, rgba(34, 211, 238, 0.07), transparent 60%),
        /* Base */
        #0A0A0A;
}

body.bg-background,
body.page-bg {
    background: var(--aurora-bg);
    /* Scroll'da aurora viewport'a sabit → glass kartlar farklı renk bölgeleri
       üzerinden geçerek kenarlarda refraksiyon (cam büyüteç) gösterir. */
    background-attachment: fixed;
    min-height: 100vh;
}
```

> Eski tek katmanlı `--lg-bg-gradient`
> (`radial-gradient(ellipse at top, rgba(0,206,209,0.05), #0A0A0A 60%)`) F.4.3b ile
> emekliye ayrıldı; yerini `--aurora-bg` aldı. (landing.css/login.css hâlâ kendi
> `--lg-bg-gradient` token'ını tanımlıyor ama artık kullanmıyor — zemini body verir.)

## Tipografi (büyük ölçüde korunur)

Mevcut tipografi **korunuyor** — Liquid Glass yüzey malzemesini değiştirir,
yazı sistemini değil.

- **Font:** `Inter` (sans), `JetBrains Mono` (mono) — aynen kal.
- **Size scale:** mevcut tailwind scale (`xs`–`4xl`) aynen kal.
- **Inter weight 700 import edilir (kilitli):** Mevcut import yalnızca 400/500/600
  içeriyor, bu yüzden `font-bold` (700) fiilen 600'e düşüyordu. Import 700'ü de
  kapsayacak şekilde güncellenir — artık `font-bold` gerçek 700 çalışır:

  ```css
  /* tailwind.css en üstündeki @import güncellenir */
  @import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');
  ```

  Cam üzerinde başlıklar için isteğe bağlı `letter-spacing: -0.01em` daha
  "premium" durur.
- **Cam üzerinde okunabilirlik:** Açık metni cam üzerinde net tutmak için
  gerektiğinde `text-shadow: 0 1px 2px rgba(0,0,0,0.4)` (mevcut `.text-shadow-sm`
  utility'si zaten var).

## Spacing (korunur)

Mevcut 8px-base spacing scale ve `p-6` / `p-4` / `gap-6` alışkanlıkları **aynen
korunur**. Cam kartlarda iç padding `24px` (`p-6`) varsayılan; küçük pill/badge
`px-3 py-1`. Radius standardı: **kartlar/paneller `16px` (rounded-2xl)**,
butonlar/input `12px`, pill `9999px` (full).

## Component Patterns (CSS örnekleriyle)

> Tüm örnekler PART 2 değişkenlerini kullanır. Üretimde bu bloklar `main.css`
> veya `tailwind.css @layer components` içine girer (inline `<style>` YASAK —
> CLAUDE.md kuralı).

### Glass Card
```css
.glass-card {
    position: relative;
    background: var(--glass-surface-base);
    background-image: var(--glass-overlay-top);
    backdrop-filter: blur(var(--blur-medium));
    -webkit-backdrop-filter: blur(var(--blur-medium));
    border: var(--glass-border-medium);
    border-radius: 16px;
    padding: 24px;
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.40);
    transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}
/* top-edge highlight */
.glass-card::before {
    content: "";
    position: absolute;
    inset: 0;
    border-radius: inherit;
    padding: 1px;
    background: linear-gradient(180deg, rgba(255,255,255,0.18), transparent 35%);
    -webkit-mask: linear-gradient(#000 0 0) content-box, linear-gradient(#000 0 0);
    -webkit-mask-composite: xor;
            mask-composite: exclude;
    pointer-events: none;
}
.glass-card:hover {
    background: var(--glass-surface-hover);
    border-color: rgba(0, 206, 209, 0.25);
    box-shadow: 0 8px 32px rgba(0,0,0,0.40), var(--glow-accent-soft);
    transform: translateY(-2px);
}
```

### Glass Button (primary)
```css
.glass-btn-primary {
    position: relative;
    background: var(--glass-gradient-accent);
    backdrop-filter: blur(var(--blur-light));
    -webkit-backdrop-filter: blur(var(--blur-light));
    border: var(--glass-border-accent);
    border-radius: 12px;
    padding: 12px 24px;
    color: #FAFAFA;
    font-weight: 600;
    cursor: pointer;
    box-shadow: var(--glow-accent-soft);
    transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}
.glass-btn-primary:hover {
    background: linear-gradient(135deg, rgba(0,206,209,0.35), rgba(0,206,209,0.18));
    box-shadow: var(--glow-accent-medium);
    transform: scale(1.02);
}
.glass-btn-primary:active {
    transform: scale(0.99);
    box-shadow: var(--glow-accent-intense);
}
.glass-btn-primary:disabled {
    opacity: 0.45;
    cursor: not-allowed;
    box-shadow: none;
    transform: none;
}
```

### Glass Button (secondary)
```css
.glass-btn-secondary {
    background: var(--glass-surface-base);
    backdrop-filter: blur(var(--blur-light));
    -webkit-backdrop-filter: blur(var(--blur-light));
    border: var(--glass-border-medium);
    border-radius: 12px;
    padding: 12px 24px;
    color: var(--lg-text-secondary);
    font-weight: 500;
    cursor: pointer;
    transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}
.glass-btn-secondary:hover {
    background: var(--glass-surface-hover);
    border-color: rgba(0, 206, 209, 0.30);
    color: #FAFAFA;
    box-shadow: var(--glow-accent-soft);
}
```

### Glass Input
```css
.glass-input {
    width: 100%;
    background: var(--glass-surface-base);
    backdrop-filter: blur(var(--blur-light));
    -webkit-backdrop-filter: blur(var(--blur-light));
    border: var(--glass-border-subtle);
    border-radius: 12px;
    padding: 12px 16px;
    color: #FAFAFA;
    transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}
.glass-input::placeholder { color: var(--lg-text-muted); }
.glass-input:focus {
    outline: none;
    background: var(--glass-surface-hover);
    border-color: rgba(0, 206, 209, 0.50);
    box-shadow: var(--glow-accent-soft);
}
```

### Glass Modal
```css
/* Sayfa karartma overlay'i */
.glass-modal-backdrop {
    position: fixed;
    inset: 0;
    background: rgba(0, 0, 0, 0.60);
    backdrop-filter: blur(4px);          /* arka planı hafif bulanıklaştır */
    -webkit-backdrop-filter: blur(4px);
    z-index: 1000;
    display: flex;
    align-items: center;
    justify-content: center;
    animation: fadeIn 0.2s ease-out;
}
/* Cam modal gövdesi */
.glass-modal {
    position: relative;
    background: var(--glass-surface-elevated);
    background-image: var(--glass-overlay-top);
    backdrop-filter: blur(var(--blur-heavy));
    -webkit-backdrop-filter: blur(var(--blur-heavy));
    border: var(--glass-border-medium);
    border-radius: 20px;
    padding: 32px;
    max-width: 480px;
    width: calc(100% - 32px);
    box-shadow: 0 24px 64px rgba(0,0,0,0.55), var(--glow-accent-soft);
    animation: slideUp 0.3s cubic-bezier(0.16, 1, 0.3, 1);
}
.glass-modal::before { /* top-edge highlight — .glass-card ile aynı */
    content: ""; position: absolute; inset: 0; border-radius: inherit; padding: 1px;
    background: linear-gradient(180deg, rgba(255,255,255,0.18), transparent 35%);
    -webkit-mask: linear-gradient(#000 0 0) content-box, linear-gradient(#000 0 0);
    -webkit-mask-composite: xor; mask-composite: exclude; pointer-events: none;
}
```

### Glass Pill / Badge
```css
/* Örnek: credit balance göstergesi */
.glass-pill {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    background: var(--glass-surface-base);
    backdrop-filter: blur(var(--blur-light));
    -webkit-backdrop-filter: blur(var(--blur-light));
    border: var(--glass-border-accent);
    border-radius: 9999px;
    padding: 4px 14px;
    font-size: 13px;
    font-weight: 500;
    color: var(--lg-accent);
    box-shadow: var(--glow-accent-soft);
}
/* Dolu/vurgulu badge (örn. "En Popüler") */
.glass-badge-accent {
    background: var(--glass-gradient-accent);
    border: var(--glass-border-accent);
    border-radius: 9999px;
    padding: 4px 16px;
    font-size: 12px;
    font-weight: 700;
    color: #FAFAFA;
    box-shadow: var(--glow-accent-medium);
}
```

### Glass Sidebar / Panel
```css
/* Studio floating panel'ler: Face Lock, Character, Pose Lock, Flux Style */
.glass-panel {
    position: fixed;
    background: var(--glass-surface-elevated);
    background-image: var(--glass-overlay-top);
    backdrop-filter: blur(var(--blur-heavy));
    -webkit-backdrop-filter: blur(var(--blur-heavy));
    border: var(--glass-border-medium);
    border-radius: 16px;
    box-shadow: 0 8px 32px rgba(0, 0, 0, 0.45);
    /* mevcut açılış animasyonu KORUNUR (translate-x + opacity) */
    transform: translateX(100%);
    opacity: 0;
    transition: transform 0.3s cubic-bezier(0.4,0,0.2,1),
                opacity 0.3s cubic-bezier(0.4,0,0.2,1);
}
.glass-panel.open {
    transform: translateX(0);
    opacity: 1;
}
.glass-panel::before { /* top-edge highlight */
    content: ""; position: absolute; inset: 0; border-radius: inherit; padding: 1px;
    background: linear-gradient(180deg, rgba(255,255,255,0.16), transparent 30%);
    -webkit-mask: linear-gradient(#000 0 0) content-box, linear-gradient(#000 0 0);
    -webkit-mask-composite: xor; mask-composite: exclude; pointer-events: none;
}
/* Panel header ayırıcı — solid border yerine alpha */
.glass-panel-divider { border-bottom: var(--glass-border-subtle); }
```

> **Mevcut davranışı koru:** Floating panel'lerin `translate-x-full opacity-0`
> → `.open` açılış animasyonu ve `right: 400px` konumlandırması mevcut JS'e bağlı.
> Sadece **yüzey malzemesini** (solid `#1E1E1E` → cam) değiştir, JS/konum dokunma.

## Hover/Focus State Pattern'leri

Tüm interaktif cam öğeler "alive" hissi verir. Standart reçete:

- **Transition:** `transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1)` (mevcut
  panel easing'i ile aynı) veya Tailwind `transition-all duration-300`.
- **Surface lift:** hover'da yüzey bir kademe açılır
  (`base → hover`, `hover → active`).
- **Border accent kayması:** `rgba(255,255,255,*)` → `rgba(0,206,209,*)`.
- **Glow yoğunlaşması:** `soft → medium → intense` (rest → hover → active/focus).
- **Subtle scale:** hover `scale(1.02)`, active `scale(0.99)`. Kartlarda
  `translateY(-2px)` tercih edilir (scale yerine).
- **Focus:** bkz. aşağıdaki **Focus State Pattern** (kilitli kural).

```css
/* genel yardımcı */
.glass-interactive {
    transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}
.glass-interactive:hover {
    background: var(--glass-surface-hover);
    border-color: rgba(0, 206, 209, 0.30);
    box-shadow: var(--glow-accent-soft);
}
```

### Focus State Pattern (kilitli — erişilebilirlik)

**Karar:** Tüm interaktif öğelerde (input, buton, link, seçilebilir kart) focus
göstergesi = **`:focus-visible` ile 2px ring `rgba(0, 206, 209, 0.5)` + accent
glow**, birlikte. Glow tek başına yeterli kontrast sağlamaz; klavye kullanıcısı
için net, ayrık bir ring şarttır (WCAG 2.4.7 focus visibility). `:focus-visible`
kullanıldığı için ring yalnızca klavye/erişilebilirlik focus'unda görünür,
mouse tıklamasında görünmez (UX temiz kalır).

Ring `box-shadow` ile çizilir (layout'u kaydırmaz) ve glow ile aynı `box-shadow`
listesinde birleştirilir:

```css
/* Tüm cam interaktif öğeler için ortak focus reçetesi */
.glass-input:focus-visible,
.glass-btn-primary:focus-visible,
.glass-btn-secondary:focus-visible,
.glass-interactive:focus-visible {
    outline: none; /* tarayıcı varsayılan outline'ı kapat */
    border-color: rgba(0, 206, 209, 0.50);
    box-shadow:
        0 0 0 2px rgba(0, 206, 209, 0.5),  /* 2px accent ring */
        var(--glow-accent-medium);          /* + yumuşak glow */
}

/* Koyu zeminde ring'i zeminden ayırmak istenirse (opsiyonel offset) */
.glass-card:focus-visible {
    outline: none;
    box-shadow:
        0 0 0 2px rgba(10, 10, 10, 1),      /* zemin rengiyle 2px ayraç */
        0 0 0 4px rgba(0, 206, 209, 0.5),   /* sonra accent ring */
        var(--glow-accent-soft);
}
```

> Not: `:focus` yerine `:focus-visible` kullan. Eski tarayıcı fallback'i için
> `:focus { outline: none; }` ile birlikte `:focus-visible` tanımla.

## Browser Support & Fallback

- `backdrop-filter`: **Chrome 76+, Safari 9+ (`-webkit-` prefix şart), Firefox
  103+, Edge 79+.** Kapsam bugün ~%96+. Mobil Safari/Chrome destekli.
- **`-webkit-backdrop-filter` prefix'ini her zaman yaz** (Safari/iOS hâlâ ister).
- **Fallback (blur desteklenmezse):** Cam yüzeyler tamamen şeffaf görünüp
  okunamaz hâle gelmesin diye `@supports not` ile opak fallback ver:

```css
@supports not ((backdrop-filter: blur(1px)) or (-webkit-backdrop-filter: blur(1px))) {
    .glass-card, .glass-panel, .glass-modal {
        background: #16181B;          /* opak fallback yüzey */
        border-color: rgba(255, 255, 255, 0.12);
    }
}
```

- **mask-composite** (top-edge highlight `::before`): Chrome/Safari/Firefox
  güncel sürümlerde destekli. Desteklenmezse efekt görünmez ama layout
  bozulmaz (progressive enhancement) — kritik değil.
- **Performans:** `backdrop-filter` GPU-yoğun. Aynı anda çok sayıda blur'lu
  katmanı (örn. uzun listede her kart) sınırla; gerekiyorsa liste item'larında
  `--blur-light` kullan.

## Page Architecture (gelecek görev hedefleri)

> **Informational — token değil.** Affogato benzeri sayfa yapısı (CLAUDE.md
> vizyonu). Liquid Glass tokens'ı bu sayfalara uygulanacak. Sıra/numaralandırma
> kesinleşince güncellenir; aşağısı yönlendirici kapsam.

- **F.0 — Landing:** Public giriş sayfası. Hero + paket vurgusu + CTA. Liquid
  Glass kartlar, page-level gradient background, glass-btn-primary CTA.
- **F.2 — Login (dedicated page):** Mevcut Home/Index Bootstrap modal'ı yerine
  ayrı, tam sayfa Liquid Glass login. Email-only Firebase Auth akışı (CLAUDE.md
  kilitli karar). Glass card + glass-input + glass-btn-primary.
- **F.4 — Studio (sidebar nav):** Ana üretim ekranı. Sol/sabit **sidebar
  navigation** ile bölümler: **Studio / Canvas / Characters / Pricing**. Mevcut
  floating panel'ler (Face Lock, Character, Pose Lock, Flux Style) cam yüzeye
  geçer; sidebar Liquid Glass panel dili (`.glass-panel`) ile kurulur. Studio,
  Liquid Glass'ın showcase'i olarak başlar (CLAUDE.md).

> Bu üç görev `design-tokens.md` PART 2'yi tek referans alır. Her birinde:
> "design-tokens.md PART 2'ye göre liquid glass'la yap" yeterli talimattır.
