# Get started

**English** · [Русский](../ru/getting-started.md)

Applies to: MapUploader `feature/2.0/3.0` · Unity 6000.3.19f1 · screenshots 2026-10-26

Get the MapUploader project, open it in the required Unity version and learn your way around the **MapBuilder** window. You do this once on each computer, before you start your first map.

## Before you start

- Windows 10 or 11, 64-bit. The build optimizer ships only for the Windows x64 editor, and **Optimize build** is on by default. On another system, clear **Optimize build** for each map under [Optimization](build.md#optimization), or the build stops with an error. <!-- src: Assets/Plugins/CarX.Modding.Creator/Runtime/BuildOptimizationSettings.cs:14 -->
- [Git](https://git-scm.com/downloads), to clone the project with its submodule.
- [Unity Hub](https://unity.com/download), to install the editor and open the project.
- A Unity account, signed in to Unity Hub, with an active license, such as Unity Personal. The editor doesn't start without a license.
- Unity Editor 6000.3.19f1, revision `7689f4515d75`, exactly. See [Install Unity 6000.3.19f1](#unity). <!-- src: ProjectSettings/ProjectVersion.txt:1-2 -->
- A PC copy of CarX Drift Racing Online 2 in a game build with mod support, to test your map locally. The game reads local maps from the `Mods` folder next to its executable. <!-- src: DR3PC1:Assets/Modules/CarX.Modding/Runtime/Providers/DefaultCollectionProvider.cs:28 -->
- A mod.io account, only when you publish. See [Publish to mod.io](publish.md).

<a name="clone"></a>
## Get the project

The project includes a git submodule, `Assets/Plugins/CarX.Modding.Creator`, so clone it with git.

To get the project:

1. In the folder where you keep your Unity projects, open a terminal, such as Git Bash or PowerShell.
1. Clone the repository with its submodule:

   ```bash
   git clone --recurse-submodules -b feature/2.0/3.0 https://github.com/CarXTechnologies/dro-map-uploader
   ```

   The `-b feature/2.0/3.0` option checks out `feature/2.0/3.0`, the branch these docs describe.

Result: the new `dro-map-uploader` folder holds the project, and its `Assets/Plugins/CarX.Modding.Creator` folder isn't empty.

> [!WARNING]
> **Code** > **Download ZIP** on GitHub doesn't include the submodule. `Assets/Plugins/CarX.Modding.Creator` stays empty, and the project doesn't compile.

<details>
<summary>The submodule fails with Permission denied (publickey) or Host key verification failed</summary>

The project registers its submodule with an SSH address, `git@github.com:CarXTechnologies/CarX.Modding.Creator.git`. Without a GitHub SSH key, the clone downloads the main project but stops at the submodule with `Permission denied (publickey)`. If this computer has never connected to GitHub over SSH and you don't confirm the `github.com` host when SSH asks, the clone stops with `Host key verification failed.` instead. <!-- src: .gitmodules:3 -->

To fetch the submodule over HTTPS:

- In a terminal, in the `dro-map-uploader` folder, run this command:

  ```bash
  git -c url."https://github.com/".insteadOf=git@github.com: submodule update --init --recursive
  ```

  The `-c` option applies to this one command and doesn't change your git configuration.

Result: `Assets/Plugins/CarX.Modding.Creator` holds the submodule files.

Later git commands that fetch the submodule, such as `git pull --recurse-submodules`, need the same `-c url."https://github.com/".insteadOf=git@github.com:` option.

</details>

<a name="unity"></a>
## Install Unity 6000.3.19f1

The project is pinned to Unity 6000.3.19f1. MapUploader never builds a game player, so you need no platform modules.

To install the editor:

1. Open the [Unity 6000.3.19f1 release page](https://unity.com/releases/editor/whats-new/6000.3.19f1) and select **Install**.
   Unity Hub opens the installation for 6000.3.19f1.
1. In Unity Hub, finish the installation without adding any modules.

Result: Unity Hub lists 6000.3.19f1 on its **Installs** page.

<details>
<summary>Install the editor with the offline installer</summary>

To use the offline installer:

1. Download the [Windows installer for Unity 6000.3.19f1](https://download.unity3d.com/download_unity/7689f4515d75/Windows64EditorInstaller/UnitySetup64-6000.3.19f1.exe) and run it.
1. In Unity Hub, on **Installs**, select **Locate**, and choose `Unity.exe` in the `Editor` folder of the new installation.

Result: Unity Hub lists 6000.3.19f1 on its **Installs** page.

</details>

> [!CAUTION]
> Opening the project in any other Unity version upgrades it in place, and the upgrade is hard to undo. If Unity Hub or the editor offers to change or upgrade the editor version, cancel.

## Open the project

To open the project:

1. In Unity Hub, on **Projects**, select **Add** > **Add project from disk**.
1. Select the `dro-map-uploader` folder, the one that contains the `Assets`, `Packages` and `ProjectSettings` folders, then select **Select Folder**.
   The project appears in the list with the editor version 6000.3.19f1.
1. Select the project in the list.
   Unity opens it. The first import takes a while, because Unity imports the whole HDRP project and creates the `Library` folder.

Result: the Unity menu bar has a **Tools** menu with a **MapBuilder** item. If Unity reports compile errors or offers to enter Safe Mode instead, the `Assets/Plugins/CarX.Modding.Creator` folder is probably empty; fetch the submodule as described in [Update an existing clone](#update-clone).

> [!NOTE]
> Once you work in the project, `git status` lists `ProjectSettings/EditorBuildSettings.asset` and `Assets/MapResources/TestMap/TestMapConfig.asset` as changed, and after you build `TestMap`, also many `.meta` files under `Assets/MapResources/TestMap/Art/`. MapUploader rewrites these files in the editor, so this is expected. <!-- src: Assets/Editor/MapBuilderProcessor.cs:79; Assets/Editor/MapMetaConfigIdStamper.cs:64-65; Assets/Plugins/CarX.Modding.Creator/Editor/ExportImporterSettings.cs:33,69 -->

<a name="window"></a>
## Open MapBuilder

**MapBuilder** is the window where you fill in the map settings, validate and build the map, export it to a folder and publish it to mod.io.

To open MapBuilder:

- In the Unity menu bar, select **Tools** > **MapBuilder**.

Result: the **MapBuilder** window opens with the map you selected last. The first time, that is `TestMap`, the sample map in `Assets/MapResources/TestMap/`.

The screenshot shows the window with `TestMap` selected on the **Map** tab, before any build.

<img src="../images/en/window-overview.png" width="1000" alt="Screenshot of the MapBuilder window with the TestMap sample selected; markers 1–7 on the main areas.">

| # | Element | What it does |
| --- | --- | --- |
| 1 | Account state, **Sign in** / **Sign out** | Shows your mod.io state: `Not signed in`, `Signing in…`, `Account: <name>` with **Sign out**, or `Unavailable` while mod.io starts or when it can't start. You need to sign in only to publish. See [Sign in to mod.io](publish.md#sign-in). <!-- src: Assets/Editor/MapBuilderEditorWindow.cs:537-543 --> |
| 2 | **Platform** field in the header | Shows the publishing platform. It is always mod.io and can't be changed. |
| 3 | Language dropdown | Switches the window between **EN** and **RU**. See [Choose the interface language](#language). |
| 4 | **LOCAL MAPS** | Lists every map config in the project. The search box filters the list by name, and **+ New map** creates a map config. See [Create or select a map](map-settings.md#new-map). |
| 5 | Map row | Shows the map's icon, name, build status (`Not built`, `Rebuild needed` or `Ready to export`) and mod.io line. `mod.io: not linked` means that no publication is linked to the map yet. <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:198 --> |
| 6 | **Map**, **Build** and **Publish** tabs | **Map** holds the map settings: name, texts and images. **Build** validates and builds the map. **Publish** exports the build to a folder or publishes it to mod.io. |
| 7 | Footer | Shows the status line, for example `Local map: TestMap`. While a build or an upload runs, it shows a progress bar and **Cancel**. <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:220 --> |

You can dock the window like any Unity window. Its minimum size is 840 × 580 px. When it is narrower than 1120 px, it switches to a compact layout: the header caption is hidden, the description and images columns on the **Map** tab wrap when they don't fit side by side, and the two columns of the **Publish** tab stack. <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:60,64; Assets/Editor/MapBuilderEditorWindow.uss:381,395,436 -->

You don't need to sign in to validate, build or export a map. Sign in only to publish to mod.io.

<a name="language"></a>
## Choose the interface language

MapBuilder is available in English and Russian. English is the default.

To change the language:

- In the **MapBuilder** header, in the language dropdown (tooltip `Interface language`), select **EN** or **RU**.

Result: the window redraws in the selected language and keeps the selected map. MapBuilder remembers the choice for your user account on this computer, in the EditorPrefs key `CarX.MapBuilder.Language` (default `en`). <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:14,58 -->

The dropdown is unavailable while a build or an upload runs.

With **RU** selected, this text stays in English:

- the Unity Editor itself;
- component inspectors, such as GameMarkerData, Minimap and CaptureCamera, and the `Save Capture` file dialog;
- the **Map Validation** window, and all validation and Console messages;
- the lines under **Scene contents** on the **Build** tab; <!-- src: Assets/Scripts/ValidItemData.cs:95-98 -->
- most tooltips;
- the size, status, `Old version!` and `Blank <n>` text of publication rows on the **Publish** tab; <!-- src: Assets/Editor/MapBuilderEditorWindow.Items.cs:199-222 -->
- the `Not signed in to https://mod.io/g/carx-dro2.` box on the **Publish** tab; <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/ModIo/ModIoAuthProvider.cs:154 -->
- the `External path` folder dialog; <!-- src: Assets/Editor/MapBuilderEditorWindow.Publish.cs:113 -->
- the mod.io sign-in and delete dialogs.

<a name="update-clone"></a>
## Update an existing clone

Update your clone to get new MapUploader features and fixes, or when you cloned it without `--recurse-submodules`.

If you have no SSH key for GitHub, put `-c url."https://github.com/".insteadOf=git@github.com:` after `git` in both the `git pull` and the `git submodule update` command, as in [Get the project](#clone).

To update the project:

1. So that Unity doesn't import files while git changes them, close the Unity Editor.
1. In a terminal, in the `dro-map-uploader` folder, pull the changes:

   ```bash
   git pull --recurse-submodules
   ```

1. Switch to the branch these docs describe:

   ```bash
   git switch feature/2.0/3.0
   ```

   A clone made without the `-b feature/2.0/3.0` option is on the repository's default branch, `target/2.0`, which uses Unity 2023.2.20f1 and an older MapBuilder. This command moves it to `feature/2.0/3.0`. On a clone that is already there, it prints `Already on 'feature/2.0/3.0'`.

1. Check out the submodule commit that the project expects:

   ```bash
   git submodule update --init --recursive
   ```

   If the submodule was never downloaded, this command downloads it.

Result: `git branch --show-current` prints `feature/2.0/3.0`, and `git submodule status` prints the submodule commit with no `-` or `+` prefix. When you open the project again, Unity imports the changes.

If `git pull` or `git switch` stops with `Your local changes to the following files would be overwritten`, restore the listed files that MapUploader rewrites, then run the command again:

```bash
git restore ProjectSettings/EditorBuildSettings.asset Assets/MapResources/TestMap
```

> [!CAUTION]
> The commands in *Start over from a clean copy* discard all local changes and delete every file git doesn't track: maps you haven't committed, and the git-ignored `Library/` and `UserSettings/` folders. The next open re-imports the whole project. <!-- src: .gitignore:1-2,43 -->

<details>
<summary>Start over from a clean copy</summary>

To reset the clone:

1. Close the Unity Editor.
1. To keep your maps, copy your map folders from `Assets/MapResources/`, and any other files you changed, to a folder outside `dro-map-uploader`.
1. In a terminal, in the `dro-map-uploader` folder, run these commands:

   ```bash
   git reset --hard
   git clean -xffd
   git submodule update --init --recursive --force
   ```

   `-x` also deletes ignored files, and the second `-f` also deletes untracked folders that are git repositories of their own.

Result: the clone matches the commit you last pulled, and the submodule matches the commit the project expects.

</details>

## Next steps

Create the folder and the scene for your map.

← [MapUploader README](../../README.md) · [Contents](README.md) · [Set up the scene](scene-setup.md) →
