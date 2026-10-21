# Fabrique le logo du mod (modio/logo.png), dans le style des autres mods.
# Usage : python modio/fabriquer-visuels.py   (Pillow, polices Segoe UI de Windows)
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

L, H = 1280, 720
POLICES = Path("C:/Windows/Fonts")
ROUGE, BLANC, BLEU = (255, 90, 90), (255, 255, 255), (90, 180, 255)
JAUNE = (242, 196, 26)


def police(nom, taille):
    return ImageFont.truetype(str(POLICES / nom), taille)


img = Image.new("RGB", (L, H))
d = ImageDraw.Draw(img)
for y in range(H):  # degrade bleu nuit, comme Location
    t = y / H
    d.line([(0, y), (L, y)], fill=(int(16 + 6 * t), int(44 + 46 * t), int(68 + 34 * t)))

d.text((96, 70), "Compteur", font=police("segoeuib.ttf", 120), fill=BLANC)
d.text((98, 228), "Compteur de vitesse des véhicules", font=police("segoeui.ttf", 40), fill=(214, 236, 244))
d.text((98, 280), "Vehicle speedometer · Eco", font=police("segoeuil.ttf", 40), fill=(150, 205, 222))

# Les trois etats de la jauge, comme a l'ecran.
mono = police("consolab.ttf", 34)
texte = police("segoeuisl.ttf", 34)
for i, (couleur, pleins, kmh, bonus, sol) in enumerate([
    (ROUGE, 6, 19, "x0.5", "végétation · plants"),
    (BLANC, 11, 43, "x1.0", "terre · dirt"),
    (BLEU, 17, 72, "x1.2", "asphalte · asphalt"),
]):
    y = 400 + i * 86
    d.rounded_rectangle([90, y - 12, 760, y + 58], radius=14, fill=(10, 30, 46))
    d.text((112, y), "|" * pleins, font=mono, fill=couleur)
    d.text((112 + 19 * pleins, y), "|" * (20 - pleins), font=mono, fill=(70, 90, 104))
    d.text((520, y - 2), f"{kmh} km/h", font=police("segoeuib.ttf", 34), fill=BLANC)
    d.text((675, y - 2), bonus, font=police("segoeuib.ttf", 34), fill=couleur)
    d.text((790, y - 2), sol, font=texte, fill=(180, 215, 228))

# Cadran : arcs rouge / blanc / bleu et aiguille.
cx, cy, r = 1100, 190, 120
d.ellipse([cx - r - 18, cy - r - 18, cx + r + 18, cy + r + 18], fill=(10, 30, 46))
for debut, fin, couleur in [(150, 210, ROUGE), (210, 300, BLANC), (300, 390, BLEU)]:
    d.arc([cx - r, cy - r, cx + r, cy + r], debut, fin, fill=couleur, width=22)
for k in range(13):  # graduations
    a = math.radians(150 + k * 20)
    d.line([(cx + (r - 30) * math.cos(a), cy + (r - 30) * math.sin(a)),
            (cx + (r - 46) * math.cos(a), cy + (r - 46) * math.sin(a))], fill=(150, 205, 222), width=4)
a = math.radians(338)
d.line([(cx, cy), (cx + (r - 34) * math.cos(a), cy + (r - 34) * math.sin(a))], fill=JAUNE, width=10)
d.ellipse([cx - 18, cy - 18, cx + 18, cy + 18], fill=JAUNE)
d.text((cx, cy + 58), "km/h", font=police("segoeuib.ttf", 36), fill=BLANC, anchor="mm")

sortie = Path(__file__).with_name("logo.png")
img.save(sortie)
print(sortie)
