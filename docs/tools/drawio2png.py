#!/usr/bin/env python3
"""
.drawio dosyasindaki her sayfayi SVG'ye cevirir, ardindan bassiz Chrome ile PNG uretir.

Kullanim:
    python3 docs/tools/drawio2png.py docs/architecture.drawio docs/img

Not: draw.io CLI gerektirmez; yalnizca Python 3 ve Chrome/Chromium ister.
"""
import math
import html
import pathlib
import subprocess
import sys
import xml.etree.ElementTree as ET

CHROME_ADAYLARI = [
    "google-chrome", "chromium", "chromium-browser", "chrome",
]


def chrome_bul():
    for ad in CHROME_ADAYLARI:
        try:
            subprocess.run([ad, "--version"], capture_output=True, check=True)
            return ad
        except (FileNotFoundError, subprocess.CalledProcessError):
            continue
    sys.exit("Chrome/Chromium bulunamadi.")


def stil_coz(metin):
    d = {}
    for parca in (metin or "").split(";"):
        if not parca:
            continue
        if "=" in parca:
            k, v = parca.split("=", 1)
            d[k] = v
        else:
            d[parca] = "1"
    return d


def kacir(t):
    return t.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def satirlara_ayir(deger):
    """drawio etiketini [(metin, kalin, italik)] satirlarina cevirir."""
    import re
    v = html.unescape(deger or "")
    v = re.sub(r"<br\s*/?>", "\n", v, flags=re.I)
    karakterler, kalin, italik, i = [], False, False, 0
    while i < len(v):
        m = re.match(r"<(/?)(b|i|font|div|span|strong|em)[^>]*>", v[i:], re.I)
        if m:
            etiket = m.group(2).lower()
            if etiket in ("b", "strong"):
                kalin = not m.group(1)
            if etiket in ("i", "em"):
                italik = not m.group(1)
            i += m.end()
            continue
        karakterler.append((v[i], kalin, italik))
        i += 1

    satirlar, gecerli = [], []
    for c, b, it in karakterler:
        if c == "\n":
            satirlar.append(gecerli)
            gecerli = []
        else:
            gecerli.append((c, b, it))
    satirlar.append(gecerli)
    return satirlar


def parcala(satir):
    out = []
    for c, b, i in satir:
        if out and out[-1][1] == b and out[-1][2] == i:
            out[-1][0] += c
        else:
            out.append([c, b, i])
    return [(t.replace("\xa0", " "), b, i) for t, b, i in out]


def genislik(t, fs, kalin):
    return len(t) * fs * (0.58 if kalin else 0.53)


