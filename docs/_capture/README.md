# Capture screenshots

This guide is the procedure for retaking the screenshots in `docs/images/`. What a screenshot shows, how its callouts look and how a page embeds it are rules of the style guide; see [Screenshots](../CONTRIBUTING.md#screenshots).

The tooling in this folder takes every screenshot from a running editor and annotates it, so one run re-creates all images after a UI change. It lives outside `Assets/`, so Unity never compiles it for map authors.

| File | What it does |
| --- | --- |
| `Editor/DocsCapture.cs`, `Editor/MapUploader.DocsCapture.asmdef` | Editor script, copied into the project only while you capture. It puts each window into a fixed state on the `TestMap` sample and saves a raw PNG of the window and a JSON file with the position of every UI Toolkit element. |
| `shots.json` | The crop, display width and numbered callouts of every shot. |
| `annotate.py` | Crops each raw PNG, scales it down to at most 1.5 times its display width, draws the badges and the border, and writes `docs/images/<en\|ru\|shared>/<id>.png`. |
| `requirements.txt` | The Python package that `annotate.py` needs: Pillow 9.1 or later. |

## Shots and pages

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

## Before you start

- Any Windows display scale. `shots.json` gives its fixed positions in points, and `annotate.py` multiplies them by the scale of the capture. At 175% or more, every image comes out at the size of the current images; at a lower scale, some images come out smaller, because `annotate.py` never upscales.
- A display with room for 1136 × 1040 points at that scale, for example 1988 × 1820 pixels at 175%, as on a 4K display. The tool opens MapBuilder at 1136 × 760 points, inspectors 460 points wide and up to 1040 points tall, the **Map Validation** window at 900 × 700 points and the Scene view at 700 × 700 points.
- Unity 6000.3.19f1 with the Dark editor theme and the editor language set to English.
- The project open in the editor, with no compile errors and all open scenes saved.
- For `publish-modio`, the editor signed in to the demo mod.io account. See [Sign-in state](#sign-in-state).
- Git Bash, for the commands in this guide.
- Python 3 with Pillow 9.1 or later. To install Pillow, run `pip install -r docs/_capture/requirements.txt`.

## Capture the raw shots

> [!CAUTION]
> The run opens `TestMap` in place of your open scenes without asking, so any unsaved scene changes are lost.

To capture the raw shots:

1. To check afterwards that the run changed nothing else, in Git Bash, in the repository folder, record the state of the working tree:

   ```bash
   snapshot() { git ls-files -m -o --exclude-standard | sort | while IFS= read -r p; do
     if [ -e "$p" ]; then echo "$(git hash-object -- "$p") $p"; else echo "deleted $p"; fi
   done; }
   snapshot > ../capture-before.txt
   ```

   Keep this terminal open: you use `snapshot` again after the run. The record lists every modified and untracked file with its content hash, so your own uncommitted changes are part of it. Unity rewrites some tracked files when it opens the project, such as `Assets/MapResources/TestMap/TestMapConfig.asset` and `ProjectSettings/EditorBuildSettings.asset`, so the tree is rarely clean; the record covers them too.
1. Copy the capture script into the project:

   ```bash
   mkdir -p Assets/Editor/DocsCapture && cp docs/_capture/Editor/* Assets/Editor/DocsCapture/
   ```

   Unity compiles it as a separate Editor-only assembly, `MapUploader.DocsCapture`, and adds the **Tools** > **Docs** > **Capture screenshots** menu item.
1. In the Unity menu bar, select **Tools** > **Docs** > **Capture screenshots**.
   The run builds `TestMap` once for each language, so it takes several minutes. Don't use the editor until the Console shows `[DocsCapture] DocsCapture done`, or `[DocsCapture] DocsCapture FAILED` when the run failed.
1. Open `Library/DocsCapture/capture.log` and check its first lines: `unity 6000.3.19f1`, `pixelsPerPoint` with your display scale, such as `1.75` at 175%, `proSkin True` and `TestMap enabled in build scenes: True`. The tool doesn't check these values and doesn't stop on a mismatch. <!-- src: docs/_capture/Editor/DocsCapture.cs:342-354 -->

Result: `Library/DocsCapture/en/`, `ru/` and `shared/` hold a PNG and a JSON file for each shot, the folder also holds `unity-menus.txt`, and `capture.log` has a `shot <folder>/<id>` line for every shot, no `FAILED:` line, and ends with `DocsCapture done`.

When it starts, the run deletes the raw shots of an earlier run from the output folder, so the folder never holds an old shot in place of one that failed. The run fails, and `capture.log` names the cause in a `FAILED:` line, when a shot can't be saved, a wait times out (900 s for a build, 60 s for the publication list), `TestMap` doesn't build, an object or a window that a shot needs isn't there, or a shot that the run should take has no raw files at the end.

## Annotate and check the images

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

   Expect differences only under `docs/images/` and in the `.meta` files of models and textures under `Assets/MapResources/TestMap/Art/`, which the `TestMap` builds re-serialize.
1. For each changed file outside `docs/images/` that `capture-before.txt` doesn't list, undo the run's change with `git checkout -- <path>`. A file that both records list held your own changes before the run, and `git checkout` would discard them too, so undo the run's change to that file by hand.
1. Run the docs checks, which compare the `width` and the callout table of every embedded image with `shots.json` and report images over 250 KB:

   ```bash
   python -I -X utf8 docs/_tools/check_docs.py
   ```

1. Open each changed image and check that every badge sits on the element that the table after the image describes, in both languages.
1. Update the screenshot date in the `Applies to` line of `README.md` and the `Относится к` line of `README.ru.md`.

Result: `docs/images/` holds the new screenshots, and the run left no other change in the working tree. Commit `docs/images/` and the two README files, plus `shots.json` if you changed it. In the pull request, review each changed image against its previous version with GitHub's swipe or onion-skin image view.

## Capture from the command line

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

Result: `Library/DocsCapture` holds the raw shots and `capture.log`. Continue with [Annotate and check the images](#annotate-and-check-the-images), and pass the same ids to `annotate.py` with `--only`.

| Option | What it does |
| --- | --- |
| `-docsCaptureOnly <id>,<id>` | Captures only these shots, and first deletes only their raw files. Without it, the run captures all shots and first deletes all raw shots. A MapBuilder shot of the **Build** or **Publish** tab still builds `TestMap` in both languages. An unknown id fails the run. |
| `-docsCaptureOut <folder>` | Writes the raw shots to this folder instead of `Library/DocsCapture`. |
| `-docsCaptureExit` | Closes the editor when the run ends, with exit code 1 if the run failed. |

## What the run changes and restores

The run never saves a scene. While it runs, it:

- deletes the raw shots of an earlier run from its output folder, as described in [Capture the raw shots](#capture-the-raw-shots);
- closes every **MapBuilder** and **Map Validation** window, docked ones included, and doesn't reopen them;
- writes the demo metadata into `TestMapConfig.asset`: version `1.0.0`, a summary, a description, the author `CarX Technologies` and the repository URL;
- builds `TestMap` in Binary once in each language, because MapBuilder stores the build summary in the language that was active at build time;
- adds temporary objects to the open scene and creates temporary assets in memory for the validation and marker shots;
- changes EditorPrefs, such as the MapBuilder language and the foldout state of HDRP Lit materials;
- for the Scene view shot, turns off every gizmo and icon except the minimap box and hides the other zone objects, then turns them back on, also when the run stops on an error.

At the end, it:

- writes back the bytes of `Assets/MapResources/TestMap/TestMapConfig.asset`, `Assets/Resources/MapManagerConfig.asset` and `Assets/Resources/GameMarkerTemplateConfig.asset`;
- restores the EditorPrefs keys `CarX.MapBuilder.Language`, `CarX.MapBuilder.BuildSummary.<id>` and `CoreRP:Material:UI_State:HDRP/Lit`, and the gizmo icon settings;
- deletes the material bake folder `Assets/MapResources/TestMap/Art/light_tower/RenderCore` if it exists;
- deletes the build cache `StandaloneTemporary<id>` of `TestMap`, even if it existed before the run, so rebuild `TestMap` before you export or publish it again;
- reopens `TestMap` without saving it.

It doesn't restore the `.meta` files that the `TestMap` builds re-serialize; the comparison with your record in [Annotate and check the images](#annotate-and-check-the-images) finds them.

If the editor closes during a run, nothing is restored. Compare the working tree with your record and undo the run's changes as in [Annotate and check the images](#annotate-and-check-the-images), then select your language again in the MapBuilder header.

Unity rewrites `TestMapConfig.asset` and `EditorBuildSettings.asset` when it opens the project, and MapBuilder re-serializes `MapManagerConfig.asset` when its window closes or a build ends. Don't commit these changes in a docs pull request, and don't write docs that depend on them.

## What the tool can't capture

The tool captures editor windows only. It can't capture popup menus, dropdown lists, OS file and folder dialogs or the modal mod.io sign-in prompts; describe those in text.

<a name="sign-in-state"></a>
## Sign-in state

Two shots show whether the editor is signed in to mod.io when the run starts:

- `window-overview` shows the account area of the MapBuilder header: `Account: <name>` with **Sign out**, or `Not signed in` with **Sign in**. Its callout 1 is anchored to whichever of the two buttons is there.
- `publish-modio` shows the **Publish** tab. Signed in, the publications list holds the linked publication, and callout 5 marks its selected row. Signed out, the tab shows the `Not signed in to https://mod.io/g/carx-dro2.` box and an empty list, so there is no row for callout 5, and `annotate.py` stops on this shot with `no element for anchor` and a note that the shot needs the signed-in state.

The current images are captured signed in, and the pages describe that state in both languages: the callout table of `window-overview` in `README.md` says the screenshot shows `Account: <name>`, and the alt text and the callout table of `publish-modio` in `publish.md` describe the signed-in tab. Capture signed in too. If you capture `window-overview` signed out, update those lines in the same pull request.

To capture signed in, sign in to the demo mod.io account, which owns a publication titled `TestMap`. Never use a personal account. The account belongs to Evgenie Myasnikov (GitHub `Evgenie-Myasnikov`), who gives access on request; its credentials are never stored in this repository. Sign-in is always manual: the terms of use, the email, then a one-time code. When the editor is signed in, the run links the `TestMap` publication, builds the map after linking, shows only that publication in the list and shows the account name as `MapAuthor`. If the account has no publication titled `TestMap`, a run that captures `publish-modio` fails.

Without access to the account, leave out `publish-modio` and `window-overview`: pass the other ids with `-docsCaptureOnly` and `--only`.

## When to re-capture

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

## Fix a badge on the wrong element

When a badge lands on the wrong element, run `annotate.py` with `--debug`. It writes `<id>.debug.png` next to the raw shot, with the rectangle of every UI element in the layout file drawn. Then adjust the callout in `shots.json`:

- **MapBuilder shots.** Their callouts follow the UI: `shots.json` anchors them to elements by type, class, binding path or label text, with the text per language. An anchor can be a list of alternatives; `annotate.py` uses the first one that matches an element, as for the **Sign out** and **Sign in** buttons of `window-overview`.
- **Shared shots.** The inspectors, the **Map Validation** window and the Scene view are drawn mostly with IMGUI, which has no element rectangles, so their crops and callouts are fixed `px` rectangles: `[x, y, width, height]` in points from the top-left corner of the raw shot, tab strip included. To measure one, read the pixel position in the raw PNG or the debug image and divide it by the `ppp` value in the shot's JSON file, for example `1.75` at 175%. Check these rectangles whenever that UI changes.
- **Badge position.** A callout's `place` sets where its badge sits: `left` (the default, 6 px left of the element and vertically centred), `right`, `top-right`, `top-left`, `above`, `above-left`, `below`, `below-left` or `center`. `dx` and `dy` shift the badge further, in pixels at the display width. When a `left` badge has no room in the crop, `annotate.py` moves it to the element's top-right corner, unless the callout sets `force`.
