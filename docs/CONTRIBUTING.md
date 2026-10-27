# Contributing to the MapUploader documentation

Applies to: MapUploader `feature/2.0/3.0` · Unity 6000.3.19f1 · screenshots 2026-10-26

This guide is for maintainers who change the MapUploader documentation or the MapBuilder UI it describes. It sets the writing rules for both languages, explains how to capture the screenshots with the tooling in `docs/_capture/`, and lists what a docs pull request must contain.

This guide is in English only.

## What lives where

```text
README.md                 English landing page
README.ru.md              Russian landing page
docs/
  CONTRIBUTING.md         this guide
  en/                     English pages
  ru/                     Russian pages: the same files under the same names
  images/
    shared/               UI that is English in both languages
    en/                   MapBuilder screenshots in English
    ru/                   MapBuilder screenshots in Russian, same file names as en/
  _capture/               screenshot tooling, never referenced from the pages
    README.md             short overview of the tooling files
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

Result: the link check in [Check the Markdown](#lint) reports no missing pages.

When you move or rename a page, update every link to it in the same pull request, in both languages, and delete the old file. Don't leave redirect stubs.

## Principles

- **One page, one job.** Each page has one type. Put a variant of a procedure under its own heading, and never write "alternatively…" inside a procedure.
- **The text carries the instructions.** A reader who never loads an image can still finish the task. Images only locate hard-to-find UI or show a result.
- **Every fact traces to code.** Check each limit, default, label, message and path in the code before you write it. When the docs and the code disagree, the code wins. See [Source comments](#src).
- **Describe the current behaviour neutrally.** When the tool behaves in a surprising way, state the behaviour as a fact and give the workaround, for example "After you edit a template, select each marker that uses it." Never call it a bug in the docs; report it in the repository's issues instead.
- **Update the docs in the same PR as the UI.** Text and screenshots change in the pull request that changes the UI they describe. Fix or delete stale text; never excuse it. Sentences such as "This screenshot predates…", "shows an older marker menu" or "shows the older context-menu command" are banned.
- **Game facts need the game's code.** Verify what the game does in the game's source before you write it. Don't promise platforms or load limits that the game team hasn't confirmed: say that maps from mod.io download in game builds that support mod.io, and that very large maps may fail to load.

## Voice, terms and headings

- **Voice.** Write in the second person, imperative mood and present tense, in short sentences. Don't use *please*, *simply*, *just*, *easily* or *obviously*, and don't use *navigate* or *toggle* as verbs.
- **One term per thing.**
  - *map*, never track, level or prototype;
  - *publication* for a mod.io entry, because the UI says `Publications`;
  - *build*, *export* and *validation* for the three operations;
  - *build cache* for the staging folder `StandaloneTemporary<id>`.
  - The [glossary](en/reference.md#glossary) is the authority for terms in both languages.
- **Numbers.** Write numerals with units: `8 MB`, `50 characters`, `64 m`, `512 × 288`.
- **Headings.** Use sentence case and no emoji. English task headings are verb + object, such as "Export the map to the game folder". Russian headings use an action noun, such as «Экспорт карты в папку игры», and stay parallel within a page.
- **No directions.** Never write left, right, above or below. Unity windows dock anywhere, and the **Publish** tab stacks its two columns when MapBuilder is narrower than 1120 points. Name the window, tab, section or field instead. <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:64 -->

## Page anatomy

### Task pages

Every element is its own paragraph. GitHub joins consecutive lines into one paragraph, so keep the blank lines.

```markdown
# <Verb + object>

**English** · [Русский](../ru/<same-file>.md)

Applies to: MapUploader `feature/2.0/3.0` · Unity 6000.3.19f1 · screenshots <YYYY-MM-DD>

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

The Russian page uses the same structure with its own switcher and date line:

```markdown
[English](../en/<same-file>.md) · **Русский**

Относится к: MapUploader `feature/2.0/3.0` · Unity 6000.3.19f1 · скриншоты <DD.MM.YYYY>
```

### Tables

