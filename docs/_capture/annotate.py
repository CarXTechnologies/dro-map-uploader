"""Turn raw DocsCapture shots into the published documentation images.

Usage:
    python docs/_capture/annotate.py <raw-dir> [--only id,id] [--debug]

<raw-dir> is the folder written by DocsCapture (Library/DocsCapture by default). For every shot in shots.json and every
language it lists, the script reads <raw-dir>/<lang>/<id>.png and .json, crops it, scales it, draws the numbered
callouts and a 1 px border, and writes docs/images/<en|ru|shared>/<id>.png. --debug also writes <id>.debug.png next to
the raw shot with every anchor rectangle drawn, which helps when a callout lands on the wrong element.

Rules (docs/CONTRIBUTING.md, screenshots): amber #FFB000 badges with a #1A1A1A ring and digit, 22 px at the displayed
width, placed 6 px left of the anchor or on its top-right corner when there is no room; a 1 px #6E7681 border; PNG,
palette-quantized; output at most 1.5x the displayed width (never upscaled).
"""
import json
import os
import sys

from PIL import Image, ImageDraw, ImageEnhance, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
DOCS = os.path.dirname(HERE)
IMAGES = os.path.join(DOCS, 'images')

BADGE_FILL = (0xFF, 0xB0, 0x00, 255)
BADGE_INK = (0x1A, 0x1A, 0x1A, 255)
BORDER = (0x6E, 0x76, 0x81, 255)
BADGE_DIAMETER = 22  # at the displayed width
BADGE_GAP = 6
MAX_SCALE_OVER_DISPLAY = 1.5
FONT_CANDIDATES = [r'C:\Windows\Fonts\segoeuib.ttf', r'C:\Windows\Fonts\arialbd.ttf', '/System/Library/Fonts/SFNS.ttf',
                   '/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf']


def load_font(size):
    for path in FONT_CANDIDATES:
        if os.path.exists(path):
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def text_of(spec, lang):
    value = spec
    if isinstance(spec, dict):
        value = spec.get(lang, spec.get('en'))
    return value


def match(element, anchor, lang):
    for key, expected in anchor.items():
        if key in ('edge', 'pad', 'index', 'px'):
            continue
        expected = text_of(expected, lang)
        if key == 'type' and element['type'] != expected:
            return False
        if key == 'class' and expected not in element['classes'].split():
            return False
        if key == 'name' and element['name'] != expected:
            return False
        if key == 'binding' and element['binding'] != expected:
            return False
        if key == 'text' and element['text'] != expected:
            return False
        if key == 'textStarts' and not element['text'].startswith(expected):
            return False
        if key == 'textContains' and expected not in element['text']:
            return False
    return True


def resolve(anchor, layout, lang):
    """Return (x, y, w, h) in raw pixels for an anchor spec."""
    if 'px' in anchor:
        px = text_of(anchor['px'], lang)
        return tuple(px) if len(px) == 4 else (px[0], px[1], 0, 0)
    found = [e for e in layout['elements'] if match(e, anchor, lang)]
    if not found:
        raise LookupError(f'no element for anchor {anchor} ({lang})')
    element = found[anchor.get('index', 0)]
    return tuple(element['rect'])


def edge_value(spec, layout, lang, ppp):
    if isinstance(spec, (int, float)):
        return spec * ppp
    x, y, w, h = resolve(spec['anchor'], layout, lang)
    edge = spec.get('edge', 'left')
    value = {'left': x, 'right': x + w, 'top': y, 'bottom': y + h}[edge]
    return value + spec.get('pad', 0) * ppp


def crop_box(shot, layout, lang):
    ppp = layout['ppp']
    crop = shot.get('crop', 'content')
    if crop == 'content':
        rx, ry, rw, rh = layout['rootBound']
        return (rx * ppp, ry * ppp, (rx + rw) * ppp, (ry + rh) * ppp)
    if crop == 'full':
        return (0, 0, layout['imageWidth'], layout['imageHeight'])
    return tuple(edge_value(crop[k], layout, lang, ppp) for k in ('left', 'top', 'right', 'bottom'))


def draw_badge(draw, cx, cy, number, scale):
    r = BADGE_DIAMETER * scale / 2
    ring = max(1, round(1.5 * scale))
    draw.ellipse((cx - r, cy - r, cx + r, cy + r), fill=BADGE_FILL, outline=BADGE_INK, width=ring)
    font = load_font(round(13 * scale))
    draw.text((cx, cy + 0.5 * scale), str(number), fill=BADGE_INK, font=font, anchor='mm')


