#!/usr/bin/env python3
"""Analiz dokumanlarini tek bir HTML dosyasina donusturur (diyagramlar dahil)."""
import html
import pathlib
import re

KLASOR = pathlib.Path(__file__).parent
CIKTI = KLASOR / "onizleme.html"


def md_to_html(metin):
    """Kucuk bir Markdown donusturucu: baslik, tablo, kod, liste, alinti."""
    parcalar = []
    i = 0
    satirlar = metin.split("\n")

    while i < len(satirlar):
        s = satirlar[i]

        # mermaid blogu
        if s.strip() == "```mermaid":
            i += 1
            kod = []
            while i < len(satirlar) and satirlar[i].strip() != "```":
                kod.append(satirlar[i])
                i += 1
            parcalar.append(
                '<pre class="mermaid">' + html.escape("\n".join(kod)) + "</pre>"
            )
            i += 1
            continue

        # normal kod blogu
        if s.startswith("```"):
            i += 1
            kod = []
            while i < len(satirlar) and not satirlar[i].startswith("```"):
                kod.append(satirlar[i])
                i += 1
            parcalar.append("<pre><code>" + html.escape("\n".join(kod)) + "</code></pre>")
            i += 1
            continue

        # tablo
        if s.startswith("|") and i + 1 < len(satirlar) and re.match(r"^\|[\s:|-]+\|$", satirlar[i + 1]):
            basliklar = [h.strip() for h in s.strip("|").split("|")]
            i += 2
            govde = []
            while i < len(satirlar) and satirlar[i].startswith("|"):
                govde.append([c.strip() for c in satirlar[i].strip("|").split("|")])
                i += 1
            t = "<table><thead><tr>"
            t += "".join(f"<th>{satir_ici(h)}</th>" for h in basliklar)
            t += "</tr></thead><tbody>"
            for satir in govde:
                t += "<tr>" + "".join(f"<td>{satir_ici(c)}</td>" for c in satir) + "</tr>"
            t += "</tbody></table>"
            parcalar.append(t)
            continue

        # baslik
        m = re.match(r"^(#{1,4})\s+(.*)$", s)
        if m:
            seviye = len(m.group(1))
            parcalar.append(f"<h{seviye}>{satir_ici(m.group(2))}</h{seviye}>")
            i += 1
            continue

        # alinti
        if s.startswith("> "):
            blok = []
            while i < len(satirlar) and satirlar[i].startswith(">"):
                blok.append(satirlar[i].lstrip("> ").rstrip())
                i += 1
            parcalar.append("<blockquote>" + satir_ici(" ".join(blok)) + "</blockquote>")
            continue

        # liste
        if re.match(r"^[-*]\s+", s):
            ogeler = []
            while i < len(satirlar) and re.match(r"^[-*]\s+", satirlar[i]):
                ogeler.append("<li>" + satir_ici(re.sub(r"^[-*]\s+", "", satirlar[i])) + "</li>")
                i += 1
            parcalar.append("<ul>" + "".join(ogeler) + "</ul>")
            continue

        # yatay cizgi
        if s.strip() == "---":
            parcalar.append("<hr>")
            i += 1
            continue

        # paragraf
        if s.strip():
            blok = [s]
            i += 1
            while (i < len(satirlar) and satirlar[i].strip()
                   and not satirlar[i].startswith(("#", "|", "```", ">", "-", "*"))):
                blok.append(satirlar[i])
                i += 1
            parcalar.append("<p>" + satir_ici(" ".join(blok)) + "</p>")
            continue

        i += 1

    return "\n".join(parcalar)


def satir_ici(t):
    t = html.escape(t)
    t = re.sub(r"`([^`]+)`", r"<code>\1</code>", t)
    t = re.sub(r"\*\*([^*]+)\*\*", r"<strong>\1</strong>", t)
    t = re.sub(r"(?<!\*)\*([^*]+)\*(?!\*)", r"<em>\1</em>", t)
    t = t.replace("&lt;br/&gt;", "<br>")
    return t


govde = []
for yol in sorted(KLASOR.glob("*.md")):
    govde.append(f'<section id="{yol.stem}">')
    govde.append(md_to_html(yol.read_text()))
    govde.append("</section>")

CIKTI.write_text(f"""<!doctype html>
<html lang="tr"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>RobotLab — Analiz</title>
<style>
  body {{ font-family: system-ui, sans-serif; line-height: 1.65; max-width: 900px;
         margin: 0 auto; padding: 40px 24px 80px; color: #1a2430; background: #fff; }}
  h1 {{ font-size: 32px; border-bottom: 2px solid #1a2430; padding-bottom: 10px; margin-top: 0; }}
  h2 {{ font-size: 23px; margin-top: 40px; border-bottom: 1px solid #dde3ea; padding-bottom: 6px; }}
  h3 {{ font-size: 18px; margin-top: 28px; }}
  table {{ border-collapse: collapse; width: 100%; margin: 16px 0; font-size: 15px; }}
  th {{ background: #f0f3f7; text-align: left; padding: 9px 12px; border: 1px solid #dde3ea; }}
  td {{ padding: 9px 12px; border: 1px solid #dde3ea; vertical-align: top; }}
  code {{ background: #f0f3f7; padding: 2px 5px; border-radius: 3px;
          font-family: ui-monospace, Consolas, monospace; font-size: 0.88em; }}
  pre {{ background: #f6f8fa; border: 1px solid #dde3ea; border-radius: 5px;
         padding: 14px; overflow-x: auto; }}
  pre code {{ background: none; padding: 0; }}
  pre.mermaid {{ background: #fff; border: 1px solid #e6eaef; text-align: center; padding: 20px; }}
  blockquote {{ border-left: 4px solid #b04a15; margin: 16px 0; padding: 4px 0 4px 18px;
                color: #4a5a6a; }}
  section {{ margin-bottom: 60px; }}
  section + section {{ border-top: 3px double #c9d2db; padding-top: 40px; }}
  hr {{ border: none; border-top: 1px solid #dde3ea; margin: 32px 0; }}
</style></head><body>
{''.join(govde)}
<script src="https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.min.js"></script>
<script>mermaid.initialize({{ startOnLoad: true, theme: 'default' }});</script>
</body></html>""")

print(f"olusturuldu: {CIKTI}")
