# Screenshot capture

Maintainer tooling that produces every image in `docs/images/`. Map authors never need it: the folder is outside
`Assets/`, so Unity does not compile it in their project.

| File | What it does |
| --- | --- |
| `Editor/DocsCapture.cs`, `Editor/MapUploader.DocsCapture.asmdef` | Editor script. Opens the windows in the documented states on the `TestMap` sample and saves a raw PNG of each window plus a JSON file with the rectangles of every visible UI element. It never saves a scene and restores every asset, EditorPrefs key and gizmo setting it changes. |
| `shots.json` | Capture spec: crop, displayed width and numbered callouts for each image. |
| `annotate.py` | Crops, scales, numbers and compresses the raw shots into `docs/images/{en,ru,shared}/`. Needs Python 3 with Pillow. |

## Before you start

- Windows display scale and Unity editor skin as you want them in the images (current images: Dark skin, 175 % scale; `annotate.py` scales everything to the displayed width).
- A clean checkout of the branch you document, opened once in Unity 6000.3.19f1 so the first import is done.
- Optional: a signed-in mod.io session in that editor. The account name is replaced with `MapAuthor` in every shot, and the publication list is reduced to the `TestMap` publication. Signed out, the Publish shot shows the signed-out state.
- Close every Unity editor that has this project open.

## Capture

1. Copy `docs/_capture/Editor` into `Assets/Editor/DocsCapture`.
1. Run Unity from the command line (one command, shown on several lines):

   ```bat
   Unity.exe -projectPath <project> -logFile capture.log ^
     -executeMethod MapUploader.DocsCapture.DocsCapture.Run ^
     -docsCaptureOut <raw folder> -docsCaptureExit
   ```

   The run takes about 10 minutes, because it builds `TestMap` once for each interface language. Add
   `-docsCaptureOnly window-overview,minimap-component` to capture only some images. Inside a running editor, use
   **Tools > Docs > Capture screenshots** instead; the raw shots then go to `Library/DocsCapture`.
1. Check `<raw folder>/capture.log`. It ends with `DocsCapture done`. `<raw folder>/unity-menus.txt` lists the Unity menu paths, so you can check the menu paths in the docs.
1. Run `python docs/_capture/annotate.py <raw folder>`. Add `--debug` to also write `<id>.debug.png` next to each raw shot, with every element rectangle drawn; use it when a callout lands on the wrong element.
1. Delete `Assets/Editor/DocsCapture` and `Assets/Editor/DocsCapture.meta`, and discard the `.meta` files that the `TestMap` builds re-serialized (`git checkout -- Assets ProjectSettings`).

## Callouts

Callouts are anchored to UI elements by type, class, binding path or label text (per language), so they follow layout
changes in the MapBuilder window. Inspector drawers and the Map Validation window are IMGUI: their anchors are pixel
rectangles in `shots.json` and must be checked whenever those drawers change. The numbers must match the `#` column of
the table under each image in both languages.

## Known limits

- Popup menus, dropdown lists, OS file dialogs and modal sign-in prompts can't be captured; describe them in text.
- The template shots use a template config and a Road marker created in memory, so the images don't depend on what
  the shipped template config or the TestMap markers happen to contain.