def annotate(shot, raw_dir, lang, debug):
    src = os.path.join(raw_dir, lang if shot['folder'] == 'lang' else 'shared', shot['id'])
    layout = json.load(open(src + '.json', encoding='utf-8'))
    image = Image.open(src + '.png').convert('RGBA')
    ppp = layout['ppp']

    if debug:
        dbg = image.copy()
        d = ImageDraw.Draw(dbg)
        for e in layout['elements']:
            x, y, w, h = e['rect']
            d.rectangle((x, y, x + w, y + h), outline=(0, 255, 255, 255))
        dbg.save(src + '.debug.png')

    left, top, right, bottom = [round(v) for v in crop_box(shot, layout, lang)]
    left, top = max(0, left), max(0, top)
    right, bottom = min(image.width, right), min(image.height, bottom)
    cropped = image.crop((left, top, right, bottom))

    display = shot['width']
    out_width = min(cropped.width, round(display * shot.get('maxScale', MAX_SCALE_OVER_DISPLAY)))
    factor = out_width / cropped.width
    if factor < 1:
        cropped = cropped.resize((out_width, round(cropped.height * factor)), Image.LANCZOS)
    scale = cropped.width / display  # output pixels per displayed pixel

    if shot.get('brightness'):
        # Scene view shots of an unlit sample scene are very dark; brighten them before drawing the callouts.
        cropped = ImageEnhance.Brightness(cropped).enhance(shot['brightness'])

    draw = ImageDraw.Draw(cropped)
    r = BADGE_DIAMETER * scale / 2
    gap = BADGE_GAP * scale
    for callout in shot.get('callouts', []):
        anchor = callout['anchor']
        x, y, w, h = resolve(anchor, layout, lang)
        x, y, w, h = (x - left) * factor, (y - top) * factor, w * factor, h * factor
        place = callout.get('place', 'left')
        if place == 'left' and x - gap - 2 * r < 0 and not callout.get('force'):
            place = 'top-right'
        if place == 'left':
            cx, cy = x - gap - r, y + h / 2
        elif place == 'right':
            cx, cy = x + w + gap + r, y + h / 2
        elif place == 'top-right':
            cx, cy = x + w - r + 4 * scale, y + r - 4 * scale
        elif place == 'top-left':
            cx, cy = x + r - 4 * scale, y + r - 4 * scale
        elif place == 'above':
            cx, cy = x + w / 2, y - gap - r
        elif place == 'above-left':
            cx, cy = x + r, y - gap - r
        elif place == 'below':
            cx, cy = x + w / 2, y + h + gap + r
        elif place == 'below-left':
            cx, cy = x + r, y + h + gap + r
        elif place == 'center':
            cx, cy = x + w / 2, y + h / 2
        else:
            raise ValueError(place)
        cx += text_of(callout.get('dx', 0), lang) * scale
        cy += text_of(callout.get('dy', 0), lang) * scale
        cx = min(max(cx, r + 1), cropped.width - r - 1)
        cy = min(max(cy, r + 1), cropped.height - r - 1)
        draw_badge(draw, cx, cy, callout['n'], scale)

    border = max(1, round(scale))
    draw.rectangle((0, 0, cropped.width - 1, cropped.height - 1), outline=BORDER, width=border)

    out_dir = os.path.join(IMAGES, lang if shot['folder'] == 'lang' else 'shared')
    os.makedirs(out_dir, exist_ok=True)
    out = os.path.join(out_dir, shot['id'] + '.png')
    quantized = cropped.convert('RGB').quantize(colors=256, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    quantized.save(out, optimize=True)
    return out, cropped.size, os.path.getsize(out)


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(2)
    raw_dir = sys.argv[1]
    only = set()
    if '--only' in sys.argv:
        only = set(sys.argv[sys.argv.index('--only') + 1].split(','))
    debug = '--debug' in sys.argv
    shots = json.load(open(os.path.join(HERE, 'shots.json'), encoding='utf-8'))['shots']
    failed = False
    for shot in shots:
        if only and shot['id'] not in only:
            continue
        langs = ['en', 'ru'] if shot['folder'] == 'lang' else ['shared']
        for lang in langs:
            try:
                out, size, nbytes = annotate(shot, raw_dir, lang, debug)
                note = '  OVER 250 KB' if nbytes > 250 * 1024 else ''
                print(f'{os.path.relpath(out, DOCS)}  {size[0]}x{size[1]}  {nbytes // 1024} KB{note}')
            except Exception as error:  # report every shot, then fail
                failed = True
                print(f'FAILED {lang}/{shot["id"]}: {error}')
    sys.exit(1 if failed else 0)


if __name__ == '__main__':
    main()
