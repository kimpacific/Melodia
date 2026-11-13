import math, random
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

W, H = 320, 180
import os
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Genres") + os.sep

def hexrgb(h): h = h.lstrip('#'); return tuple(int(h[i:i+2], 16) for i in (0, 2, 4))

def gradient(c1, c2, angle=35):
    a = math.radians(angle)
    y, x = np.mgrid[0:H, 0:W]
    t = (x * math.cos(a) + y * math.sin(a))
    t = (t - t.min()) / (t.max() - t.min())
    c1, c2 = np.array(hexrgb(c1)), np.array(hexrgb(c2))
    img = (c1[None, None, :] * (1 - t[..., None]) + c2[None, None, :] * t[..., None]).astype(np.uint8)
    return Image.fromarray(img, "RGB").convert("RGBA")

def layer(): return Image.new("RGBA", (W, H), (0, 0, 0, 0))
def comp(base, over): return Image.alpha_composite(base, over)
def rgba(h, a): r, g, b = hexrgb(h); return (r, g, b, a)

def glow(base, cx, cy, r, color, a=140):
    l = layer(); d = ImageDraw.Draw(l); d.ellipse((cx-r, cy-r, cx+r, cy+r), fill=rgba(color, a))
    return comp(base, l.filter(ImageFilter.GaussianBlur(r * 0.45)))

def p_all(img, rnd):
    for i, c in enumerate(["#FFB347", "#FF5E62", "#B721FF", "#21D4FD", "#FFE53B"]):
        img = glow(img, 40 + i * 62, 40 + (i % 2) * 90, 55, c, 120)
    return img
def p_electronic(img, rnd):
    l = layer(); d = ImageDraw.Draw(l); n = 26
    for i in range(n):
        h = int(25 + 105 * abs(math.sin(i * 0.55 + 1)) * (0.5 + rnd.random() * 0.5))
        x = 8 + i * 12
        d.rectangle((x, H - 8 - h, x + 7, H - 8), fill=rgba("#7DF9FF", 120))
        d.rectangle((x, H - 8 - h, x + 7, H - 8 - h + 3), fill=rgba("#FFFFFF", 200))
    return comp(img, l)
def p_hiphop(img, rnd):
    l = layer(); d = ImageDraw.Draw(l); cx, cy = 235, 95
    for r in range(95, 8, -9):
        d.ellipse((cx-r, cy-r, cx+r, cy+r), outline=rgba("#000000", 150), width=2)
    d.ellipse((cx-22, cy-22, cx+22, cy+22), fill=rgba("#FFB300", 230))
    d.ellipse((cx-4, cy-4, cx+4, cy+4), fill=rgba("#000000", 255))
    return comp(img, l)
def p_pop(img, rnd):
    l = layer(); d = ImageDraw.Draw(l)
    for _ in range(46):
        x, y, r = rnd.randint(0, W), rnd.randint(0, H), rnd.randint(4, 17)
        d.ellipse((x-r, y-r, x+r, y+r), fill=rgba(rnd.choice(["#FFFFFF", "#FFE066", "#7AF3FF", "#FF9CEE"]), rnd.randint(70, 170)))
    return comp(img, l)
def p_rock(img, rnd):
    l = layer(); d = ImageDraw.Draw(l)
    for i in range(-3, 12):
        x = i * 36
        d.polygon([(x, H), (x + 18, H), (x + 78, 0), (x + 60, 0)], fill=rgba("#000000", 85))
    d.polygon([(165, 0), (205, 0), (150, H), (110, H)], fill=rgba("#FF2D55", 110))
    return comp(img, l)
def p_alternative(img, rnd):
    for cx, cy, r, c in [(90, 80, 62, "#00F5D4"), (150, 100, 62, "#9B5DE5"), (210, 80, 62, "#F15BB5")]:
        l = layer(); d = ImageDraw.Draw(l); d.ellipse((cx-r, cy-r, cx+r, cy+r), fill=rgba(c, 120)); img = comp(img, l)
    return img
def p_rnb(img, rnd):
    img = glow(img, 70, 130, 90, "#FFB3D9", 130); img = glow(img, 250, 40, 80, "#FFD27F", 110)
    l = layer(); d = ImageDraw.Draw(l)
    for i in range(6):
        d.arc((-60 + i * 60, 50, 140 + i * 60, 250), 200, 340, fill=rgba("#FFFFFF", 60), width=2)
    return comp(img, l)
