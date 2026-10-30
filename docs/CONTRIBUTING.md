# Contributing to the MapUploader documentation

This guide is for maintainers who change the MapUploader documentation or the MapBuilder UI it describes. It sets the writing rules for both languages and lists what a docs pull request must contain. To retake screenshots, follow the [capture guide](_capture/README.md).

This guide is in English only.

## Quick path

- **Fix a paragraph.** Change the English and the Russian page in the same pull request. Keep the `<!-- src -->` comment at the end of its line, and fix its line numbers if you moved the fact or the code moved.
- **Retake one screenshot.** Follow the [capture guide](_capture/README.md): run the capture with `-docsCaptureOnly <id>`, run `annotate.py --only <id>`, and commit the new images.
- **Add a page.** Follow [Add, move or rename a page](#add-move-or-rename-a-page).
- **A UI text changed.** Copy the new label from its `L("<ru>", "<en>")` call into both languages, as in [UI labels and literals](#ui-labels-and-literals), and retake the shots that [When to re-capture](_capture/README.md#when-to-re-capture) lists for the file you changed.

For every task, run `python -I -X utf8 docs/_tools/check_docs.py` (see [Check the docs](#lint)) and copy the [pull request checklist](#checklist) into the pull request.

## What lives where

```text
README.md                 English landing page
README.ru.md              Russian landing page
docs/
  CONTRIBUTING.md         this guide: writing rules and the pull request checklist
  en/                     English pages
  ru/                     Russian pages: the same files under the same names
  images/
    shared/               UI that is English in both languages
    en/                   MapBuilder screenshots in English
    ru/                   MapBuilder screenshots in Russian, same file names as en/
  _tools/
    check_docs.py         the docs checks that CI runs
  _capture/               screenshot tooling, never referenced from the pages
    README.md             the capture guide: how to retake the screenshots
    requirements.txt      the Python package that annotate.py needs
    shots.json            crop, display width and callouts of every shot
    annotate.py           turns raw captures into docs/images/**
    Editor/               capture script, copied into Assets/ only while capturing
```

Each page has one type. Don't mix types on a page.

| Type | Pages | What it holds |
| --- | --- | --- |
| Tutorial | The *Quick start* section of `README.md` and `README.ru.md` | The shortest path from the clone to a published map, with a link to the task page for each step |
| How-to | `getting-started`, `scene-setup`, `markers`, `minimap-and-images`, `map-settings`, `build`, `validation`, `test-in-game`, `publish` | Procedures, plus the tables the reader needs while doing them |
| Reference | `reference`, `troubleshooting` | Limits, settings, messages with their fixes, and the glossary |
| Explanation for integrators | `advanced/mod-format`, `advanced/publishing-vendors` | File-format internals and vendor SDKs |

The pages are written for map authors who know basic Unity. Keep integrator material, such as vendor SDKs and file-format internals, in `docs/en/advanced/` and `docs/ru/advanced/`.

Every page in `docs/en/` and `docs/ru/` ends with a *Next steps* section. Its last line links to the previous page, the contents page and the next page, in the order of the contents page `docs/en/README.md`.

## Add, move or rename a page

To add a page:

1. Create `docs/en/<name>.md` and `docs/ru/<name>.md` under the same lowercase kebab-case file name.
1. Add the page to the contents pages `docs/en/README.md` and `docs/ru/README.md`, and to the *Documentation* table in `README.md` and `README.ru.md`.
1. Update the *Next steps* line of the pages that come before and after it.

Result: `check_docs.py` reports no missing page or link.

When you move or rename a page, update every link to it in the same pull request, in both languages, and delete the old file. Don't leave redirect stubs.

## Principles

- **One page, one job.** Each page has one type. Put a variant of a procedure under its own heading, and never write "alternatively…" inside a procedure.
- **The text carries the instructions.** A reader who never loads an image can still finish the task. Images only locate hard-to-find UI or show a result.
- **Every fact traces to code.** Check each limit, default, label, message and path in the code before you write it. When the docs and the code disagree, the code wins. See [Source comments](#src).
- **Describe the current behaviour neutrally.** When the tool behaves in a surprising way, state the behaviour as a fact and give the workaround, for example "After you edit a template, select each marker that uses it." Never call it a bug in the docs; report it in the repository's issues instead.
- **Update the docs in the same PR as the UI.** Text and screenshots change in the pull request that changes the UI they describe. Fix or delete stale text; never excuse it. Sentences such as "This screenshot predates…", "shows an older marker menu" or "shows the older context-menu command" are banned.
- **Game facts need the game's code.** Verify what the game does in the game's source before you write it. Don't promise platforms or load limits that the game team hasn't confirmed; name the copy of the game as described in [Voice, terms and headings](#voice-terms-and-headings), and say that very large maps may fail to load.

## Voice, terms and headings

- **Voice.** Write in the second person, imperative mood and present tense, in short sentences. Don't use *please*, *simply*, *just*, *easily* or *obviously*, and don't use *navigate* or *toggle* as verbs.
- **One term per thing.**
  - *map*, never track, level or prototype;
  - *publication* for a mod.io entry, because the UI says `Publications`;
  - *build*, *export* and *validation* for the three operations;
  - *build cache* for the staging folder `StandaloneTemporary<id>`.
  - The [glossary](en/reference.md#glossary) is the authority for terms in both languages.
- **The copy of the game.** Not every copy of the game loads maps in the same way, so name the copy by what it does:
  - for local maps, "a copy of the game that loads local maps", RU «копия игры, которая загружает локальные карты», with a link to [Check that the game loads local maps](en/test-in-game.md#check-game), RU [Проверка загрузки локальных карт в игре](ru/test-in-game.md#check-game);
  - for mod.io publications, "game builds that support mod.io", RU «сборки игры с поддержкой mod.io», for example "maps from mod.io download in game builds that support mod.io".
- **Numbers.** Write numerals with units: `8 MB`, `50 characters`, `64 m`, `512 × 288`.
- **Headings.** Use sentence case and no emoji. English task headings are verb + object, such as "Export the map to the game folder". Russian headings use an action noun, such as «Экспорт карты в папку игры», and stay parallel within a page.
- **No directions.** Never write left, right, above or below. Unity windows dock anywhere, and the **Publish** tab stacks its two columns when MapBuilder is narrower than 1120 points. Name the window, tab, section or field instead. <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:64 -->

## Page anatomy

### Task pages

Every element is its own paragraph. GitHub joins consecutive lines into one paragraph, so keep the blank lines.

```markdown
# <Verb + object>

**English** · [Русский](../ru/<same-file>.md)

<1–2 sentences: what the reader gets and when to do it.>

## Before you start

- <Prerequisite noun phrase, linking to where it is done.>

## <Task heading>

To <goal>:

1. <Location>, <action>.
   <Result sentence, if any.>
1. <Next step.>

Result: <what the reader sees, and how to check it>.

## Next steps

← [<previous page>](x.md) · [Contents](README.md) · [<next page>](y.md) →
```

The Russian page uses the same structure with its own switcher:

```markdown
[English](../en/<same-file>.md) · **Русский**
```

### The Applies to line

Only `README.md` and `README.ru.md` say which version the docs describe, in their own paragraph after the language switcher. The pages in `docs/en/` and `docs/ru/` have no such line.

```markdown
Applies to: MapUploader `<branch>` · Unity <version> · screenshots <YYYY-MM-DD>
```

```markdown
Относится к: MapUploader `<branch>` · Unity <version> · скриншоты <DD.MM.YYYY>
```

The branch is the branch the docs describe, the version is the project's Unity version, and the date is the day of the latest screenshot capture. Both lines name the same branch, version and date; `check_docs.py` compares them with each other and with `ProjectSettings/ProjectVersion.txt`.

### Tables

- Use one table per UI panel, with the rows in the same order as the UI.
- Settings tables use the columns `| Option | Default | What it does | When to change |`.
- The callout table goes directly after its image and uses `| # | Element | What it does |`. For a form, use `| # | Field | What to enter | Limit |`, with an extra column such as `Where it appears` when the reader needs it. The `#` column holds the callout number.
- Leave a blank line before a table, and escape `|` inside a cell as `\|`.

### Troubleshooting entries

Entries appear only in `troubleshooting.md`, and the page opens with the triage rule "Run **Validate map**, fix the errors from the top, then build again." Keep every line of an entry its own paragraph.

- **Anchor.** Every entry starts with its anchor: `<a name="msg-<short name>"></a>` for a message, such as `msg-road-no-collider`, or a plain slug for a symptom, such as `road-grip` for "A road surface has the wrong grip".
- **Heading.** A message entry is headed with the message in a code span, shortened with … to 70 characters or fewer. A symptom entry is headed with the symptom in plain words.
- **Full message.** When the heading shortens the message, the first line of the entry is `Message:`, RU `Сообщение:`, followed by the full text. So every message entry starts with the full message.
- **Placeholders.** Write a value from the reader's map in the notation of `validation.md`: `<object>`, `<type>`, `<material>`, `<variant>`, `<size>`, `<extension>`, `<texture>`, `<width>x<height>`, `<property>`.
- **Sections.** *Validation messages* has an H3 heading for each category, in the order of the **Map Validation** window, with an H4 entry for each message under it. In the other sections, entries are H3 headings.
- **Export errors.** *Build errors in the Console* starts with the table of export errors under the anchor `export-errors`, followed by the table of export warnings, both with the columns `| Message | Cause | Fix |`. When an exporter gets a new `throw` or `Debug.LogWarning`, add a row for its message to the matching table.

```markdown
<a name="msg-road-no-collider"></a>
#### `<exact message, shortened with … to ≤ 70 characters>`

Message: `<the full message, when the heading is shortened>`

Severity: Error (blocks the build) · Category: Markers

**Cause.** This happens when …

**Fix.**

1. …

See also: [Mark road surfaces](markers.md#road)
```

A message that the Console shows instead of the **Map Validation** window has `Severity: Error (stops the build) · Shown in: Console`.

### Markdown that GitHub renders differently

- A line at column 0 right after a list item continues that item. Leave a blank line before `Result:` or `Next:`, or it renders inside the last step.
- An HTML comment at column 0 right after a list item starts an HTML block and ends the list, so with `1.` numbering the next step restarts at 1. Keep a comment at the end of the line it belongs to.
- An unclosed `<a name="x">` alone on a line starts a raw-HTML block that swallows the lines that follow it up to the next blank line, so the heading after it renders as plain `## …` text. Always write `<a name="x"></a>`.
- An alert inside a list item or inside `<details>` renders as a literal `[!WARNING]`.
- GitHub strips the `style` attribute, so `style="width:400px"` shows the image at full size. Use the `width` attribute.
- A nested ordered list renders as i., ii., and Markdown has no letter markers: `a.` and `b.` render as plain text.
- Without a blank line after `</summary>`, the content of `<details>` doesn't render as Markdown.

`check_docs.py` finds most of these patterns; see [Check the docs](#lint).

## Procedures

- Introduce each procedure with "To <goal>:" (RU «Чтобы <цель>:»).
- Write one action per step. You can combine two short actions in the same place, for example "In **Folder**, enter the path". Every step contains an action.
- Order each step as: `Optional:` (RU `Необязательно:`), the reason, the location, the action. The result is the next sentence in the same step, never a step of its own.
- Put the location first: "In the **MapBuilder** window, on the **Build** tab, select the **Build map** button."
- Write `1.` for every ordered item. A procedure with a single step is one bullet.
- Write sub-steps as a nested `1.` list indented 3 spaces, and never refer to a sub-step by letter. Prefer splitting a long procedure over nesting.
- Include the confirming action, such as **Accept**, **Continue** or **Delete**.
- Put prerequisites under *Before you start*, never in a note or between the steps.
- End with a `Result:` paragraph that describes the visible state, for example "the library row changes to `Ready to export`".

## UI labels and literals

| Thing | Format | Example |
| --- | --- | --- |
| Interactive label: button, tab, field, checkbox, dropdown, menu item | **Bold**, with the exact text and case from the code. Drop a trailing `:` or `…` and collapse double spaces. | **Build map**, **Delete publication**, **+ New map** |
| Menu path | Each item bold, separated by a plain spaced `>`. The first time a page uses a menu, say where it is: "In the Unity menu bar, select …". | **Tools** > **MapBuilder** |
| Window or panel name | Bold, as titled | the **Map Validation** window, the **Inspector** |
| Non-interactive UI text, status lines, tool and validation messages | Code span, verbatim | `Ready to export`, `mod.io: not linked` |
| Keys | `<kbd>` | <kbd>Ctrl</kbd>+<kbd>Shift</kbd>+<kbd>N</kbd> |
| Paths, file names, extensions, tags, layers, EditorPrefs keys, typed values | Code span | `Assets/MapResources/<MapName>/`, `mod.cxmod`, `Garbage`, `1.0.0` |
| Placeholders | `<angle brackets>` inside code, explained once | `<game folder>/Mods/<MapName>` |
| Unity component names in prose | Plain text, exact case. Use a code span only when the reader types the name. | add the GameMarkerData component; type `GameMarkerData` |

**Copy labels from the code.** Never type a label from memory.

- Copy MapBuilder labels from the `L("<ru>", "<en>")` calls in `Assets/Editor/MapBuilderEditorWindow*.cs` and `Assets/Editor/BuildOptimizationPanel.cs`.
- Copy inspector labels from a running editor, because Unity reformats field names for display.

**Duplicate labels.** Some labels name two different controls. The first time a procedure uses one of them, name the control type and where it is:

- **Export to folder** is an option under **Build destination** and a button. RU: the option «Экспорт в папку», the button «Экспортировать в папку».
- **Platform** is a field in the header and an option under **Build destination**. RU: «Площадка» for both.
- **Build map** is the heading of the build section and a button. RU: the heading «Сборка карты», the button «Собрать карту».

For example: "Under **Build destination**, select the **Export to folder** option. … Select the **Export to folder** button." If the code renames these controls, drop this rule.

## Alerts

Use the GitHub alert syntax, `> [!NOTE]` and so on.

| Type | Use it for |
| --- | --- |
| `NOTE` | Context worth knowing |
| `TIP` | An optional shortcut |
| `IMPORTANT` | A constraint that isn't obvious and that the tool doesn't check, or reports only later. Example: a Road marker applies only to the GameObject it is on, although validation also accepts a collider on a child. |
| `WARNING` | A risk of a broken result. Examples: **Download ZIP** leaves out the submodule; exporting into a folder that isn't empty. |
| `CAUTION` | An action that can't be undone or destroys data. Examples: `git reset --hard` and `git clean -xffd`; **Delete publication**; opening the project in another Unity version, which upgrades it in place. |

- Use at most one alert per H2 section, never two in a row, and at most two sentences in each.
- Never put an alert inside a list item or `<details>`. Place it after the list.
- Never use an alert for prerequisites, required steps or expected results. For example, the exact Unity version is a plain *Before you start* item; a `CAUTION` alert carries only the consequence of using another version.

## Links

- Link inside this repository with relative links, so that they follow the branch being viewed and work in clones.
- Link into the `Assets/Plugins/CarX.Modding.Creator` submodule with absolute URLs to the CarX.Modding.Creator repository at the pinned commit, because GitHub can't resolve submodule paths from this repository: `https://github.com/CarXTechnologies/CarX.Modding.Creator/blob/<commit>/<path>`. `git submodule status` prints the pinned commit, and `check_docs.py` reports a link to another commit or to a branch such as `main`.
- Make the link text describe the target. Never write "here".
- Keep a task page to about 15 links or fewer. `reference.md`, the *See also* lines in `troubleshooting.md`, the message catalogue in `validation.md`, whose rows link to their troubleshooting entries, and the *Documentation* table in the READMEs are exempt.
- To link to a section from another page, give the section an ASCII anchor: put `<a name="road"></a>` on its own line directly before the heading, at the same place in EN and RU. Never link to an auto-generated Cyrillic heading anchor; custom anchors survive renames and translation.

## Code blocks, diagrams and collapsible sections

- Use fenced code blocks, never screenshots, for folder trees, Console output, validation messages and the **Scene contents** text, so that readers can copy and search them.
- Draw the pipeline overview as a Mermaid `flowchart LR`, followed by a one-line text summary.
- Use `<details><summary>…</summary>` for optional or recovery content, such as updating a clone, the SSH fallback for the submodule or the full validation catalogue. Leave a blank line after `</summary>`.
- Use task lists (`- [ ]`) only for the checklist before publishing in `test-in-game.md`.

<a name="src"></a>
## Source comments

A hidden source comment lets a reviewer check a fact against the code when that code changes.

- Add `<!-- src: <path>:<line or range> -->` only to limits, defaults, exact messages and storage paths. Use at most one per paragraph, list item or table row.
- Put the comment at the end of the line that states the fact: the paragraph line, the list item line, or the last cell of the table row. Never put it on a line of its own inside a list.
- Write paths relative to the repository root. Submodule paths start with `Assets/Plugins/CarX.Modding.Creator/`. The game's source code is closed and this repository is public, so never write a path into it: mark a fact that you checked in the game's code with the bare reference `game`, for example `<!-- src: game -->` or `<!-- src: Assets/Editor/MapBuilder.cs:12; game -->`.
- Write the lines as `path:12`, `path:12-20` or `path:12,40-42`, and separate several paths with `; `. When the line also quotes a message in a code span, cite the line of the message too, not only the line of the limit it belongs to.
- When you change code that a comment points at, update the comment in the same PR. `check_docs.py` reports a comment whose file or lines don't exist. It warns when a message or identifier quoted on the same line is no longer near the cited lines, and when a quoted message occurs in none of the cited files. It doesn't check `game` references, and it reports any path into the game's source as an error.
- Mark a menu path that you couldn't check in a running editor with `<!-- verify-menu -->` at the end of its line. A capture run writes `unity-menus.txt` with the items of the **Assets** > **Create**, **File**, **GameObject**, **Tools** and **Edit** menus. Check the marked lines against it and remove each marker you confirm. Check Unity Hub and other menus by hand.

## Two languages

`README.md` is English and `README.ru.md` is Russian. `docs/en/` and `docs/ru/` hold the same files, with the same headings in the same order. Both languages change in the same pull request.

### Translate a page

The Russian page is a faithful translation of the English page, not a summary. It keeps:

- the same headings, steps, tables, alerts, anchors and images, in the same order;
- the same `<!-- src -->` comments;
- MapBuilder images from `docs/images/ru/`, under the same file names as in `docs/images/en/`, and the same shared images.

It changes:

- the switcher to `[English](…) · **Русский**`, as in [Task pages](#task-pages);
- MapBuilder labels to Russian, copied from the `L("<ru>", "<en>")` calls. Add the English label in parentheses on the first mention on each page, for example **Собрать карту** (Build map);
- the alt text, which you write in Russian, also for shared images. Never copy alt text from the other language.

The Russian getting-started page, *Начало работы*, tells readers to set the language dropdown in the MapBuilder header to **RU**.

Use these verbs:

| Action | EN | RU |
| --- | --- | --- |
| Button | select **X** | нажмите **X** |
| Tab, list item, radio option, dropdown value | select **X** | выберите **X** (вкладку **X**) |
| Checkbox or toggle field | select / clear **X** | установите / снимите флажок **X** |
| Setting state | turn on / turn off | включите / выключите |
| Text input | enter | введите |
| Window or page | open, go to | откройте, перейдите |

### Text that stays in English

MapBuilder switches its own labels with the **EN** / **RU** dropdown in its header. The choice is stored in the EditorPrefs key `CarX.MapBuilder.Language`, default `en`. <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:14,58 -->

This text stays in English in both modes. Quote it in English on both pages, and explain it in Russian prose where needed:

- the Unity Editor, which has no Russian interface;
- component inspectors, drawn by `MarkerDataDrawer.cs`, `RenderCoreMaterialMarkerSettingsDrawer.cs`, `AnimationMarkerSettingsDrawer.cs`, `GameMarkerTemplateConfigDrawer.cs` and `CaptureCameraEditor.cs`;
- the **Map Validation** window, and all validation and Console messages;
- most MapBuilder tooltips;
- the text of a publication row on the **Publish** tab: `<n> / 4120 mb`, the file status, `Old version!` and `Blank <n>`;
- the `External path` folder dialog and the `Save Capture` file dialog;
- the **Scene contents** lines `<Type> : <n> count (<min> min - <max> max)`;
- the mod.io sign-in and delete dialogs;
- the `Not signed in to https://mod.io/g/carx-dro2.` box.

This text is localized, so quote it in the page language:

- the tooltips of **Version**, **Summary (optional)**, **+ New map** and the language dropdown;
- **Cancel** and its tooltips;
- the tooltip of the **Select for this map** badge;
- the `Packing the map` / `Упаковка карты` progress bar.

### When one language lags behind

If you can't update both languages in the same pull request, mark the page that lags behind:

1. After its language switcher line, add a `NOTE` alert. On a Russian page it reads «Эта страница отстаёт от английской версии.»; on an English page, "This page lags behind the Russian version."
1. After the alert, add a hidden marker on its own line, with blank lines around it, that names the source page and its commit: `<!-- synced-with: docs/en/<page>.md@<commit> -->`.

Result: readers see that the page is behind, and the marker shows which version to translate from. Finish the translation and remove the marker before a release; a release branch has no `synced-with` markers, and `check_docs.py --release` reports any that are left.

<a name="screenshots"></a>
## Screenshots

The [capture guide](_capture/README.md) explains how to retake a screenshot and lists every shot with its page. This section sets what a screenshot shows and how a page embeds it.

### When to take one

Take a screenshot when:

- the control is hard to find: small, inside a foldout or behind a panel button, or among similar options;
- the reader hasn't seen the window before, which earns one full-window orientation shot per area;
- it shows a result worth checking, such as a finished **Build** tab or the **Map Validation** window.

Don't take one for:

- simple fields, checkboxes or menus whose path the text names;
- popup menus, dropdown lists, OS dialogs and modal prompts, which the capture tool can't capture either;
- Console or log text and folder listings: use a code block;
- the standard Rigidbody and Terrain inspectors.

These topics have no screenshot by design: the marker type menu, the sign-in dialogs, texture Read/Write, the Build Profiles scene list, the **Tools** menu, Unity Hub, the folder layout, the Hierarchy, **Advanced settings**, **Scene contents**, build progress, the Animation marker, the SpawnPoint marker, an unsupported marker, the Rigidbody inspector, Terrain and the `External path` dialog.

### Callouts

- Mark elements with numbered badges: amber `#FFB000` circles 22 px across at the display width, with a 1.5 px `#1A1A1A` ring and a bold 13 px `#1A1A1A` digit. `annotate.py` draws them.
- A badge never covers label text. When its default place would cover it, choose another `place` in `docs/_capture/shots.json`, as described in the [capture guide](_capture/README.md#fix-a-badge-on-the-wrong-element).
- Use at most 8 badges per image, numbered in reading order: top to bottom, then left to right.
- The numbers match the `#` column of the table directly after the image. English and Russian images use the same number for the same element.
- Never draw words, arrows with text or labels on an image. They block translation and search.

`docs/_capture/shots.json` is the single source for each shot's display width, crop and numbered callouts. The `width` attribute on every page must equal the shot's `width` there, and the callout table after the image must have one row for each callout number; `check_docs.py` compares both.

### Embed an image

- Embed the image with HTML and a `width` attribute:

  ```html
  <img src="../images/en/build-tab.png" width="728" alt="Screenshot of the MapBuilder Build tab after a build; callouts 1–7 on the build controls.">
  ```

- English pages use `../images/en/<id>.png` and `../images/shared/<id>.png`. Russian pages use `../images/ru/<id>.png` and the same shared files. `README.md` and `README.ru.md` use `docs/images/en/window-overview.png` and `docs/images/ru/window-overview.png`.
- Place the image directly after the step it illustrates. Inside a list item, indent it 3 spaces, with a blank line before and after it.
- Introduce a standalone image with a full sentence. Never write "see the image above".
- Write the alt text in the page language: 40–150 characters, sentence case, ending with a period. Start with the image type ("Screenshot of …", «Скриншот …») and name what is highlighted as "callouts 1–N", RU «выноски 1–N». Don't repeat the steps. The Russian alt text of the image above reads «Скриншот вкладки «Сборка» окна MapBuilder после сборки; выноски 1–7 на элементах управления сборкой.»

### Files, names and sizes

- Name files in lowercase kebab-case as `<topic>-<subject>[-<state>].png`. The topic is one of `window`, `map`, `build`, `validation`, `publish`, `export`, `scene`, `markers` or `minimap`. Don't use numbers or version suffixes; git history keeps the versions.
- `docs/images/shared/` holds UI that is English in both languages: Unity's own views, inspectors and the **Map Validation** window. `docs/images/en/` and `docs/images/ru/` hold MapBuilder shots under identical file names.
- When you re-capture, overwrite the file under the same name. Delete any image that no page references.
- Keep the display width at 1000 px or less and the height at about 900 px or less; split a taller panel into two shots.
- Aim for 150 KB or less per file; 250 KB is the limit. Use PNG only: no JPEG, no transparency, no GIF or video. Never upscale.
- Every PNG has a 1 px `#6E7681` border, so that dark Unity UI stays distinct on GitHub's dark theme. `annotate.py` adds it.
- Show demo content only: the `TestMap` sample, no personal account names, no community map names and no user folders in paths. Replace sensitive data with realistic samples; never blur it.

<a name="lint"></a>
## Check the docs

`docs/_tools/check_docs.py` checks `README.md`, `README.ru.md`, `docs/en/` and `docs/ru/`; the link and source comment checks also read this guide and the capture guide. CI runs it for every pull request and every push to `main`, `target/**` and `feature/**`, in the *Submodule and project layout* job. Run it before you open a pull request:

```bash
python -I -X utf8 docs/_tools/check_docs.py
```

It needs Python 3 and nothing else. It prints each problem as `<file>:<line>: error: …` or `<file>:<line>: warning: …`, then the totals, and exits with code 1 when there is an error. Warnings don't fail the run.

| Check | What it reports |
| --- | --- |
| `links` | A relative link or image whose file doesn't exist, and a `#anchor` that the target page doesn't have |
| `markdown` | The patterns in [Markdown that GitHub renders differently](#markdown-that-github-renders-differently): `style` widths, alerts in lists, unclosed anchors, a missing blank line before `Result:` or `Next:`, an HTML comment right after a block, two alerts in a row; legacy `Image/` paths; `synced-with` markers, as warnings |
| `parity` | A page that exists in one language only; a page pair, or the README pair, with a different number of H2 and H3 headings or images, different anchors or different `<!-- src -->` comments |
| `images` | An image whose `width` or callout table doesn't match `shots.json`, an image from the folder of the other language, a PNG that no page references or that is over 250 KB, and a shot without its PNG files |
| `versions` | A README or `getting-started.md` page that doesn't name the project's Unity version, or revision; an `Applies to` line on a page other than the READMEs, or one that doesn't match the other README; a clone or `git switch` command in `getting-started.md` with another branch; a link into the CarX.Modding.Creator repository that points to a branch or to a commit other than the submodule commit, or to a missing file |
| `src` | A `<!-- src -->` comment whose file or lines don't exist; as a warning, a line whose quoted messages and identifiers don't occur within 5 lines of the cited lines, with the line where the text is now, and a quoted message that occurs in none of the cited files |

To run some of the checks, pass them with `--check`, for example `--check links,parity`. `--strict` turns warnings into errors, and `--release` turns `synced-with` markers into errors.

The script can't see the badges themselves. Open each changed image and check that every badge sits on the element its table row describes, in both languages.

## Keep version facts current

- **Unity version.** Update the version and the revision in `README.md` and `README.ru.md`, including their `Applies to` and `Относится к` lines, and in both `getting-started.md` pages. Then re-capture all screenshots.
- **Docs branch.** When the branch these docs describe is merged or renamed, update its name in:
  - `README.md` and `README.ru.md`: the `Applies to` and `Относится к` lines;
  - both `getting-started.md` pages, *Get the project*: the clone command and the sentence after it, which explains the `-b` option. That sentence also names the repository's default branch and its Unity version; after a merge into the default branch, rewrite or delete that part;
  - both `getting-started.md` pages, *Update an existing clone*: the `git switch` command.
- **SDK submodule.** When you move the pinned commit of `Assets/Plugins/CarX.Modding.Creator`, re-pin every link into the CarX.Modding.Creator repository to the new commit in the same pull request, and check that the linked files still exist at that commit. `git submodule status` prints the commit, and `grep -rn 'CarX.Modding.Creator/\(blob\|tree\)/' README.md README.ru.md docs/en docs/ru` finds the links. Then run `check_docs.py` and check its warnings about source comments that point into the submodule.

Don't add a root CHANGELOG. Describe author-facing changes in the release notes or the pull request description.

<a name="checklist"></a>
## Pull request checklist

Copy this list into the description of every pull request that changes the docs, or the UI that the docs describe:

```markdown
- [ ] EN and RU are updated in this PR, or the lagging page has the lag note and a `synced-with` marker
- [ ] Screenshots are re-captured for every UI change, and the screenshot date in `README.md` and `README.ru.md` is updated
- [ ] UI labels are copied from the code, not typed from memory
- [ ] `python -I -X utf8 docs/_tools/check_docs.py` reports no errors, and each of its warnings is checked
- [ ] `<!-- verify-menu -->` lines are checked against `unity-menus.txt`, or still marked
- [ ] The diff has no files from `Assets/Editor/DocsCapture/` and no re-serialized `TestMapConfig.asset`, `MapManagerConfig.asset` or `EditorBuildSettings.asset`
- [ ] Every link to a moved or renamed page points to its new path, and the old file is deleted
- [ ] If the docs branch was merged or renamed, its name is updated everywhere listed in "Keep version facts current"
- [ ] If the SDK submodule commit moved, the links into CarX.Modding.Creator are re-pinned to the new commit
```
