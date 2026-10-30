#!/usr/bin/env python3
"""Check the MapUploader documentation.

Usage:
    python -I -X utf8 docs/_tools/check_docs.py [--check NAME,NAME] [--strict] [--release] [--verbose]
                                                [--src-window N]

Python 3.8 or later, standard library only. Run it from any folder; paths are resolved from the repository root.

The pages are README.md, README.ru.md, docs/en/** and docs/ru/**. The link and src checks also read
docs/CONTRIBUTING.md and docs/_capture/README.md.

Checks (--check selects some of them):
  links     Relative links and images point to existing files, and every #anchor exists on the target page.
  markdown  Patterns that GitHub renders differently: style widths, alerts in lists, legacy Image/ paths, unclosed
            anchors, a missing blank line before Result:/Next:, an HTML comment right after a block, two alerts in a
            row, leftover synced-with markers.
  parity    docs/en and docs/ru have the same pages; each pair of pages, and README.md / README.ru.md, has the same
            number of H2/H3 headings and images, the same <a name> anchors in the same order and the same src comments.
  images    Every <img> has the width from docs/_capture/shots.json, the callout table after it numbers its rows like
            the shot's callouts, the image is in the folder of the page language; no PNG is unreferenced or over
            250 KB, and every shot in shots.json has its PNG files.
  versions  README.md, README.ru.md and both getting-started.md pages name the project's Unity version, the
            getting-started pages name its revision; only the two READMEs carry an Applies to / Относится к line, and
            both lines agree with each other and with the project; the getting-started clone commands use the branch
            from that line; links into the CarX.Modding.Creator repository use the pinned submodule commit, not a
            branch such as main or another commit, and point to files that exist in it.
  src       Every <!-- src: path:line --> comment points to an existing file and to lines inside it. When the line
            with the comment has a code-span literal that looks like a message or a code identifier, the literal, or a
            distinctive part of it, must occur within --src-window lines (default 5) of the cited lines; otherwise
            the script prints a warning that names where the literal is now. A quoted message that occurs in none
            of the cited files is a warning too: the quote is stale, or its file isn't cited. A fact that comes from the game's
            source, which is closed, is marked with the bare reference `game` and is not checked. This repository
            is public, so a src comment never names a path in the game's source.

Exit code: 0 when there are no errors, 1 when there are errors, 2 for a usage error. Warnings don't change the exit code
unless you pass --strict. A synced-with marker is a warning, or an error with --release.
"""
import argparse
import collections
import json
import os
import re
import subprocess
import sys
from pathlib import Path
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[2]
DOCS = ROOT / 'docs'
SHOTS = DOCS / '_capture' / 'shots.json'
IMAGES = DOCS / 'images'
SUBMODULE = 'Assets/Plugins/CarX.Modding.Creator'
SUBMODULE_MARKER = ROOT / SUBMODULE / 'CarX.Modding.Creator.asmdef'
PROJECT_VERSION = ROOT / 'ProjectSettings' / 'ProjectVersion.txt'
GAME_REF = 'game'
MAX_IMAGE_BYTES = 250 * 1024
CHECKS = ('links', 'markdown', 'parity', 'images', 'versions', 'src')

FENCE = re.compile(r'^\s*(```|~~~)')
CODE_SPAN = re.compile(r'(`+)(.+?)\1')
COMMENT = re.compile(r'<!--.*?-->')
SRC_COMMENT = re.compile(r'<!--\s*src:(.*?)-->')
SRC_REF = re.compile(r'^([^:;<>|]+?):(\d+(?:-\d+)?(?:,\d+(?:-\d+)?)*)$')
LINK = re.compile(r'\]\(([^)\s]+)\)')
IMG = re.compile(r'<img\b[^>]*>', re.I)
ATTRIBUTE = re.compile(r'([\w-]+)="([^"]*)"')
ANCHOR = re.compile(r'<a name="([^"]+)"></a>')
HEADING = re.compile(r'^ {0,3}(#{1,6})\s+(.*?)\s*#*\s*$')
SDK_LINK = re.compile(r'https://github\.com/CarXTechnologies/CarX\.Modding\.Creator/(?:blob|tree)/([^/\s)"#?<>]+)/?([^)\s"#?<>]*)')
PLACEHOLDER = re.compile(r'<[^<>]*>|\{[^{}]*\}|…|\.\.\.')
WORD = re.compile(r'[A-Za-zА-Яа-яЁё]{2,}')