def sayfayi_cevir(dosya, indeks, cikti_svg):
    kok = ET.parse(dosya).getroot()
    diyagram = kok.findall("diagram")[indeks]
    model = diyagram.find("mxGraphModel")
    W = int(model.get("pageWidth", 1400))
    H = int(model.get("pageHeight", 900))

    hucreler = {}
    for c in model.findall("root/mxCell"):
        g = c.find("mxGeometry")
        hucreler[c.get("id")] = {
            "stil": stil_coz(c.get("style")),
            "deger": c.get("value"),
            "vertex": c.get("vertex") == "1",
            "edge": c.get("edge") == "1",
            "kaynak": c.get("source"),
            "hedef": c.get("target"),
            "geo": g,
            "x": float(g.get("x", 0)) if g is not None else 0,
            "y": float(g.get("y", 0)) if g is not None else 0,
            "w": float(g.get("width", 0)) if g is not None else 0,
            "h": float(g.get("height", 0)) if g is not None else 0,
        }

    p = [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">',
        '<rect width="100%" height="100%" fill="#ffffff"/>',
        '<g font-family="Helvetica, Arial, sans-serif">',
    ]

    def metin(c, x, y, w, h, arka=False):
        s = c["stil"]
        fs = float(s.get("fontSize", 12))
        renk = s.get("fontColor", "#000000")
        satirlar = [parcala(l) for l in satirlara_ayir(c["deger"])]
        satirlar = [l for l in satirlar if any(t.strip() for t, _, _ in l)]
        if not satirlar:
            return
        lh = fs * 1.45
        toplam = len(satirlar) * lh
        if s.get("verticalAlign") == "top":
            by = y + float(s.get("spacingTop", 4)) + fs
        else:
            by = y + (h - toplam) / 2 + fs
        hiza = s.get("align", "center")
        for li, satir in enumerate(satirlar):
            yy = by + li * lh
            gen = sum(genislik(t, fs, b) for t, b, _ in satir)
            sx = x + float(s.get("spacingLeft", 4)) if hiza == "left" else x + (w - gen) / 2
            if arka:
                p.append(f'<rect x="{sx-4}" y="{yy-fs}" width="{gen+8}" height="{fs*1.35}" fill="#ffffff"/>')
            p.append(f'<text x="{sx}" y="{yy}" font-size="{fs}" fill="{renk}">')
            for t, b, i in satir:
                if not t:
                    continue
                st = ' font-weight="bold"' if (b or s.get("fontStyle") == "1") else ""
                st += ' font-style="italic"' if i else ""
                p.append(f"<tspan{st}>{kacir(t)}</tspan>")
            p.append("</text>")

    def kutu(c):
        s, x, y, w, h = c["stil"], c["x"], c["y"], c["w"], c["h"]
        dolgu = s.get("fillColor", "#ffffff")
        cizgi = s.get("strokeColor", "#000000")
        kesik = ' stroke-dasharray="6,4"' if s.get("dashed") == "1" else ""
        kalinlik = s.get("strokeWidth", "1.5")
        if s.get("shape", "").startswith("cylinder"):
            ry = 12
            p.append(f'<path d="M{x},{y+ry} a{w/2},{ry} 0 0 1 {w},0 v{h-2*ry} a{w/2},{ry} 0 0 1 {-w},0 z" '
                     f'fill="{dolgu}" stroke="{cizgi}" stroke-width="{kalinlik}"{kesik}/>')
            p.append(f'<path d="M{x},{y+ry} a{w/2},{ry} 0 0 0 {w},0" fill="none" stroke="{cizgi}" stroke-width="{kalinlik}"/>')
        elif "ellipse" in s:
            p.append(f'<ellipse cx="{x+w/2}" cy="{y+h/2}" rx="{w/2}" ry="{h/2}" '
                     f'fill="{dolgu}" stroke="{cizgi}" stroke-width="{kalinlik}"{kesik}/>')
        else:
            r = 8 if s.get("rounded") == "1" else 0
            p.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{r}" '
                     f'fill="{dolgu}" stroke="{cizgi}" stroke-width="{kalinlik}"{kesik}/>')
        metin(c, x, y, w, h)

    def kenar(c):
        s = c["stil"]
        a, b = hucreler.get(c["kaynak"]), hucreler.get(c["hedef"])
        g = c["geo"]
        ara = []
        dizi = g.find("Array") if g is not None else None
        if dizi is not None:
            ara = [(float(pt.get("x")), float(pt.get("y"))) for pt in dizi.findall("mxPoint")]

        if a is None or b is None:
            sp = g.find('mxPoint[@as="sourcePoint"]') if g is not None else None
            tp = g.find('mxPoint[@as="targetPoint"]') if g is not None else None
            if sp is None or tp is None:
                return
            yol = [(float(sp.get("x")), float(sp.get("y"))), (float(tp.get("x")), float(tp.get("y")))]
        else:
            am = (a["x"] + a["w"] / 2, a["y"] + a["h"] / 2)
            bm = (b["x"] + b["w"] / 2, b["y"] + b["h"] / 2)

            def uc(h_, ox, oy, varsayilan):
                if ox is None:
                    return varsayilan
                return (h_["x"] + float(ox) * h_["w"], h_["y"] + float(oy) * h_["h"])

            if s.get("exitX") is not None:
                p0 = uc(a, s.get("exitX"), s.get("exitY"), am)
            elif abs(bm[0] - am[0]) > abs(bm[1] - am[1]):
                p0 = (a["x"] + a["w"], am[1]) if bm[0] > am[0] else (a["x"], am[1])
            else:
                p0 = (am[0], a["y"] + a["h"]) if bm[1] > am[1] else (am[0], a["y"])

            if s.get("entryX") is not None:
                p1 = uc(b, s.get("entryX"), s.get("entryY"), bm)
            elif abs(bm[0] - am[0]) > abs(bm[1] - am[1]):
                p1 = (b["x"], bm[1]) if bm[0] > am[0] else (b["x"] + b["w"], bm[1])
            else:
                p1 = (bm[0], b["y"]) if bm[1] > am[1] else (bm[0], b["y"] + b["h"])

            yol = [p0] + ara + [p1]

        tam = [yol[0]]
        for i in range(1, len(yol)):
            x0, y0 = tam[-1]
            x1, y1 = yol[i]
            if abs(x0 - x1) > 1 and abs(y0 - y1) > 1:
                if abs(y1 - y0) >= abs(x1 - x0):
                    tam.append((x0, y1))
                else:
                    tam.append((x1, y0))
            tam.append((x1, y1))

        renk = s.get("strokeColor", "#000000")
        kalinlik = s.get("strokeWidth", "1.5")
        kesik = ' stroke-dasharray="7,5"' if s.get("dashed") == "1" else ""
        d = "M" + " L".join(f"{x},{y}" for x, y in tam)
        p.append(f'<path d="{d}" fill="none" stroke="{renk}" stroke-width="{kalinlik}"{kesik}/>')

        if s.get("endArrow") != "none":
            (x0, y0), (x1, y1) = tam[-2], tam[-1]
            aci = math.atan2(y1 - y0, x1 - x0)
            L, Wd = 12, 5.5
            noktalar = [
                (x1, y1),
                (x1 - L * math.cos(aci) + Wd * math.sin(aci), y1 - L * math.sin(aci) - Wd * math.cos(aci)),
                (x1 - L * math.cos(aci) - Wd * math.sin(aci), y1 - L * math.sin(aci) + Wd * math.cos(aci)),
            ]
            p.append('<polygon points="' + " ".join(f"{x},{y}" for x, y in noktalar) + f'" fill="{renk}"/>')

        if c["deger"]:
            mi = max(1, len(tam) // 2)
            mx = (tam[mi - 1][0] + tam[mi][0]) / 2
            my = (tam[mi - 1][1] + tam[mi][1]) / 2
            sahte = {"stil": dict(s, align="center", verticalAlign="middle"), "deger": c["deger"]}
            metin(sahte, mx - 110, my - 12, 220, 24, arka=True)

    for c in hucreler.values():
        if c["edge"]:
            kenar(c)
    for c in hucreler.values():
        if c["vertex"]:
            s = c["stil"]
            if "text" in s and "rounded" not in s and "ellipse" not in s:
                metin(c, c["x"], c["y"], c["w"], c["h"])
            else:
                kutu(c)

    p.append("</g></svg>")
    pathlib.Path(cikti_svg).write_text("\n".join(p))
    return diyagram.get("name") or f"sayfa{indeks+1}"


def ana():
    if len(sys.argv) < 3:
        sys.exit(__doc__)
    kaynak = pathlib.Path(sys.argv[1])
    hedef = pathlib.Path(sys.argv[2])
    hedef.mkdir(parents=True, exist_ok=True)
    chrome = chrome_bul()

    kok = ET.parse(kaynak).getroot()
    sayfalar = kok.findall("diagram")
    print(f"{len(sayfalar)} sayfa bulundu.")

    for i, d in enumerate(sayfalar):
        ad = (d.get("name") or f"sayfa{i+1}").lower().replace(" ", "-")
        svg = hedef / f"{ad}.svg"
        png = hedef / f"{ad}.png"
        sayfayi_cevir(kaynak, i, svg)

        model = d.find("mxGraphModel")
        W = int(model.get("pageWidth", 1400))
        H = int(model.get("pageHeight", 900))
        subprocess.run([
            chrome, "--headless", "--disable-gpu", "--no-sandbox", "--hide-scrollbars",
            "--force-device-scale-factor=2", f"--window-size={W},{H}",
            "--default-background-color=ffffff",
            f"--screenshot={png}", f"file://{svg.resolve()}",
        ], capture_output=True)

        try:
            from PIL import Image, ImageChops
            im = Image.open(png).convert("RGB")
            zemin = Image.new("RGB", im.size, (255, 255, 255))
            kutu_ = ImageChops.difference(im, zemin).getbbox()
            if kutu_:
                pad = 40
                x0, y0, x1, y1 = kutu_
                im.crop((max(0, x0 - pad), max(0, y0 - pad),
                         min(im.width, x1 + pad), min(im.height, y1 + pad))).save(png, optimize=True)
        except ImportError:
            pass

        svg.unlink()
        print(f"  {png.name}")


if __name__ == "__main__":
    ana()