def p_lofi(img, rnd):
    l = layer(); d = ImageDraw.Draw(l)
    d.ellipse((190, 40, 270, 120), fill=rgba("#FFD6A5", 220))
    d.rectangle((0, 118, W, H), fill=rgba("#1B1B3A", 150))
    for y in range(0, H, 6): d.line((0, y, W, y), fill=rgba("#000000", 40), width=1)
    for i in range(0, 300, 28): d.rectangle((i, 130 + (i % 3) * 6, i + 20, H), fill=rgba("#0B0B22", 190))
    return comp(img, l)
def p_ambient(img, rnd):
    l = layer(); d = ImageDraw.Draw(l)
    for k in range(7):
        pts = [(x, 70 + k * 14 + 16 * math.sin(x / 38 + k * 0.8)) for x in range(0, W + 8, 8)]
        d.line(pts, fill=rgba("#FFFFFF", 40 + k * 14), width=3)
    return comp(img, glow(img, 250, 35, 50, "#9BF6FF", 120)) if False else comp(glow(img, 250, 35, 50, "#9BF6FF", 120), l)
def p_jazz(img, rnd):
    l = layer(); d = ImageDraw.Draw(l)
    for i in range(-4, 14):
        x = i * 30; d.polygon([(x, 0), (x + 10, 0), (x - 40, H), (x - 50, H)], fill=rgba("#FFD166", 55))
    d.ellipse((205, 28, 285, 108), outline=rgba("#FFD166", 200), width=4)
    d.ellipse((222, 45, 268, 91), outline=rgba("#FFD166", 120), width=2)
    return comp(img, l)
def p_classical(img, rnd):
    l = layer(); d = ImageDraw.Draw(l)
    for i in range(14):
        x = 12 + i * 23; d.line((x, 0, x, H), fill=rgba("#FFF3D6", 60 + (i % 3) * 25), width=1 + (i % 2))
    for y in (55, 70, 85, 100, 115): d.line((0, y, W, y), fill=rgba("#FFF3D6", 45), width=1)
    d.ellipse((210, 78, 240, 98), fill=rgba("#FFF3D6", 190)); d.line((238, 88, 238, 38), fill=rgba("#FFF3D6", 190), width=3)
    return comp(img, l)
def p_country(img, rnd):
    l = layer(); d = ImageDraw.Draw(l)
    d.ellipse((230, 18, 280, 68), fill=rgba("#FFF1B8", 230))
    for k, (c, a) in enumerate([("#8B5A2B", 150), ("#6B4423", 190), ("#4A2F18", 230)]):
        pts = [(x, 112 + k * 20 + 14 * math.sin(x / 55 + k * 1.7)) for x in range(0, W + 8, 8)] + [(W, H), (0, H)]
        d.polygon(pts, fill=rgba(c, a))
    return comp(img, l)
def p_folk(img, rnd):
    l = layer(); d = ImageDraw.Draw(l)
    for i, (x, s, a) in enumerate([(60, 110, 110), (150, 140, 150), (250, 120, 130), (-10, 90, 90), (320, 90, 90)]):
        d.polygon([(x - s, H), (x, H - s), (x + s, H)], fill=rgba("#06261C", a))
    for _ in range(26):
        x, y = rnd.randint(0, W), rnd.randint(0, 80); d.ellipse((x, y, x + 3, y + 3), fill=rgba("#FFFFFF", 150))
    return comp(img, l)
def p_metal(img, rnd):
    l = layer(); d = ImageDraw.Draw(l)
    for i in range(0, W + 30, 26): d.polygon([(i, H), (i + 13, H - 40 - (i * 7) % 60), (i + 26, H)], fill=rgba("#000000", 170))
    d.polygon([(70, 0), (92, 0), (60, 120), (44, 120)], fill=rgba("#E11D2A", 190))
    d.polygon([(210, 0), (225, 0), (195, 90), (183, 90)], fill=rgba("#E11D2A", 120))
    return comp(img, l)
def p_reggae(img, rnd):
    l = layer(); d = ImageDraw.Draw(l)
    for k, c in enumerate(["#009B3A", "#FED100", "#E8112D"]):
        for j in range(-2, 8):
            x = j * 80 + k * 26; d.polygon([(x, 0), (x + 26, 0), (x - 24, H), (x - 50, H)], fill=rgba(c, 120))
    d.ellipse((135, 55, 185, 105), fill=rgba("#FED100", 210))
    return comp(img, l)