class Report:
    def __init__(self, strict, verbose):
        self.strict = strict
        self.verbose = verbose
        self.errors = 0
        self.warnings = 0
        self.annotate = os.environ.get('GITHUB_ACTIONS') == 'true'

    def error(self, path, line, message):
        self.errors += 1
        self._print('error', path, line, message)

    def warning(self, path, line, message):
        if self.strict:
            self.error(path, line, message)
            return
        self.warnings += 1
        self._print('warning', path, line, message)

    def note(self, path, line, message):
        if self.verbose:
            self._print('note', path, line, message)

    def _print(self, kind, path, line, message):
        if path is None:
            print(f'{kind}: {message}')
            return
        where = rel(path) + (f':{line}' if line else '')
        print(f'{where}: {kind}: {message}')
        if self.annotate and kind != 'note':
            text = message.replace('%', '%25').replace('\r', '%0D').replace('\n', '%0A')
            print(f'::{kind} file={rel(path)}' + (f',line={line}' if line else '') + f'::{text}')


def rel(path):
    try:
        return Path(path).resolve().relative_to(ROOT).as_posix()
    except ValueError:
        return str(path)


_text_cache = {}


def read_lines(path):
    path = Path(path)
    if path not in _text_cache:
        _text_cache[path] = path.read_text(encoding='utf-8', errors='replace').splitlines()
    return _text_cache[path]


def prose(path):
    """Yield (line number, line) for every line of a Markdown file outside fenced code blocks."""
    fenced = False
    for number, line in enumerate(read_lines(path), 1):
        if FENCE.match(line):
            fenced = not fenced
            continue
        if not fenced:
            yield number, line


def pages():
    result = [ROOT / 'README.md', ROOT / 'README.ru.md']
    for lang in ('en', 'ru'):
        result += sorted((DOCS / lang).rglob('*.md'))
    return [p for p in result if p.is_file()]


def guides():
    return [p for p in (DOCS / 'CONTRIBUTING.md', DOCS / '_capture' / 'README.md') if p.is_file()]


def page_lang(path):
    path = Path(path).resolve()
    if path == (ROOT / 'README.ru.md').resolve() or (DOCS / 'ru') in path.parents:
        return 'ru'
    return 'en'


# ---------- links ----------

def slug(heading):
    text = re.sub(r'<[^>]+>', '', heading)
    text = re.sub(r'!?\[([^\]]*)\]\([^)]*\)', r'\1', text)
    text = re.sub(r'[^\w\- ]', '', text.strip().lower())
    return text.replace(' ', '-')


_anchor_cache = {}


def anchors(path):
    path = Path(path).resolve()
    if path not in _anchor_cache:
        names = set()
        seen = collections.Counter()
        for _, line in prose(path):
            names.update(ANCHOR.findall(CODE_SPAN.sub('', line)))
            match = HEADING.match(line)
            if match:
                base = slug(match.group(2))
                names.add(base if seen[base] == 0 else f'{base}-{seen[base]}')
                seen[base] += 1
        _anchor_cache[path] = names
    return _anchor_cache[path]


def link_targets(line):
    text = CODE_SPAN.sub('', COMMENT.sub('', line))
    targets = LINK.findall(text)
    for tag in IMG.findall(text):
        src = dict(ATTRIBUTE.findall(tag)).get('src')
        if src:
            targets.append(src)
    return targets


def check_links(report):
    for page in pages() + guides():
        for number, line in prose(page):
            for link in link_targets(line):
                if re.match(r'[a-z][a-z0-9+.-]*:', link, re.I):
                    continue
                path, _, fragment = link.partition('#')
                target = (page.parent / unquote(path)).resolve() if path else page.resolve()
                if not target.exists():
                    report.error(page, number, f'link target does not exist: {link}')
                elif fragment and target.suffix == '.md' and fragment not in anchors(target):
                    report.error(page, number, f'no anchor #{fragment} in {rel(target)}: {link}')


# ---------- markdown ----------