- Use one table per UI panel, with the rows in the same order as the UI.
- Settings tables use the columns `| Option | Default | What it does | When to change |`.
- The callout table goes directly after its image and uses `| # | Element | What it does |`. For a form, use `| # | Field | What to enter | Limit |`, with an extra column such as `Where it appears` when the reader needs it. The `#` column holds the callout number.
- Leave a blank line before a table, and escape `|` inside a cell as `\|`.

### Troubleshooting entries

Entries appear only in `troubleshooting.md`, and the page opens with the triage rule "Run **Validate map**, fix the errors from the top, then build again." Keep every line of an entry its own paragraph:

```markdown
### `<exact message, shortened with … to ≤ 70 characters>`

Severity: Error (blocks the build) · Category: Markers

**Cause.** This happens when …

**Fix.**

1. …

See also: [Road surfaces](markers.md#road)
```

### Markdown that GitHub renders differently

- A line at column 0 right after a list item continues that item. Leave a blank line before `Result:` or `Next:`, or it renders inside the last step.
- An HTML comment at column 0 right after a list item starts an HTML block and ends the list, so with `1.` numbering the next step restarts at 1. Keep a comment at the end of the line it belongs to.
- An unclosed `<a name="x">` alone on a line starts a raw-HTML block that swallows the lines that follow it up to the next blank line, so the heading after it renders as plain `## …` text. Always write `<a name="x"></a>`.
- An alert inside a list item or inside `<details>` renders as a literal `[!WARNING]`.
- GitHub strips the `style` attribute, so `style="width:400px"` shows the image at full size. Use the `width` attribute.
- A nested ordered list renders as i., ii., and Markdown has no letter markers: `a.` and `b.` render as plain text.
- Without a blank line after `</summary>`, the content of `<details>` doesn't render as Markdown.

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
- Link into the `Assets/Plugins/CarX.Modding.Creator` submodule with absolute URLs to the CarX.Modding.Creator repository at the pinned commit, because GitHub can't resolve submodule paths from this repository: `https://github.com/CarXTechnologies/CarX.Modding.Creator/blob/<commit>/<path>`. `git submodule status` prints the pinned commit.
- Make the link text describe the target. Never write "here".
- Keep a task page to about 15 links or fewer. `reference.md`, the *See also* lines in `troubleshooting.md` and the *Documentation* table in the READMEs are exempt.
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
- Write paths relative to the repository root. Submodule paths start with `Assets/Plugins/CarX.Modding.Creator/`. The prefix `DR3PC1:` marks a path in the game's source code, which is available only inside CarX.
- When you change code that a comment points at, update the comment in the same PR.
- Mark a menu path that you couldn't check in a running editor with `<!-- verify-menu -->` at the end of its line. A capture run writes `unity-menus.txt` with the items of the **Assets** > **Create**, **File**, **GameObject**, **Tools** and **Edit** menus. Check the marked lines against it and remove each marker you confirm. Check Unity Hub and other menus by hand.

## Two languages

`README.md` is English and `README.ru.md` is Russian. `docs/en/` and `docs/ru/` hold the same files, with the same headings in the same order. Both languages change in the same pull request.

### Translate a page

The Russian page is a faithful translation of the English page, not a summary. It keeps:

- the same headings, steps, tables, alerts, anchors and images, in the same order;
- the same `<!-- src -->` comments;
- MapBuilder images from `docs/images/ru/`, under the same file names as in `docs/images/en/`, and the same shared images.

It changes:

- the switcher to `[English](…) · **Русский**` and the date line to `Относится к: …`, as in [Task pages](#task-pages);
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

1. After its `Applies to` or `Относится к` line, add a `NOTE` alert. On a Russian page it reads «Эта страница отстаёт от английской версии.»; on an English page, "This page lags behind the Russian version."
1. After the alert, add a hidden marker on its own line, with blank lines around it, that names the source page and its commit: `<!-- synced-with: docs/en/<page>.md@<commit> -->`.

Result: readers see that the page is behind, and the marker shows which version to translate from. Finish the translation and remove the marker before a release; a release branch has no `synced-with` markers.

## Screenshots

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

### Shots and pages

The docs use 14 screenshots. The shot id is the file name without `.png`. MapBuilder shots exist in English and Russian under the same name; the other shots are shared by both languages.

| Shot | Folder | Page and section |
| --- | --- | --- |
| `window-overview` | `en`, `ru` | `README.md` and `README.ru.md`; `getting-started.md`, Open MapBuilder |
| `map-settings` | `en`, `ru` | `map-settings.md`, Fill in the Map tab |
| `build-tab` | `en`, `ru` | `build.md`, The Build tab |
| `build-optimization` | `en`, `ru` | `build.md`, Optimization |
| `export-folder` | `en`, `ru` | `test-in-game.md`, Export the map to the game folder |
| `publish-modio` | `en`, `ru` | `publish.md`, The Publish tab |
| `validation-window` | `shared` | `validation.md`, Read the results |
| `scene-hdrp-lit-material` | `shared` | `scene-setup.md`, Set up materials |
| `markers-template-config` | `shared` | `markers.md`, Reuse road values with templates |
| `markers-road-template` | `shared` | `markers.md`, Reuse road values with templates |
| `markers-material` | `shared` | `markers.md`, Replace materials with render core variants |
| `minimap-component` | `shared` | `minimap-and-images.md`, Add the minimap |
| `minimap-top-view` | `shared` | `minimap-and-images.md`, Add the minimap |
| `minimap-capture-camera` | `shared` | `minimap-and-images.md`, Capture images with CaptureCamera |

`docs/_capture/shots.json` is the single source for each shot's display width, crop and numbered callouts. The `width` attribute on every page must equal the shot's `width` there, and the callout table after the image must have one row for each callout number. The [image check](#images) compares both.

### Callouts

- Mark elements with numbered badges: amber `#FFB000` circles 22 px across at the display width, with a 1.5 px `#1A1A1A` ring and a bold 13 px `#1A1A1A` digit. `annotate.py` draws them.
- Each callout's `place` in `shots.json` sets where its badge sits: `left` (the default, 6 px left of the element and vertically centred), `right`, `top-right`, `top-left`, `above`, `above-left`, `below`, `below-left` or `center`. `dx` and `dy` shift the badge further. When a `left` badge has no room in the crop, `annotate.py` moves it to the element's top-right corner.
- A badge never covers label text. When the default would cover it, choose another `place`.
- Use at most 8 badges per image, numbered in reading order: top to bottom, then left to right.
- The numbers match the `#` column of the table directly after the image. English and Russian images use the same number for the same element.
- Never draw words, arrows with text or labels on an image. They block translation and search.

### Embed an image

- Embed the image with HTML and a `width` attribute:

  ```html
  <img src="../images/en/build-tab.png" width="728" alt="Screenshot of the MapBuilder Build tab after a build; markers 1–7 on the build controls.">
  ```

- English pages use `../images/en/<id>.png` and `../images/shared/<id>.png`. Russian pages use `../images/ru/<id>.png` and the same shared files. `README.md` and `README.ru.md` use `docs/images/en/window-overview.png` and `docs/images/ru/window-overview.png`.
- Place the image directly after the step it illustrates. Inside a list item, indent it 3 spaces, with a blank line before and after it.
- Introduce a standalone image with a full sentence. Never write "see the image above".
- Write the alt text in the page language: 40–150 characters, sentence case, ending with a period. Start with the image type ("Screenshot of …", «Скриншот …») and name what is highlighted. Don't repeat the steps.

### Files, names and sizes

- Name files in lowercase kebab-case as `<topic>-<subject>[-<state>].png`. The topic is one of `window`, `map`, `build`, `validation`, `publish`, `export`, `scene`, `markers` or `minimap`. Don't use numbers or version suffixes; git history keeps the versions.
- `docs/images/shared/` holds UI that is English in both languages: Unity's own views, inspectors and the **Map Validation** window. `docs/images/en/` and `docs/images/ru/` hold MapBuilder shots under identical file names.
- When you re-capture, overwrite the file under the same name. Delete any image that no page references.
- Keep the display width at 1000 px or less and the height at about 900 px or less; split a taller panel into two shots.
- Aim for 150 KB or less per file; 250 KB is the limit. Use PNG only: no JPEG, no transparency, no GIF or video. Never upscale.
- Every PNG has a 1 px `#6E7681` border, so that dark Unity UI stays distinct on GitHub's dark theme. `annotate.py` adds it.
- Show demo content only: the `TestMap` sample, no personal account names, no community map names and no user folders in paths. Replace sensitive data with realistic samples; never blur it.

<a name="capture"></a>
## Capture screenshots

The tooling in `docs/_capture/` takes every screenshot from a running editor and annotates it, so one run re-creates all images after a UI change. It lives outside `Assets/`, so it never compiles for map authors. The [capture tooling README](_capture/README.md) gives a short overview of its files; for the procedure, follow this guide.

A run has two parts:

- `DocsCapture.cs`, an editor script, puts each window into a fixed state, saves a raw PNG of the window and a JSON file with the position of every UI element, and restores the project afterwards.
- `annotate.py` crops each raw PNG, scales it down to at most 1.5 times its display width by default, draws the badges and the border, and writes `docs/images/<en|ru|shared>/<id>.png`.

> [!CAUTION]
> The run opens `TestMap` in place of your open scenes without asking, so any unsaved scene changes are lost.

### Before you start

- Windows display scale 175%, the scale of the current images. The callouts of the shared shots are pixel positions in `shots.json` at this scale, so a run at another scale puts their badges in the wrong place. To change the scale, re-measure every `px` anchor with `--debug`.
- A display with room for 1136 × 1040 points at 175%, about 2000 × 1820 pixels, such as a 4K display. The tool opens MapBuilder at 1136 × 760 points, inspectors 460 points wide and up to 1040 points tall, the **Map Validation** window at 900 × 700 points and the Scene view at 700 × 700 points.
- Unity 6000.3.19f1 with the Dark editor theme and the editor language set to English.
- The project open in the editor, with no compile errors and all open scenes saved.
- The editor in the same mod.io sign-in state as the current images. See [Sign-in state](#sign-in-state).
- Python 3 with Pillow 9.1 or later, for example from `pip install pillow`.

### Capture the raw shots

To capture the raw shots:

1. To check afterwards that the run changed nothing else, in Git Bash, in the repository folder, record the state of the working tree:

   ```bash
   snapshot() { git ls-files -m -o --exclude-standard | sort | while IFS= read -r p; do
     if [ -e "$p" ]; then echo "$(git hash-object -- "$p") $p"; else echo "deleted $p"; fi
   done; }
   snapshot > ../capture-before.txt
   ```

   Keep this terminal open: you use `snapshot` again after the run. Unity rewrites some tracked files when it opens the project, such as `Assets/MapResources/TestMap/TestMapConfig.asset` and `ProjectSettings/EditorBuildSettings.asset`, so the tree is rarely clean; the record covers them.
1. Copy the capture script into the project:

   ```bash
   mkdir -p Assets/Editor/DocsCapture && cp docs/_capture/Editor/* Assets/Editor/DocsCapture/
   ```

   Unity compiles it as a separate Editor-only assembly, `MapUploader.DocsCapture`, and adds the **Tools** > **Docs** > **Capture screenshots** menu item.
1. In the Unity menu bar, select **Tools** > **Docs** > **Capture screenshots**.
   The run builds `TestMap` once for each language, so it takes several minutes. Don't use the editor until the Console shows `[DocsCapture] DocsCapture done`, or `[DocsCapture] DocsCapture FAILED` when the run stopped on an error.
1. Open `Library/DocsCapture/capture.log` and check its first lines: `unity 6000.3.19f1`, `pixelsPerPoint 1.75`, `proSkin True` and `TestMap enabled in build scenes: True`. Compare the values with these; the tool doesn't check them and doesn't stop on a mismatch. <!-- src: docs/_capture/Editor/DocsCapture.cs:228-237 -->

Result: `Library/DocsCapture/en/`, `ru/` and `shared/` hold a PNG and a JSON file for each shot, `capture.log` has a `shot <folder>/<id>` line with no `failed` for every shot and ends with `DocsCapture done`, and the folder also holds `unity-menus.txt`.

### Annotate and check the images

To turn the raw shots into the published images:

1. In the same terminal, run the annotation script on the raw shots:

   ```bash
   python docs/_capture/annotate.py Library/DocsCapture
   ```

   The script prints each file it writes with its size in pixels and in KB, adds `OVER 250 KB` to a file that is too large, and prints `FAILED <folder>/<id>: …` for a shot it couldn't annotate. To redo some shots, add `--only build-tab,build-optimization`.
1. Delete the copied script and the `.meta` files Unity created for it:

   ```bash
   rm -r Assets/Editor/DocsCapture Assets/Editor/DocsCapture.meta
   ```

1. Compare the working tree with the record you made before the run:

   ```bash
   snapshot > ../capture-after.txt
   diff ../capture-before.txt ../capture-after.txt
   ```

   Expect differences only under `docs/images/` and in the `.meta` files of models and textures under `Assets/MapResources/TestMap/Art/`, which the `TestMap` builds re-serialize. Restore each `.meta` file with `git checkout -- <path>`, and commit only `docs/images/`.
1. Open each changed image and check that every badge sits on the element that the table after the image describes.
1. In the pages whose screenshots changed, update the date in the `Applies to` and `Относится к` lines.

Result: `docs/images/` holds the new screenshots, and nothing else in the working tree changed. In the pull request, review each changed image against its previous version with GitHub's swipe or onion-skin image view.

When a badge lands on the wrong element, run `annotate.py` with `--debug`. It writes `<id>.debug.png` next to the raw shot, with the rectangle of every UI element in the layout file drawn. MapBuilder callouts follow the UI, because `shots.json` anchors them to elements by type, class, binding path or label text. The shared shots of inspectors, the **Map Validation** window and the Scene view use fixed pixel rectangles in `shots.json` instead, because most of their content is drawn with IMGUI, which has no element rectangles. Check and adjust those rectangles whenever that UI changes.

### Run the capture from the command line

The script also has a command-line entry point, for example to redo a few shots without the menu. The editor opens its windows during the run, so the command doesn't use `-batchmode`.

To run the capture from the command line:

1. Close the project in the editor.
1. Record the state of the working tree and copy the capture script into the project, as in the first two steps of [Capture the raw shots](#capture-the-raw-shots).
1. In Git Bash, in the repository folder, start the editor with the capture method:

   ```bash
   "C:/Program Files/Unity/Hub/Editor/6000.3.19f1/Editor/Unity.exe" -projectPath "$(pwd -W)" \
     -executeMethod MapUploader.DocsCapture.DocsCapture.Run \
     -docsCaptureOnly build-tab,build-optimization -docsCaptureExit
   ```

   The editor opens the project, runs the capture and closes. The path is the default Unity Hub location; change it if you installed the editor elsewhere.

Result: `Library/DocsCapture` holds the raw shots and `capture.log`. Continue with [Annotate and check the images](#annotate-and-check-the-images), and pass the same ids to `annotate.py` with `--only`. The run doesn't clear `Library/DocsCapture`, so without `--only` the script also re-annotates raw shots left from earlier runs.

| Option | What it does |
| --- | --- |
| `-docsCaptureOnly <id>,<id>` | Captures only these shots. Without it, the run captures all shots. A MapBuilder shot of the **Build** or **Publish** tab still builds `TestMap` in both languages. |
| `-docsCaptureOut <folder>` | Writes the raw shots to this folder instead of `Library/DocsCapture`. |
| `-docsCaptureExit` | Closes the editor when the run ends, with exit code 1 if it failed. |

### What the run changes and restores

The run never saves a scene. While it runs, it:

- closes every **MapBuilder** and **Map Validation** window, docked ones included, and doesn't reopen them;
- writes the demo metadata into `TestMapConfig.asset`: version `1.0.0`, a summary, a description, the author `CarX Technologies` and the repository URL;
- builds `TestMap` in Binary once in each language, because MapBuilder stores the build summary in the language that was active at build time;
- adds temporary objects to the open scene and creates temporary assets in memory for the validation and marker shots;
- changes EditorPrefs, such as the MapBuilder language and the foldout state of HDRP Lit materials;
- for the Scene view shot, turns off every gizmo and icon except the minimap box and hides the other zone objects, then turns them back on.

At the end, it:

- writes back the bytes of `Assets/MapResources/TestMap/TestMapConfig.asset`, `Assets/Resources/MapManagerConfig.asset` and `Assets/Resources/GameMarkerTemplateConfig.asset`;
- restores the EditorPrefs keys `CarX.MapBuilder.Language`, `CarX.MapBuilder.BuildSummary.<id>` and `CoreRP:Material:UI_State:HDRP/Lit`, and the gizmo icon settings;
- deletes the material bake folder `Assets/MapResources/TestMap/Art/light_tower/RenderCore` if it exists;
- deletes the build cache `StandaloneTemporary<id>` of `TestMap`, even if it existed before the run, so rebuild `TestMap` before you export or publish it again;
- reopens `TestMap` without saving it.

If the editor closes during a run, nothing is restored. Restore the three assets with `git checkout -- <path>`, and select your language again in the MapBuilder header.

Unity rewrites `TestMapConfig.asset` and `EditorBuildSettings.asset` when it opens the project, and MapBuilder re-serializes `MapManagerConfig.asset` when its window closes or a build ends. Don't commit these changes in a docs pull request, and don't write docs that depend on them.

### What the tool can't capture

The tool captures editor windows only. It can't capture popup menus, dropdown lists, OS file and folder dialogs or the modal mod.io sign-in prompts; describe those in text.

<a name="sign-in-state"></a>
### Sign-in state

Two shots depend on whether the editor is signed in to mod.io when the run starts:

- `window-overview` shows the account area of the MapBuilder header: `Account: <name>` with **Sign out**, or `Not signed in` with **Sign in**.
- `publish-modio` shows the **Publish** tab. Signed in, the publications list holds the linked publication. Signed out, the tab shows the `Not signed in to https://mod.io/g/carx-dro2.` box and an empty list, and **Create publication** and **Update publication** are unavailable.

`shots.json` anchors the callouts of these two shots for one state. The callout tables in `README.md`, `getting-started.md` and `publish.md` must describe the same state in both languages; the [image check](#images) reports a table whose rows don't match the callouts. The account area of `docs/images/en/window-overview.png` shows which state the current images use; capture in the same state. If you change the state, update `shots.json` and those tables in the same pull request, or `annotate.py` stops on those shots with `no element for anchor`.

To capture signed in, use a dedicated demo mod.io account that owns a publication titled `TestMap`, never a personal account. Sign-in is always manual: the terms of use, the email, then a one-time code. When the editor is signed in, the run links the `TestMap` publication, builds the map after linking, shows only that publication in the list and shows the account name as `MapAuthor`.

### When to re-capture

Re-capture in every pull request that changes what a shot shows:

| You change | Re-capture |
| --- | --- |
| `Assets/Editor/MapBuilderEditorWindow*.cs` or `MapBuilderEditorWindow.uss` | `window-overview`, `map-settings`, `build-tab`, `build-optimization`, `export-folder`, `publish-modio` |
| `Assets/Editor/BuildOptimizationPanel.cs`, or the defaults in `BuildOptimizationSettings.cs` in the submodule | `build-optimization` |
| Anything in `Assets/Editor/Validation/` | `validation-window` |
| `Assets/Editor/MarkerDataDrawer.cs` | `markers-road-template`, `markers-material` |
| `Assets/Editor/GameMarkerTemplateConfigDrawer.cs` | `markers-template-config` |
| `Assets/Editor/RenderCoreMaterialMarkerSettingsDrawer.cs` | `markers-material` |
| `Assets/Scripts/Minimap.cs` or `Assets/Scripts/DrawZoneBehaviour.cs` | `minimap-component`, `minimap-top-view` |
| `Assets/Scripts/CaptureCamera.cs` or `Assets/Editor/CaptureCameraEditor.cs` | `minimap-capture-camera` |
| The `TestMap` scene or its config, the Unity version or the HDRP version | All shots |

<a name="lint"></a>
## Check the Markdown

CI doesn't check the docs yet, apart from requiring that `README.md` names the Unity version from `ProjectSettings/ProjectVersion.txt`. Before you open a pull request, run these checks in Git Bash in the repository folder. They cover `README.md`, `README.ru.md`, `docs/en/` and `docs/ru/`; the link check also covers this guide. They print nothing, or `0 problem(s)`, when everything passes.

### Links and anchors

This script checks that every relative link and image points to an existing file, and that every `#anchor` exists on the target page:

```bash
python - <<'EOF'
import pathlib, re
files = [pathlib.Path(p) for p in ('README.md', 'README.ru.md', 'docs/CONTRIBUTING.md')]
files += sorted(pathlib.Path('docs').glob('[er][nu]/**/*.md'))
def anchors(path):
    text = path.read_text(encoding='utf-8')
    names = set(re.findall(r'<a name="([^"]+)"></a>', text))
    names |= {re.sub(r'[^\w\- ]', '', h.strip().lower()).replace(' ', '-') for h in re.findall(r'^#{1,6} (.+)$', text, re.M)}
    return names
problems = 0
for page in files:
    if not page.exists():
        continue
    text = re.sub(r'```.*?```', '', page.read_text(encoding='utf-8'), flags=re.S)
    text = re.sub(r'`[^`\n]*`', '', text)
    for link in re.findall(r'\]\(([^)\s]+)\)', text) + re.findall(r'<img src="([^"]+)"', text):
        if re.match(r'[a-z]+:', link):
            continue
        path, _, fragment = link.partition('#')
        target = (page.parent / path).resolve() if path else page.resolve()
        if not target.exists():
            print(f'{page}: missing {link}'); problems += 1
        elif fragment and target.suffix == '.md' and fragment not in anchors(target):
            print(f'{page}: no anchor {link}'); problems += 1
print(f'{problems} problem(s)')
EOF
```

### Patterns that break rendering

These commands find `style` widths, alerts inside lists, legacy `Image/` paths, unclosed anchors and leftover `synced-with` markers, then lines that need a blank line before them and alerts that follow each other:

```bash
F="README.md README.ru.md docs/en docs/ru"
grep -rnE --include='*.md' '<img[^>]*style=' $F
grep -rnE --include='*.md' '^\s+> \[!' $F
grep -rn --include='*.md' 'Image/' $F
grep -rnP --include='*.md' '<a name="[^"]*">(?!</a>)' $F
grep -rn --include='*.md' 'synced-with' $F
find $F -name '*.md' -exec awk 'FNR==1{prev=""; lastnb=""}
  /^(Result|Next):/ && prev!="" {print FILENAME":"FNR": no blank line before "$1}
  /^<!--/ && prev!="" {print FILENAME":"FNR": HTML comment right after a block"}
  /^> \[!/ && lastnb ~ /^>/ {print FILENAME":"FNR": two alerts in a row"}
  {prev=$0; if ($0!="") lastnb=$0}' {} +
```

A `synced-with` marker is allowed during work on a feature branch, but not on a release branch.

### Language parity

These commands check that both languages have the same pages, and the same number of H2 and H3 headings and images on each page:

```bash
diff <(cd docs/en && find . -name '*.md' | sort) <(cd docs/ru && find . -name '*.md' | sort)
for f in $(cd docs/en && find . -name '*.md' | sort); do
  for pattern in '^#{2,3} ' '<img '; do
    en=$(grep -cE "$pattern" "docs/en/$f"); ru=$(grep -cE "$pattern" "docs/ru/$f" 2>/dev/null)
    [ "$en" = "$ru" ] || echo "$f: '$pattern' en $en, ru ${ru:-missing}"
  done
done
```

<a name="images"></a>
### Images

This script checks every embedded screenshot against `docs/_capture/shots.json`: the `width` attribute must equal the shot's `width`, and the callout table after the image must number its rows exactly like the shot's callouts:

```bash
python - <<'EOF'
import json, pathlib, re
shots = {s['id']: s for s in json.load(open('docs/_capture/shots.json', encoding='utf-8'))['shots']}
pages = [pathlib.Path(p) for p in ('README.md', 'README.ru.md')] + sorted(pathlib.Path('docs').glob('[er][nu]/**/*.md'))
problems = 0
for page in pages:
    if not page.exists():
        continue
    lines = page.read_text(encoding='utf-8').splitlines()
    for i, line in enumerate(lines):
        m = re.search(r'<img src="([^"]+)" width="(\d+)"', line)
        if not m:
            continue
        src, width = m.groups()
        shot = shots.get(pathlib.Path(src).stem)
        if shot is None:
            print(f'{page}:{i + 1}: {src} is not in shots.json'); problems += 1
            continue
        if int(width) != shot['width']:
            print(f'{page}:{i + 1}: width {width}, shots.json says {shot["width"]}'); problems += 1
        table = []
        for row in [l.strip() for l in lines[i + 1:] if l.strip()]:
            if not row.startswith('|'):
                break
            table.append(row)
        numbers = [int(n) for n in re.findall(r'^\|\s*(\d+)\s*\|', '\n'.join(table), re.M)]
        expected = [c['n'] for c in shot.get('callouts', [])]
        if numbers != expected:
            print(f'{page}:{i + 1}: table numbers {numbers}, callouts {expected}'); problems += 1
print(f'{problems} problem(s)')
EOF
```

These commands list images that no page references, and images over 250 KB:

```bash
for f in docs/images/shared/*.png docs/images/en/*.png docs/images/ru/*.png; do
  [ -e "$f" ] || continue
  grep -rqF --include='*.md' "${f#docs/}" README.md README.ru.md docs/en docs/ru || echo "unreferenced: $f"
done
find docs/images -name '*.png' -size +250k
```

The scripts can't see the badges themselves. Open each changed image and check that every badge sits on the element its table row describes, in both languages.

### Unity version

These commands list pages that don't name the project's Unity version, and `getting-started.md` pages that don't name its revision:

```bash
v="$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt | tr -d '\r')"
r="$(sed -n 's/^m_EditorVersionWithRevision: .*(\(.*\))/\1/p' ProjectSettings/ProjectVersion.txt | tr -d '\r')"
grep -rL --include='*.md' "Unity $v" README.md README.ru.md docs/en docs/ru
grep -L "$r" docs/en/getting-started.md docs/ru/getting-started.md
```

## Keep version facts current

- **Unity version.** Update the version and the revision in `README.md`, `README.ru.md` and both `getting-started.md` pages, and the `Applies to` and `Относится к` lines on every page. Then re-capture all screenshots.
- **Submodule commit.** When the pinned commit of `Assets/Plugins/CarX.Modding.Creator` changes, update the absolute links into it. `grep -rn 'CarX.Modding.Creator/\(blob\|tree\)/' README.md README.ru.md docs/en docs/ru` finds them.
- **Branch.** When the docs describe another branch, update the clone command in both `getting-started.md` pages and the `Applies to` and `Относится к` lines.

Don't add a root CHANGELOG. Describe author-facing changes in the release notes or the pull request description.

<a name="checklist"></a>
## Pull request checklist

Copy this list into the description of every pull request that changes the docs, or the UI that the docs describe:

```markdown
- [ ] EN and RU are updated in this PR, or the lagging page has the lag note and a `synced-with` marker
- [ ] Screenshots are re-captured for every UI change, and the `Applies to` / `Относится к` dates are updated on those pages
- [ ] Callout numbers in `docs/_capture/shots.json` match the `#` tables in both languages
- [ ] UI labels are copied from the code, not typed from memory
- [ ] `<!-- src -->` comments still point at the right lines
- [ ] `<!-- verify-menu -->` lines are checked against `unity-menus.txt`, or still marked
- [ ] The checks in "Check the Markdown" pass
- [ ] The diff has no files from `Assets/Editor/DocsCapture/` and no re-serialized `TestMapConfig.asset`, `MapManagerConfig.asset` or `EditorBuildSettings.asset`
- [ ] Every link to a moved or renamed page points to its new path, and the old file is deleted
```