def p_latin(img, rnd):
    l = layer(); d = ImageDraw.Draw(l); cx, cy = 250, 150
    for k in range(18):
        a = math.radians(k * 20)
        d.polygon([(cx, cy), (cx + 400 * math.cos(a - 0.06), cy + 400 * math.sin(a - 0.06)),
                   (cx + 400 * math.cos(a + 0.06), cy + 400 * math.sin(a + 0.06))], fill=rgba("#FFE29A", 70))
    d.ellipse((cx-34, cy-34, cx+34, cy+34), fill=rgba("#FFF3B0", 230))
    return comp(img, l)
def p_world(img, rnd):
    l = layer(); d = ImageDraw.Draw(l); cx, cy, r = 225, 90, 74
    d.ellipse((cx-r, cy-r, cx+r, cy+r), fill=rgba("#0B3D91", 120), outline=rgba("#FFFFFF", 170), width=2)
    for k in (-0.6, -0.3, 0, 0.3, 0.6):
        rr = abs(math.cos(math.asin(k))) * r; y = cy + k * r
        d.line((cx - rr, y, cx + rr, y), fill=rgba("#FFFFFF", 90), width=1)
    for k in (0.3, 0.6, 0.9):
        d.ellipse((cx - r * k, cy - r, cx + r * k, cy + r), outline=rgba("#FFFFFF", 90), width=1)
    d.line((cx, cy - r, cx, cy + r), fill=rgba("#FFFFFF", 90), width=1)
    return comp(img, l)
def p_trap(img, rnd):
    l = layer(); d = ImageDraw.Draw(l)
    for i in range(-2, 12):
        for j in range(-1, 6):
            x, y = i * 40 + (20 if j % 2 else 0), j * 34
            d.polygon([(x, y - 17), (x + 20, y), (x, y + 17), (x - 20, y)], outline=rgba("#C77DFF", 70), width=1)
    d.polygon([(235, 30), (285, 80), (235, 130), (185, 80)], fill=rgba("#9D4EDD", 150), outline=rgba("#E0AAFF", 230))
    d.polygon([(235, 30), (260, 80), (235, 130), (210, 80)], fill=rgba("#E0AAFF", 70))
    return comp(img, l)
def p_house(img, rnd):
    l = layer(); d = ImageDraw.Draw(l); x = 0
    while x < W:
        w, h = rnd.randint(24, 46), rnd.randint(50, 125)
        d.rectangle((x, H - h, x + w, H), fill=rgba("#12082B", 200))
        for wy in range(H - h + 8, H - 6, 12):
            for wx in range(x + 5, x + w - 5, 9):
                if rnd.random() > 0.4: d.rectangle((wx, wy, wx + 4, wy + 6), fill=rgba("#FFE66D", 200))
        x += w + 3
    return comp(img, l)
def p_default(img, rnd):
    for cx, cy, r in [(80, 60, 70), (220, 120, 90)]: img = glow(img, cx, cy, r, "#8A8A8A", 70)
    return img

SPEC = {
    "all": ("#E8440C", "#7A1FA2", 30, p_all), "electronic": ("#0F2BDC", "#8E2DE2", 40, p_electronic),
    "hiphop": ("#2B2B2B", "#C98A00", 30, p_hiphop), "pop": ("#FF4E9B", "#FFB347", 35, p_pop),
    "rock": ("#B3122C", "#1B0A2A", 40, p_rock), "alternative": ("#0F4C5C", "#5F0F8C", 35, p_alternative),
    "rnb": ("#6A11CB", "#E8118F", 30, p_rnb), "lofi": ("#2C3E78", "#E96D71", 80, p_lofi),
    "ambient": ("#1CB5E0", "#0A0A5C", 70, p_ambient), "jazz": ("#101A2E", "#9A7B1A", 35, p_jazz),
    "classical": ("#4E1A28", "#C9A96E", 35, p_classical), "country": ("#F2994A", "#F2C94C", 80, p_country),
    "folk": ("#134E5E", "#71B280", 70, p_folk), "metal": ("#16181A", "#5B6168", 35, p_metal),
    "reggae": ("#0B3D1E", "#8A6B00", 30, p_reggae), "latin": ("#E8321A", "#F5A31A", 30, p_latin),
    "world": ("#0575E6", "#12C27A", 35, p_world), "trap": ("#0F0C29", "#5A0DAD", 35, p_trap),
    "house": ("#FC466B", "#3F5EFB", 35, p_house), "default": ("#3B3B3B", "#0D0D0D", 35, p_default),
}
for name, (c1, c2, ang, fn) in SPEC.items():
    rnd = random.Random(hash(name) & 0xFFFF)
    img = fn(gradient(c1, c2, ang), rnd).convert("RGB")
    img.save(OUT + name + ".jpg", "JPEG", quality=86, optimize=True)
print("ok", len(SPEC))