def check_markdown(report, release):
    for page in pages():
        previous = ''
        last_text = ''
        fenced = False
        for number, line in enumerate(read_lines(page), 1):
            if FENCE.match(line):
                fenced = not fenced
                previous = line
                last_text = line
                continue
            if fenced:
                previous = line
                if line.strip():
                    last_text = line
                continue
            if re.search(r'<img[^>]*style=', line):
                report.error(page, number, 'GitHub strips the style attribute; use the width attribute')
            if re.match(r'\s+> \[!', line):
                report.error(page, number, 'alert inside a list item renders as literal text; put it after the list')
            if 'Image/' in line:
                report.error(page, number, 'legacy Image/ path; images live in docs/images/')
            if re.search(r'<a name="[^"]*">(?!</a>)', line):
                report.error(page, number, 'unclosed anchor; write <a name="x"></a>')
            if 'synced-with' in line:
                message = 'synced-with marker: finish the translation and remove it before a release'
                (report.error if release else report.warning)(page, number, message)
            if re.match(r'(Result|Next|Результат|Далее):', line) and previous.strip():
                report.error(page, number, f'no blank line before "{line.split(":")[0]}:"; it renders inside the block above')
            if line.startswith('<!--') and previous.strip():
                report.error(page, number, 'HTML comment right after a block; keep the comment at the end of the line it belongs to')
            if line.startswith('> [!') and last_text.startswith('>'):
                report.error(page, number, 'two alerts in a row')
            previous = line
            if line.strip():
                last_text = line


# ---------- parity ----------

def structure(path):
    headings, images, names, sources = 0, 0, [], collections.Counter()
    for _, line in prose(path):
        if re.match(r'#{2,3} ', line):
            headings += 1
        images += len(IMG.findall(line))
        names += ANCHOR.findall(CODE_SPAN.sub('', line))
        for body in SRC_COMMENT.findall(CODE_SPAN.sub('', line)):
            sources[' '.join(body.split())] += 1
    return headings, images, names, sources


def check_parity(report):
    en_pages = {p.relative_to(DOCS / 'en') for p in (DOCS / 'en').rglob('*.md')}
    ru_pages = {p.relative_to(DOCS / 'ru') for p in (DOCS / 'ru').rglob('*.md')}
    for missing in sorted(en_pages - ru_pages):
        report.error(DOCS / 'en' / missing, 0, f'no Russian page docs/ru/{missing.as_posix()}')
    for missing in sorted(ru_pages - en_pages):
        report.error(DOCS / 'ru' / missing, 0, f'no English page docs/en/{missing.as_posix()}')
    pairs = [(ROOT / 'README.md', ROOT / 'README.ru.md')]
    pairs += [(DOCS / 'en' / p, DOCS / 'ru' / p) for p in sorted(en_pages & ru_pages)]
    for en, ru in pairs:
        if not (en.is_file() and ru.is_file()):
            continue
        en_h, en_i, en_a, en_s = structure(en)
        ru_h, ru_i, ru_a, ru_s = structure(ru)
        if en_h != ru_h:
            report.error(ru, 0, f'{ru_h} H2/H3 headings, {rel(en)} has {en_h}')
        if en_i != ru_i:
            report.error(ru, 0, f'{ru_i} images, {rel(en)} has {en_i}')
        if en_a != ru_a:
            index = next((i for i, (a, b) in enumerate(zip(en_a, ru_a)) if a != b), min(len(en_a), len(ru_a)))
            en_name = en_a[index] if index < len(en_a) else 'none'
            ru_name = ru_a[index] if index < len(ru_a) else 'none'
            report.error(ru, 0, f'anchors differ from {rel(en)} at anchor {index + 1}: en "{en_name}", ru "{ru_name}"')
        for body in sorted((en_s - ru_s).elements()):
            report.error(ru, 0, f'src comment only in {rel(en)}: <!-- src:{" " + body} -->')
        for body in sorted((ru_s - en_s).elements()):
            report.error(ru, 0, f'src comment not in {rel(en)}: <!-- src:{" " + body} -->')


# ---------- images ----------

def load_shots(report):
    try:
        return {s['id']: s for s in json.loads(SHOTS.read_text(encoding='utf-8'))['shots']}
    except (OSError, ValueError, KeyError) as error:
        report.error(SHOTS, 0, f'cannot read shots.json: {error}')
        return None


def callout_numbers(lines, index):
    table = []
    for row in (l.strip() for l in lines[index + 1:]):
        if not row:
            if table:
                break
            continue
        if not row.startswith('|'):
            break
        table.append(row)
    return [int(n) for n in re.findall(r'^\|\s*(\d+)\s*\|', '\n'.join(table), re.M)]


