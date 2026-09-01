#!/usr/bin/env python3
"""
Markdown icindeki mermaid bloklarini PNG'ye cevirir ve blogun yerine gorsel koyar.

Mermaid kaynagi kaybolmaz: her blok docs/diagrams/*.mmd olarak saklanir.
GitHub'in mermaid render'i guvenilir olmadigi icin dokumanlarda PNG kullanilir.

Kullanim:
    python3 docs/tools/mermaid2png.py docs/01-business-analysis.md docs/02-functional-analysis.md
"""
import html
import pathlib
import re
import subprocess
import sys
import unicodedata

MERMAID_CDN = "https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.min.js"
CHROME_ADAYLARI = ["google-chrome", "chromium", "chromium-browser", "chrome"]


def chrome_bul():
    for ad in CHROME_ADAYLARI:
        try:
            subprocess.run([ad, "--version"], capture_output=True, check=True)
            return ad
        except (FileNotFoundError, subprocess.CalledProcessError):
            continue
    sys.exit("Chrome/Chromium bulunamadi.")


def slug(metin):
    metin = unicodedata.normalize("NFKD", metin).encode("ascii", "ignore").decode()
    metin = re.sub(r"[^a-zA-Z0-9]+", "-", metin).strip("-").lower()
    return re.sub(r"-+", "-", metin)[:45] or "diyagram"


# Bu genislikteki diyagramlar dokumanda tam boy gosterilir (ornek: ER semasi)
TAM_BOY_ESIGI = 3000

# Ekranda hedeflenen olculer (CSS pikseli)
HEDEF_GENISLIK = 700
HEDEF_YUKSEKLIK = 560

# Bu genisligin altina inilmez; aksi halde yazilar okunmaz olur
ASGARI_GENISLIK = 430


def gorsel_etiketi(ad, png):
    """Diyagrami dokumana makul bir olcude yerlestiren isaretleme uretir."""
    kaynak_notu = f"<sub>Diyagram kaynagi: `docs/diagrams/{ad}.mmd`</sub>"
    try:
        from PIL import Image
        g, y = Image.open(png).size
    except ImportError:
        return f"![{ad}](img/{ad}.png)\n\n{kaynak_notu}"

    if g >= TAM_BOY_ESIGI:
        return f"![{ad}](img/{ad}.png)\n\n{kaynak_notu}"

    # PNG'ler 2x uretiliyor; dogal CSS olcusu yarisi
    dg, dy = g / 2, y / 2
    genislik = min(HEDEF_GENISLIK, dg)
    if dy * (genislik / dg) > HEDEF_YUKSEKLIK:
        genislik = HEDEF_YUKSEKLIK * dg / dy
    genislik = max(genislik, min(dg, ASGARI_GENISLIK))
    return (f'<img src="img/{ad}.png" alt="{ad}" width="{round(genislik)}">\n\n'
            f"{kaynak_notu}")


def dosyayi_isle(md_yolu, chrome):
    md = pathlib.Path(md_yolu)
    kok = md.parent
    img = kok / "img"
    kaynak = kok / "diagrams"
    img.mkdir(exist_ok=True)
    kaynak.mkdir(exist_ok=True)

    onek = md.stem.split("-")[0]
    metin = md.read_text()

    # Her bloktan once gelen en yakin basligi bul
    parcalar = re.split(r"(```mermaid\n.*?```)", metin, flags=re.S)
    sonuc = []
    sayac = 0

    for parca in parcalar:
        if not parca.startswith("```mermaid"):
            sonuc.append(parca)
            continue

        sayac += 1
        kod = parca[len("```mermaid\n"):-3]

        # Onceki metinden son basligi al
        onceki = "".join(sonuc)
        basliklar = re.findall(r"^#{1,4}\s+(.*)$", onceki, flags=re.M)
        ad = f"{onek}-{sayac:02d}-{slug(basliklar[-1] if basliklar else '')}"

        (kaynak / f"{ad}.mmd").write_text(kod)

        gecici = kok / f"_{ad}.html"
        gecici.write_text(
            f'<!doctype html><html><head><meta charset="utf-8">'
            f'<style>body{{margin:0;padding:24px;background:#fff;width:max-content}}'
            f'.mermaid{{display:inline-block}}</style></head><body>'
            f'<pre class="mermaid">{html.escape(kod)}</pre>'
            f'<script src="{MERMAID_CDN}"></script>'
            f'<script>mermaid.initialize({{startOnLoad:true,theme:"default",'
            f'flowchart:{{useMaxWidth:false}},sequence:{{useMaxWidth:false}},'
            f'er:{{useMaxWidth:false}},state:{{useMaxWidth:false}},'
            f'gantt:{{useMaxWidth:false}}}});</script>'
            f"</body></html>"
        )

        png = img / f"{ad}.png"
        subprocess.run([
            chrome, "--headless", "--disable-gpu", "--no-sandbox", "--hide-scrollbars",
            "--force-device-scale-factor=2", "--window-size=2400,3000",
            "--default-background-color=ffffff", "--virtual-time-budget=15000",
            f"--screenshot={png}", f"file://{gecici.resolve()}",
        ], capture_output=True)
        gecici.unlink()

        try:
            from PIL import Image, ImageChops
            im = Image.open(png).convert("RGB")
            zemin = Image.new("RGB", im.size, (255, 255, 255))
            kutu = ImageChops.difference(im, zemin).getbbox()
            if kutu:
                pad = 24
                x0, y0, x1, y1 = kutu
                im.crop((max(0, x0 - pad), max(0, y0 - pad),
                         min(im.width, x1 + pad), min(im.height, y1 + pad))).save(png, optimize=True)
            print(f"  {png.name}  {Image.open(png).size}")
        except ImportError:
            print(f"  {png.name}")

        sonuc.append(gorsel_etiketi(ad, png))

    md.write_text("".join(sonuc))
    print(f"{md.name}: {sayac} diyagram donusturuldu")


def ana():
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    chrome = chrome_bul()
    for yol in sys.argv[1:]:
        dosyayi_isle(yol, chrome)


if __name__ == "__main__":
    ana()
