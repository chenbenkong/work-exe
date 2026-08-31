#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Generate WorkExe WPF assets from a boss photo.
Place a photo named boss.png in ../assets/ and run this script.
Outputs transparent PNGs to WorkExe/Assets/.
"""
import os
import sys
import math
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BOSS_PATH = os.path.join(ROOT, "assets", "boss.png")
OUT_DIR = os.path.join(ROOT, "WorkExe", "Assets")
W, H = 200, 250


def ensure_dir(d):
    os.makedirs(d, exist_ok=True)


def new_canvas(w=W, h=H):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))


def draw_rounded_rect(draw, xy, radius, fill, outline=None, width=1):
    x1, y1, x2, y2 = xy
    draw.rounded_rectangle(xy, radius=radius, fill=fill, outline=outline, width=width)


def load_face():
    if not os.path.exists(BOSS_PATH):
        return None
    img = Image.open(BOSS_PATH).convert("RGBA")
    # Crop to center square
    w, h = img.size
    s = min(w, h)
    left = (w - s) // 2
    top = (h - s) // 4  # bias to top for face
    face = img.crop((left, top, left + s, top + s))
    return face.resize((90, 90), Image.Resampling.LANCZOS)


def draw_placeholder_face(draw):
    draw.ellipse([55, 25, 145, 115], fill=(255, 220, 200, 255), outline=(200, 160, 140, 255), width=2)
    draw.ellipse([75, 55, 90, 70], fill=(60, 40, 30, 255))
    draw.ellipse([110, 55, 125, 70], fill=(60, 40, 30, 255))
    draw.arc([80, 75, 120, 95], start=0, end=180, fill=(180, 80, 80, 255), width=3)
    # Hair
    draw.arc([50, 15, 150, 85], start=180, end=360, fill=(50, 40, 30, 255), width=10)


def draw_body(draw, color=(80, 120, 180, 255)):
    # Torso
    draw.rounded_rectangle([60, 110, 140, 200], radius=20, fill=color)
    # Arms / legs
    draw.rounded_rectangle([40, 130, 60, 210], radius=8, fill=color)
    draw.rounded_rectangle([140, 130, 160, 210], radius=8, fill=color)
    draw.rounded_rectangle([70, 200, 95, 245], radius=6, fill=(50, 50, 50, 255))
    draw.rounded_rectangle([105, 200, 130, 245], radius=6, fill=(50, 50, 50, 255))


def draw_suit(draw, color=(60, 90, 150, 255)):
    draw_body(draw, color)
    # Tie
    draw.polygon([(100, 115), (90, 180), (100, 195), (110, 180)], fill=(200, 50, 50, 255))


def composite_face(img, face, y_offset=20, size=90):
    if face is None:
        d = ImageDraw.Draw(img)
        draw_placeholder_face(d)
    else:
        x = (img.width - size) // 2
        img.paste(face, (x, y_offset), face)


def draw_shadow(img):
    # Soft shadow under feet
    d = ImageDraw.Draw(img)
    d.ellipse([40, 235, 160, 250], fill=(0, 0, 0, 60))


def make_idle(face):
    img = new_canvas()
    draw_shadow(img)
    d = ImageDraw.Draw(img)
    draw_suit(d)
    composite_face(img, face)
    return img


def make_kowtow(face):
    img = new_canvas()
    d = ImageDraw.Draw(img)
    # Body bowed forward
    draw_suit(d, (60, 90, 150, 255))
    composite_face(img, face, y_offset=90, size=70)
    # Sweat drops
    d.polygon([(135, 80), (140, 95), (145, 80)], fill=(150, 200, 255, 200))
    return img


def make_crawl(face):
    img = new_canvas()
    d = ImageDraw.Draw(img)
    # Flat body
    d.rounded_rectangle([40, 160, 160, 210], radius=20, fill=(60, 90, 150, 255))
    d.rounded_rectangle([30, 175, 55, 215], radius=6, fill=(60, 90, 150, 255))
    d.rounded_rectangle([145, 175, 170, 215], radius=6, fill=(60, 90, 150, 255))
    composite_face(img, face, y_offset=120, size=60)
    return img


def make_hit(face):
    img = new_canvas()
    d = ImageDraw.Draw(img)
    draw_suit(d, (60, 90, 150, 255))
    # Head hug pose - arms up
    d.rounded_rectangle([35, 80, 60, 140], radius=8, fill=(60, 90, 150, 255))
    d.rounded_rectangle([140, 80, 165, 140], radius=8, fill=(60, 90, 150, 255))
    composite_face(img, face, y_offset=30, size=80)
    # Shake lines
    d.line([(30, 40), (20, 30)], fill=(255, 100, 100, 200), width=3)
    d.line([(170, 40), (180, 30)], fill=(255, 100, 100, 200), width=3)
    return img


def make_cannon_ready(face):
    img = new_canvas()
    d = ImageDraw.Draw(img)
    # Body hidden, only crying head top right
    composite_face(img, face, y_offset=20, size=110)
    # Tears
    d.polygon([(65, 80), (60, 110), (70, 80)], fill=(150, 200, 255, 220))
    d.polygon([(125, 80), (130, 110), (120, 80)], fill=(150, 200, 255, 220))
    return img


def make_cannon_fire(face):
    img = new_canvas()
    d = ImageDraw.Draw(img)
    composite_face(img, face, y_offset=30, size=90)
    # Motion blur lines
    d.line([(20, 50), (5, 40)], fill=(255, 200, 50, 180), width=4)
    d.line([(20, 100), (5, 110)], fill=(255, 200, 50, 180), width=4)
    return img


def make_cow_appear():
    img = new_canvas()
    d = ImageDraw.Draw(img)
    # Cow body
    d.rounded_rectangle([40, 120, 160, 200], radius=25, fill=(255, 255, 255, 255), outline=(80, 80, 80, 255), width=2)
    # Spots
    d.ellipse([60, 130, 90, 160], fill=(30, 30, 30, 255))
    d.ellipse([120, 160, 145, 190], fill=(30, 30, 30, 255))
    # Head
    d.ellipse([140, 100, 180, 150], fill=(255, 255, 255, 255), outline=(80, 80, 80, 255), width=2)
    d.ellipse([155, 120, 165, 130], fill=(30, 30, 30, 255))
    # Horns
    d.polygon([(150, 105), (145, 85), (160, 100)], fill=(200, 180, 120, 255))
    d.polygon([(170, 105), (180, 85), (165, 100)], fill=(200, 180, 120, 255))
    # Legs
    d.rounded_rectangle([50, 195, 70, 245], radius=5, fill=(255, 255, 255, 255), outline=(80, 80, 80, 255), width=2)
    d.rounded_rectangle([130, 195, 150, 245], radius=5, fill=(255, 255, 255, 255), outline=(80, 80, 80, 255), width=2)
    # Dust
    d.ellipse([20, 210, 50, 235], fill=(180, 160, 140, 120))
    d.ellipse([0, 215, 30, 240], fill=(180, 160, 140, 100))
    return img


def make_flying_out(face):
    img = new_canvas()
    d = ImageDraw.Draw(img)
    composite_face(img, face, y_offset=40, size=80)
    d.line([(30, 60), (5, 50)], fill=(255, 255, 255, 180), width=4)
    d.line([(30, 110), (5, 120)], fill=(255, 255, 255, 180), width=4)
    d.line([(30, 160), (10, 170)], fill=(255, 255, 255, 180), width=4)
    return img


def make_whip():
    img = Image.new("RGBA", (120, 120), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Whip handle
    d.rounded_rectangle([50, 60, 70, 110], radius=4, fill=(120, 80, 50, 255))
    # Whip lash curved
    d.arc([20, 10, 100, 70], start=200, end=340, fill=(80, 50, 30, 255), width=4)
    return img


def make_cannon():
    img = Image.new("RGBA", (180, 180), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    # Wheels
    d.ellipse([20, 130, 60, 170], fill=(60, 60, 60, 255), outline=(30, 30, 30, 255), width=3)
    d.ellipse([120, 130, 160, 170], fill=(60, 60, 60, 255), outline=(30, 30, 30, 255), width=3)
    # Base
    d.rounded_rectangle([30, 110, 150, 140], radius=4, fill=(80, 80, 80, 255))
    # Barrel
    d.rounded_rectangle([50, 20, 130, 120], radius=8, fill=(100, 100, 100, 255), outline=(60, 60, 60, 255), width=2)
    d.rounded_rectangle([45, 15, 135, 35], radius=6, fill=(120, 120, 120, 255), outline=(60, 60, 60, 255), width=2)
    return img


def make_app_ico():
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse([8, 8, 56, 56], fill=(255, 105, 180, 255))
    d.ellipse([20, 20, 44, 44], fill=(255, 255, 255, 255))
    d.ellipse([26, 28, 32, 34], fill=(50, 50, 50, 255))
    d.ellipse([36, 28, 42, 34], fill=(50, 50, 50, 255))
    d.arc([24, 34, 44, 46], start=0, end=180, fill=(180, 40, 80, 255), width=2)
    return img


def save(img, name):
    img.save(os.path.join(OUT_DIR, name), "PNG")


def main():
    ensure_dir(OUT_DIR)
    face = load_face()

    save(make_idle(face), "idle.png")
    save(make_idle(face), "drag.png")
    save(make_kowtow(face), "kowtow_0.png")
    save(make_kowtow(face), "kowtow_1.png")
    save(make_crawl(face), "crawl_0.png")
    save(make_crawl(face), "crawl_1.png")
    save(make_hit(face), "hit.png")
    save(make_cannon_ready(face), "cannon_ready.png")
    save(make_cannon_fire(face), "cannon_fire.png")
    save(make_cow_appear(), "cow_appear.png")
    save(make_cow_appear(), "cow_hit.png")
    save(make_flying_out(face), "flying_out.png")
    save(make_whip(), "whip.png")
    save(make_cannon(), "cannon.png")
    save(make_cow_appear(), "cow.png")

    # Also save single-frame fallback names for each state prefix
    save(make_idle(face), "idle_0.png")

    # App icon
    ico = make_app_ico()
    ico.save(os.path.join(OUT_DIR, "app.ico"), format="ICO", sizes=[(64, 64)])

    print(f"Assets generated in: {OUT_DIR}")


if __name__ == "__main__":
    main()