def check_images(report):
    shots = load_shots(report)
    if shots is None:
        return
    referenced = set()
    for page in pages():
        lang = page_lang(page)
        lines = read_lines(page)
        for number, line in prose(page):
            for link in link_targets(line):
                if not re.match(r'[a-z][a-z0-9+.-]*:', link, re.I):
                    referenced.add((page.parent / unquote(link.partition('#')[0])).resolve())
            for tag in IMG.findall(CODE_SPAN.sub('', COMMENT.sub('', line))):
                attributes = dict(ATTRIBUTE.findall(tag))
                src = attributes.get('src', '')
                target = (page.parent / unquote(src)).resolve()
                if IMAGES.resolve() not in target.parents:
                    continue
                folder = target.parent.name
                shot = shots.get(target.stem)
                if shot is None:
                    report.error(page, number, f'{src} is not in docs/_capture/shots.json')
                    continue
                expected_folder = lang if shot.get('folder') == 'lang' else 'shared'
                if folder != expected_folder:
                    language = 'Russian' if lang == 'ru' else 'English'
                    report.error(page, number, f'{src}: a {language} page uses docs/images/{expected_folder}/{target.name}')
                width = attributes.get('width')
                if width is None:
                    report.error(page, number, f'{src} has no width attribute; shots.json says {shot["width"]}')
                elif not width.isdigit() or int(width) != shot['width']:
                    report.error(page, number, f'{src}: width {width}, shots.json says {shot["width"]}')
                numbers = callout_numbers(lines, number - 1)
                expected = [c['n'] for c in shot.get('callouts', [])]
                if numbers != expected:
                    report.error(page, number, f'{src}: callout table numbers {numbers}, shots.json callouts {expected}')
    for folder in ('shared', 'en', 'ru'):
        for png in sorted((IMAGES / folder).glob('*.png')):
            if png.resolve() not in referenced:
                report.error(png, 0, 'no page references this image; delete it or embed it')
            if png.stat().st_size > MAX_IMAGE_BYTES:
                report.error(png, 0, f'{png.stat().st_size // 1024} KB, the limit is {MAX_IMAGE_BYTES // 1024} KB')
    for shot_id, shot in shots.items():
        for folder in (('en', 'ru') if shot.get('folder') == 'lang' else ('shared',)):
            if not (IMAGES / folder / f'{shot_id}.png').is_file():
                report.error(SHOTS, 0, f'shot {shot_id} has no docs/images/{folder}/{shot_id}.png')


# ---------- versions ----------

def git(*args):
    try:
        result = subprocess.run(['git', '-C', str(ROOT)] + list(args), capture_output=True, text=True, timeout=30)
    except (OSError, subprocess.SubprocessError):
        return None
    return result.stdout if result.returncode == 0 else None


def check_versions(report):
    text = PROJECT_VERSION.read_text(encoding='utf-8') if PROJECT_VERSION.is_file() else ''
    version = re.search(r'^m_EditorVersion: (\S+)', text, re.M)
    revision = re.search(r'^m_EditorVersionWithRevision: .*\((\w+)\)', text, re.M)
    if not version or not revision:
        report.error(PROJECT_VERSION, 0, 'cannot read the Unity version and revision')
        return
    version, revision = version.group(1), revision.group(1)
    getting_started = [DOCS / 'en' / 'getting-started.md', DOCS / 'ru' / 'getting-started.md']
    for path in [ROOT / 'README.md', ROOT / 'README.ru.md'] + getting_started:
        if path.is_file() and f'Unity {version}' not in '\n'.join(read_lines(path)):
            report.error(path, 0, f'does not name the project\'s Unity version, "Unity {version}"')
    for path in getting_started:
        if path.is_file() and revision not in '\n'.join(read_lines(path)):
            report.error(path, 0, f'does not name the Unity revision {revision}')

    for page in pages():
        if page.resolve() in ((ROOT / 'README.md').resolve(), (ROOT / 'README.ru.md').resolve()):
            continue
        for number, line in prose(page):
            if re.match(r'(Applies to|Относится к):', line):
                report.error(page, number, 'only README.md and README.ru.md carry an Applies to / Относится к line')

    def applies_line(path, pattern):
        for number, line in prose(path):
            match = re.match(pattern, line)
            if match:
                return number, match
            if re.match(r'(Applies to|Относится к):', line):
                report.error(path, number, 'the Applies to line does not follow the template in docs/CONTRIBUTING.md')
                return number, None
        report.error(path, 0, 'no Applies to / Относится к line')
        return 0, None

    en_number, en = applies_line(ROOT / 'README.md',
                                 r'Applies to: MapUploader `([^`]+)` · Unity (\S+) · screenshots (\d{4})-(\d{2})-(\d{2})$')
    ru_number, ru = applies_line(ROOT / 'README.ru.md',
                                 r'Относится к: MapUploader `([^`]+)` · Unity (\S+) · скриншоты (\d{2})\.(\d{2})\.(\d{4})$')
    branch = None
    if en:
        branch = en.group(1)
        if en.group(2) != version:
            report.error(ROOT / 'README.md', en_number, f'Applies to names Unity {en.group(2)}, the project is on {version}')
    if ru:
        if ru.group(2) != version:
            report.error(ROOT / 'README.ru.md', ru_number, f'Относится к names Unity {ru.group(2)}, the project is on {version}')
    if en and ru:
        if ru.group(1) != en.group(1):
            report.error(ROOT / 'README.ru.md', ru_number, f'branch `{ru.group(1)}`, README.md says `{en.group(1)}`')
        if (ru.group(5), ru.group(4), ru.group(3)) != (en.group(3), en.group(4), en.group(5)):
            report.error(ROOT / 'README.ru.md', ru_number, 'the screenshot date differs from README.md')
    if branch:
        for path in getting_started:
            if not path.is_file():
                continue
            for number, line in enumerate(read_lines(path), 1):
                for pattern in (r'git clone\b.*?\s-b\s+([\w./-]+)', r'git switch\s+([\w./-]+)', r"Already on '([^']+)'"):
                    for match in re.finditer(pattern, line):
                        if match.group(1) != branch:
                            report.error(path, number, f'branch `{match.group(1)}`, README.md Applies to says `{branch}`')

    status = git('ls-files', '-s', '--', SUBMODULE)
    pinned = status.split()[1] if status and status.split() else None
    populated = SUBMODULE_MARKER.is_file()
    for page in pages():
        for number, line in prose(page):
            for ref, path in SDK_LINK.findall(line):
                # Ссылка на ветку (main) или на чужой коммит молча уезжает от закреплённого сабмодуля.
                if pinned and not (re.fullmatch(r'[0-9a-f]{7,40}', ref) and pinned.startswith(ref)):
                    report.error(page, number, f'SDK link points to {ref[:40]}, not to the submodule commit {pinned[:12]}; '
                                               'link to blob/<commit>/ or tree/<commit>/ with the pinned commit')
                if path and populated and not (ROOT / SUBMODULE / unquote(path)).exists():
                    report.error(page, number, f'SDK link points to {path}, which is not in {SUBMODULE}')
    if pinned is None:
        report.note(None, 0, 'git is unavailable; SDK link commits not checked')


# ---------- src ----------

def literal_kind(literal):
    """'strong' for a message of 3+ words or a qualified identifier, 'weak' for a short message or a plain
    camel-case identifier, None for values, paths and file names, which are not distinctive."""
    core = ' '.join(PLACEHOLDER.sub(' ', literal).split())
    if len(core) < 6:
        return None
    # A path with placeholders, such as <temp>/StandaloneTemporary<map id>/MapTemp, is a path, not a message.
    filled = PLACEHOLDER.sub('x', literal)
    if ('/' in filled or '\\' in filled) and not re.search(r'\s', filled):
        return None
    if ' ' in core:
        words = WORD.findall(core)
        if len(words) >= 3 and len(core) >= 12:
            return 'strong'
        return 'weak' if len(words) >= 2 else None
    if '/' in core or '\\' in core or re.fullmatch(r'[a-z0-9_.-]+', core) and '.' in core:
        return None
    if re.fullmatch(r'[A-Za-z_][\w.]*', core):
        if ('.' in core or '_' in core) and re.search(r'[A-Z]', core):
            return 'strong'
        if re.search(r'[a-z][A-Z]|[A-Z]{2}', core):
            return 'weak'
    return None


def normalize(text):
    text = text.replace('\\"', '"').replace("\\'", "'").replace('\\\\', '\\')
    return ' '.join(text.split()).lower()


def fragments(literal, segments=False):
    parts = [' '.join(p.split()).strip(' \'"`.,;:()[]') for p in PLACEHOLDER.split(literal)]
    result = [normalize(p) for p in parts if len(p) >= 6 and WORD.search(p)]
    # A part of three words or more is distinctive; a lone word such as 'Rigidbody' would match a stale quote too.
    long_parts = [p for p in result if len(WORD.findall(p)) >= 3]
    if long_parts:
        result = long_parts
    if segments and re.fullmatch(r'[\w.]+', literal):
        result += [s.lower() for s in literal.split('.') if len(s) >= 8 and re.search(r'[a-z][A-Z]', s)]
    return result


def parse_ref(text):
    match = SRC_REF.match(text.strip())
    if not match:
        return None
    ranges = []
    for item in match.group(2).split(','):
        first, _, last = item.partition('-')
        ranges.append((int(first), int(last or first)))
    return match.group(1), ranges


def window_text(lines, ranges, size):
    chunks = []
    for first, last in ranges:
        chunk = ' '.join(lines[max(0, first - 1 - size):min(len(lines), last + size)])
        chunks.append(normalize(re.sub(r'"\s*\+\s*\$?@?"', '', chunk)))
    return ' '.join(chunks)


def check_src(report, size):
    populated = SUBMODULE_MARKER.is_file()
    if not populated:
        report.error(ROOT / SUBMODULE, 0, 'the submodule is empty, so src comments that point into it are not checked; '
                                          'run git submodule update --init')
    counts = collections.Counter()
    for page in pages() + guides():
        for number, line in prose(page):
            plain = CODE_SPAN.sub('', line)
            bodies = SRC_COMMENT.findall(plain)
            if not bodies:
                continue
            literals = [(m.group(2), literal_kind(m.group(2))) for m in CODE_SPAN.finditer(COMMENT.sub('', line))]
            literals = [(text, kind) for text, kind in literals if kind]
            for body in bodies:
                checked = []
                for ref_text in body.split(';'):
                    counts['refs'] += 1
                    if ref_text.strip() == GAME_REF:
                        counts['game'] += 1
                        continue
                    ref = parse_ref(ref_text)
                    if ref is None:
                        report.error(page, number, f'malformed src reference "{ref_text.strip()}"; write path:line, path:first-last, path:a,b, '
                                                   f'or `{GAME_REF}` for a fact from the game\'s closed source')
                        continue
                    path, ranges = ref
                    if path.startswith(SUBMODULE + '/') and not populated:
                        continue
                    target = ROOT / path
                    shown = path
                    if not target.is_file():
                        report.error(page, number, f'src file does not exist: {shown}')
                        continue
                    lines = read_lines(target)
                    bad = [(a, b) for a, b in ranges if a < 1 or b < a or b > len(lines)]
                    if bad:
                        spans = ', '.join(f'{a}-{b}' if a != b else str(a) for a, b in bad)
                        report.error(page, number, f'src lines {spans} are outside {shown}, which has {len(lines)} lines')
                        continue
                    counts['checked'] += 1
                    checked.append((target, shown, ranges, lines))
                if checked and literals:
                    check_literals(report, page, number, literals, checked, size)
    report.note(None, 0, f'src: {counts["refs"]} references, {counts["checked"]} checked, '
                         f'{counts["game"]} `{GAME_REF}` not checked')


_word_cache = {}


def file_words(target):
    """The words of a file as one lowercase string, with string concatenations and interpolation holes removed, so that
    a message split over several lines or built with {…} still reads as one run of words."""
    if target not in _word_cache:
        text = '\n'.join(read_lines(target))
        text = re.sub(r'"\s*\+\s*\$?@?"', '', text)
        text = re.sub(r'\{[^{}"\n]*\}', ' ', text)
        _word_cache[target] = ' ' + ' '.join(re.findall(r'\w+', text.lower())) + ' '
    return _word_cache[target]


def word_runs(literal):
    """Runs of three consecutive words from each part of the literal between placeholders; a part of one or two
    words of at least 6 characters is a run of its own."""
    runs, short = [], []
    for part in PLACEHOLDER.split(literal):
        words = re.findall(r'\w+', part.lower())
        if len(words) >= 3:
            runs += [' '.join(words[i:i + 3]) for i in range(len(words) - 2)]
        elif words and len(' '.join(words)) >= 6:
            short.append(' '.join(words))
    # Short parts count only when the literal has no part of three words.
    return runs or short


_piece_cache = {}
CS_STRING = re.compile(r'\$?@?"((?:[^"\\\n]|\\.)*)"')


def string_pieces(target):
    """The constant pieces of the C# string literals in a file, split at {…} holes, that have two words or more and
    8 characters or more. A message that the code builds from a piece and a value, such as "Local map: " + name, is
    quoted in the docs with the value filled in, and such a quote contains the piece."""
    if target not in _piece_cache:
        pieces = set()
        if Path(target).suffix == '.cs':
            for line in read_lines(target):
                for body in CS_STRING.findall(line):
                    for piece in re.split(r'\{[^{}]*\}', body):
                        piece = normalize(piece).strip(' \'"`.,;:()[]')
                        if len(piece) >= 8 and len(re.findall(r'\w+', piece)) >= 2:
                            pieces.add(piece)
        _piece_cache[target] = pieces
    return _piece_cache[target]


def is_message(literal):
    """True for a literal of words separated by spaces, false for an identifier, a path or a sample value. Only a
    message is reported when no cited file has it: an identifier such as Application.temporaryCachePath often names
    an API that no file of this repository defines."""
    return ' ' in ' '.join(PLACEHOLDER.sub(' ', literal).split())


def occurs_in_file(literal, target):
    words = file_words(target)
    if any(f' {run} ' in words for run in word_runs(literal)):
        return True
    text = normalize(literal)
    return any(piece in text for piece in string_pieces(target))


def check_literals(report, page, number, literals, checked, size):
    for target, shown, ranges, lines in checked:
        window = window_text(lines, ranges, size)
        for literal, _ in literals:
            if any(f in window for f in fragments(literal, segments=True)):
                return
    moved = []
    missing = []
    for literal, kind in literals:
        if kind != 'strong':
            continue
        wanted = fragments(literal)
        found_in_any = False
        for target, shown, ranges, lines in checked:
            found = [i for i, text in enumerate(lines, 1) if any(f in normalize(text) for f in wanted)]
            if found:
                found_in_any = True
                moved.append(f'`{literal}` is at {shown}:{",".join(map(str, found[:3]))}')
        if not found_in_any and is_message(literal) and word_runs(literal) \
                and not any(occurs_in_file(literal, target) for target, _, _, _ in checked):
            missing.append(f'`{literal}`')
    cited = '; '.join(shown + ':' + ','.join(f'{a}-{b}' if a != b else str(a) for a, b in ranges)
                      for _, shown, ranges, _ in checked)
    if moved:
        report.warning(page, number, f'no code-span literal of this line is within {size} lines of {cited}, but '
                       + '; '.join(moved[:2]) + '. If the code moved, fix the line numbers; if the src lines cite '
                       'another fact of this line, add the line of the literal to the comment.')
    elif missing:
        # Сильный литерал-сообщение (3+ слова) не найден ни в одном цитируемом файле:
        # либо цитата устарела, либо комментарий указывает не на тот файл.
        report.warning(page, number, f'no cited file has {" or ".join(missing[:2])}: {cited}. If the code changed the text, '
                       'quote the current text; if the literal comes from another file, cite that file too.')
    else:
        report.note(page, number, f'no code-span literal of this line occurs in {cited}')


# ---------- main ----------

def main():
    parser = argparse.ArgumentParser(description='Check the MapUploader documentation.')
    parser.add_argument('--check', default=','.join(CHECKS), help='comma-separated checks: ' + ', '.join(CHECKS))
    parser.add_argument('--strict', action='store_true', help='treat warnings as errors')
    parser.add_argument('--release', action='store_true', help='treat synced-with markers as errors')
    parser.add_argument('--verbose', action='store_true', help='also print notes')
    parser.add_argument('--src-window', type=int, default=5, metavar='N',
                        help='lines around the cited lines in which a code-span literal must occur (default 5)')
    args = parser.parse_args()
    selected = [c.strip() for c in args.check.split(',') if c.strip()]
    unknown = [c for c in selected if c not in CHECKS]
    if unknown:
        parser.error('unknown check: ' + ', '.join(unknown))

    report = Report(args.strict, args.verbose)
    if 'links' in selected:
        check_links(report)
    if 'markdown' in selected:
        check_markdown(report, args.release)
    if 'parity' in selected:
        check_parity(report)
    if 'images' in selected:
        check_images(report)
    if 'versions' in selected:
        check_versions(report)
    if 'src' in selected:
        check_src(report, args.src_window)
    print(f'{report.errors} error(s), {report.warnings} warning(s)')
    return 1 if report.errors else 0


if __name__ == '__main__':
    sys.exit(main())
