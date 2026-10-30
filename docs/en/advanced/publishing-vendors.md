# Publishing vendors (for integrators)

**English** · [Русский](../../ru/advanced/publishing-vendors.md)

This page explains how MapUploader publishes through vendor SDKs: how the publishing layer is built, how the SDKs and the vendor config assets are set up, how to point the project at another mod.io game and how to add a vendor. Read it if you maintain MapUploader or reuse its publishing layer in another project.

> [!NOTE]
> Map authors configure nothing on this page: the mod.io config for CarX Drift Racing Online 2 ships with the project in `Assets/Resources/CarX.Modding/ModIoConfig.asset`. To sign in and publish a map, follow [Publish to mod.io](../publish.md). <!-- src: Assets/Resources/CarX.Modding/ModIoConfig.asset:15-34 -->

## Vendors in this project

MapUploader has two vendor implementations. Only mod.io is available in the **MapBuilder** window. The Steam Workshop implementation compiles, but the window never shows it.

| Vendor | Vendor id | SDK folder | Define | Config asset | In MapBuilder |
| --- | --- | --- | --- | --- | --- |
| mod.io | `modio` | `Assets/Plugins/Modio` | `CARX_MODDING_MODIO` | `ModIoConfig`, filled in for CarX Drift Racing Online 2 (game id `14124`) | The only vendor. The **Platform** field in the header always shows mod.io and can't be changed. <!-- src: Assets/Editor/MapBuilderEditorWindow.Workspace.cs:142-146 --> |
| Steam Workshop | `steam` | `Assets/Plugins/Facepunch.Steamworks` | `CARX_MODDING_STEAM` | `SteamWorkshopConfig`; its only game entry has **Available** cleared | Not shown. <!-- src: Assets/Resources/CarX.Modding/SteamWorkshopConfig.asset:15-18 --> |

## How it fits together

MapUploader never calls a vendor SDK directly. It talks to one vendor-neutral contract in the [CarX.Modding.Creator](https://github.com/CarXTechnologies/CarX.Modding.Creator/tree/1f0b558aeffaa069344fb3e909f3fa5ddc1fff95) submodule. Each vendor implementation sits in its own assembly behind a define constraint, so a project compiles only the vendors whose SDKs it carries, and another project can add a vendor without changing the contract.

```
Assets/Plugins/CarX.Modding.Creator/          submodule
├── Publishing/
│   ├── Core/                                 CarX.Modding.Creator.Publishing: Editor only, always compiled
│   │   ├── IModPublisher, IModAuthProvider, IModAuthPrompt   what every vendor implements
│   │   ├── ModItem, ModItemKey, ModCreateRequest, ModUploadRequest
│   │   ├── ModVendorLimits, ModPublisherContext, ModUploadProgress
│   │   ├── ModPublisherAttribute, ModPublisherRegistry       find the vendors that got compiled
│   │   └── ModVendorConfig<T>, ModIoConfig, SteamWorkshopConfig   credential and limit assets
│   ├── ModIo/                                CarX.Modding.Creator.Publishing.ModIo: only with CARX_MODDING_MODIO
│   │   └── ModIoPublisher, ModIoAuthProvider, ModIoEditorBootstrap
│   └── Steam/                                CarX.Modding.Creator.Publishing.Steam: only with CARX_MODDING_STEAM
│       └── SteamWorkshopPublisher, SteamWorkshopAuthProvider
└── Editor/Publishing/                        part of CarX.Modding.Creator.Editor, always compiled
    ├── ModPublisherSession                   what a tool window talks to
    ├── ModPublishingDefines                  keeps the vendor defines in step with the SDKs
    └── EditorModAuthPrompt, ModAuthPromptWindow, ModAuthPreferences   sign-in dialogs, remembered email and terms

Assets/Plugins/Modio/                         mod.io SDK, committed to this repository
Assets/Plugins/Facepunch.Steamworks/          Steam SDK, committed to this repository
Assets/Editor/ModPublisherConfigSetup.cs      creates the config assets with the CarX values
Assets/Resources/CarX.Modding/                ModIoConfig.asset, SteamWorkshopConfig.asset
```

The parts work like this:

- **Contracts.** `Publishing/Core` is an Editor-only assembly with no define constraint. Its namespace is still `Plugins.CarX.Modding.Creator.Runtime.Publishing`. The config base class `ModVendorConfig<T>` lives here too, so removing a vendor SDK never turns a saved config asset into a missing script.
- **Vendors.** `ModIoPublisher` and `SteamWorkshopPublisher` implement `IModPublisher`, each with its own `IModAuthProvider`. Their assemblies compile only while their define is set.
- **Discovery.** `ModPublisherRegistry` finds vendors by reflection rather than by reference, because nothing may hard-link an assembly that might not exist. It scans the loaded assemblies for non-abstract `IModPublisher` types that carry `[ModPublisher]` and have a public constructor that takes a single `ModPublisherContext`. A marked type without that constructor is skipped with an error in the Console. Vendors are sorted by the attribute's `order`, then by display name: Steam Workshop has `0` and mod.io has `1`.
- **Session.** A tool window talks to `ModPublisherSession`, never to the registry. The session creates and initializes the selected vendor, pumps it on every editor update, runs the sign-in flow in modal editor dialogs and confirms a delete before it sends it. It stores the selected vendor id in the EditorPrefs key `CarX.Modding.Publisher.Vendor.<hash of the project's Assets path>`. With nothing stored, it picks the first vendor in the registry. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/Publishing/ModPublisherSession.cs:18,38 -->
- **Host.** MapBuilder creates one session, `MapBuilder.session`, with a `ModPublisherContext` that holds the tagging policy and the visibility of new entries: the content tag `map_2.0`, the legacy tag `Map` and private visibility. `Map` is the tag of maps published by earlier versions of the uploader; MapBuilder marks such entries `Old version!`. Whether a vendor actually sends the tags is up to the vendor; see [Known differences between vendors](#differences). <!-- src: Assets/Editor/MapBuilder.Publishing.cs:20-23 -->

### Vendor defines

`ModPublishingDefines` keeps three scripting defines in step with what is loaded in the editor:

| Define | Set when | What it turns on |
| --- | --- | --- |
| `CARX_MODDING_MODIO` | An assembly whose name starts with `Modio` is loaded | The `CarX.Modding.Creator.Publishing.ModIo` assembly |
| `CARX_MODDING_STEAM` | An assembly whose name starts with `Facepunch.Steamworks` is loaded | The `CarX.Modding.Creator.Publishing.Steam` assembly |
| `CARX_MODDING_MODIO_UPLOAD_PROGRESS` | The type `Modio.Mods.Builder.ModfileUploadProgress` exists. Only the patched mod.io plugin has it; see [Vendor SDKs](#sdks). | Real packing and upload progress for mod.io. Without it, the mod.io publisher still compiles against an unpatched plugin. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/Publishing/ModPublishingDefines.cs:22-40 --> |

The sync doesn't run on domain load. It runs in two cases:

- on the next editor update after the first use of `ModPublisherSession`, for example when you open the **MapBuilder** window;
- when you select **ModSystem** > **Sync Publishing Defines**.

The sync writes the defines for the selected build target group, logs each change to the Console and starts a recompile only when something changed. The project commits all three defines for `Standalone`, so a fresh clone compiles both vendors before any sync. <!-- src: ProjectSettings/ProjectSettings.asset:687 -->

A project that never publishes, such as the game itself, never runs the sync. It sets the defines it needs by hand.

<a name="mapbuilder"></a>
### What MapBuilder fixes to mod.io

The contract supports any number of vendors and games, but the **MapBuilder** window limits both:

- The window lists and selects only the vendor `modio`. Before every fetch of the publications list, it switches the session to `modio` if another vendor was stored.
- The window has no game picker. Its **Game** field, with Steam store artwork and an `(incomplete)` mark for entries without credentials, is built but never added to the window. So MapBuilder publishes to the game that the config has selected.
- **Build destination** offers the **Local install** option only for a vendor that supports local installs. mod.io doesn't, so the options are **Platform** and **Export to folder**.

These members fix the window to mod.io:

| File | Member | What it does |
| --- | --- | --- |
| `Assets/Editor/MapBuilderEditorWindow.cs` | `GetVendorDisplayNames`, `RefreshVendorBar` | Offer only the vendor `modio` in **Platform** and keep the field disabled |
| `Assets/Editor/MapBuilderEditorWindow.Items.cs` | `FetchItems` | Switches the session to `modio` before every fetch |
| `Assets/Editor/MapBuilderEditorWindow.Workspace.cs` | `BuildWorkspace` | Fills **Platform** with mod.io only, disables it, and leaves the **Game** field out of the window |
| `Assets/Editor/MapBuilderEditorWindow.Workspace.cs` | `PublishedVersionLabel` | Reads a map's link to a `modio` entry for the `mod.io: …` line of its row in **LOCAL MAPS** |
| `Assets/Editor/MapBuilder.Publishing.cs` | `CreateNewCommunityItem` | Reports `Connecting to mod.io…` and `Sending files / waiting for mod.io…` as progress steps |

<a name="sdks"></a>
## Vendor SDKs

Both SDKs are committed as plain files under `Assets/Plugins`. There is nothing to install and nothing to fetch with the submodule. The submodule holds the contracts and the vendor implementations; the SDKs they compile against belong to the project, so a project that ships one vendor carries only that vendor's binaries.

- **mod.io.** The mod.io Unity plugin is in `Assets/Plugins/Modio`. It isn't a UPM package: the folder has no `package.json`, and mod.io distributes the plugin through the Asset Store, as a `.unitypackage` on the GitHub Releases page, or as a GitHub archive that you unpack into `Assets/Plugins`. <!-- src: Assets/Plugins/Modio/README.md:75-78 --> So `Packages/manifest.json` can't reference it. The plugin needs Newtonsoft.Json, which the project lists as `com.unity.nuget.newtonsoft-json` 3.2.2. <!-- src: Packages/manifest.json:5 -->
- **Steam.** [Facepunch.Steamworks](https://wiki.facepunch.com/steamworks/) is in `Assets/Plugins/Facepunch.Steamworks`: the managed assemblies for Win64, Win32 and Posix, plus the native Steam libraries in `redistributable_bin`.

The mod.io plugin in this repository carries local changes. They are listed in [`Assets/Plugins/Modio/CARX_PATCHES.md`](../../../Assets/Plugins/Modio/CARX_PATCHES.md) and marked in the source with `CARX PATCH` comments:

- The modfile is uploaded as `<mod id>.zip` instead of `upload.zip`, so a published file names the mod it belongs to.
- `ModfileBuilder.SetUploadProgress` reports packing and upload progress. The define `CARX_MODDING_MODIO_UPLOAD_PROGRESS` depends on this patch.
- A multipart upload, used for files over 100 MiB, returns the result of its final add-modfile call. Without the patch, a modfile that mod.io refuses looks like a successful upload.

To update the mod.io plugin:

1. Replace everything in `Assets/Plugins/Modio` except `CARX_PATCHES.md` and its `.meta` file with a newer release from [modio/modio-unity](https://github.com/modio/modio-unity/releases).
   Only `Modio/` and `Unity/` are needed. `CARX_PATCHES.md` is the CarX list of local changes, and an upstream release doesn't contain it.
1. Optional: If you don't want the sample scenes and the runtime UI compiled into the project, delete `Unity/Examples` and `Unity/UI`.
1. Re-apply the patches listed in `CARX_PATCHES.md`.
   To list the patches that are present, search the folder for `CARX PATCH`, for example with `grep -rn "CARX PATCH" Assets/Plugins/Modio`.
1. In `CARX_PATCHES.md`, delete the patches that upstream has fixed.
1. Select **ModSystem** > **Sync Publishing Defines**.
1. Commit the result.

Result: The project compiles, and `CARX_MODDING_MODIO_UPLOAD_PROGRESS` is set only if the upload-progress patch is in place.

To update Facepunch.Steamworks:

1. Replace the contents of `Assets/Plugins/Facepunch.Steamworks` with a newer release.
1. Commit the result.

Result: The project compiles, and `CARX_MODDING_STEAM` stays set.

> [!WARNING]
> Replacing `Assets/Plugins/Modio` silently drops the CarX patches. If the upload-progress patch is missing while `CARX_MODDING_MODIO_UPLOAD_PROGRESS` is still set, the publishing code stops compiling.

To make it compile again, select **ModSystem** > **Sync Publishing Defines**. While the project has compile errors, Unity keeps the assemblies compiled before the update loaded, so the sync can still find the patched type and change nothing. In that case, remove the define by hand: in **Edit** > **Project Settings**, on the **Player** page, under **Other Settings** > **Script Compilation**, remove `CARX_MODDING_MODIO_UPLOAD_PROGRESS` from **Scripting Define Symbols** for `Standalone`.

The same applies when you remove a vendor's SDK folder: if its assembly then fails to compile, remove its define by hand. The vendor's config asset stays valid, because its class lives in the always-compiled core assembly.

<a name="configs"></a>
## Vendor configs

Each vendor reads its credentials and limits from a ScriptableObject under a `Resources` folder. The project keeps both assets in `Assets/Resources/CarX.Modding/`: <!-- src: Assets/Editor/ModPublisherConfigSetup.cs:18 -->

- `ModIoConfig.asset` holds CarX Drift Racing Online 2 on mod.io, with the game id, the API key and the endpoint filled in. Map authors use it as it is.
- `SteamWorkshopConfig.asset` holds the game's Steam app id, with **Available** cleared.

How the assets are found and created:

- A vendor loads the first asset of its type from any `Resources` folder. If there are several, it logs `Multiple <Type> assets found under Resources; using '<name>'.` Keep one asset of each type. <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/Core/ModVendorConfig.cs:36 -->
- After every script reload, including editor start, `ModPublisherConfigSetup` creates an asset in `Assets/Resources/CarX.Modding/` for each vendor that has none in any `Resources` folder. It seeds the game name, the Steam app id `1826420` and the mod.io profile URL, and leaves the mod.io game id and API key empty. It never overwrites an existing asset. This class is part of MapUploader, not of the submodule, so the submodule stays free of game-specific values.
- Deleting `ModIoConfig.asset` therefore doesn't remove the vendor: the next script reload creates an asset without the game id and API key, and MapBuilder shows `Unavailable`. Restore the committed file from git instead, for example with `git checkout -- Assets/Resources/CarX.Modding/ModIoConfig.asset`. To take a game out of use, clear its **Available** checkbox.
- **Tools** > **CarX Modding** > **Create publisher configs** runs the same check on demand. When both assets exist, it logs `All mod publisher configs already exist in 'Assets/Resources/CarX.Modding'.` <!-- src: Assets/Editor/ModPublisherConfigSetup.cs:84 -->
- You can also create an asset by hand with **Assets** > **Create** > **CarX** > **Modding** > **mod.io Config** or **Steam Workshop Config**.

Each config holds a list of games in **Games**. You author the list by hand, because the ids can't be discovered, and each vendor has its own list: a Steam app id and a mod.io game id are different things issued by different people, and mod.io also needs an API key for each game. Settings that apply to every game of a vendor sit outside the list.

### ModIoConfig fields

The labels are Unity's display names for the fields. The **Default** column shows the values committed with the project.

Each entry in **Games** has these fields:

| Option | Default | What it does | When to change |
| --- | --- | --- | --- |
| **Display Name** | `CarX Drift Racing Online 2` | Name of the game in the game picker, which MapBuilder doesn't show. | Only for another game. |
| **Game Id** | `14124` | The mod.io id of the game. Every API call goes to this game. | Only for another game. |
| **Api Key** | Filled in | The game's API key. The code treats it as a public read key: each author gets a separate write token from the email sign-in, so the asset can ship to map authors. | Only for another game. |
| **Server Url** | `https://g-14124.modapi.io/v1` | The API endpoint, which mod.io calls the API path. When empty, the publisher uses `https://g-<Game Id>.modapi.io/v1`. | Only when mod.io lists a different API path for the game, for example in its test environment. <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/Core/ModIoConfig.cs:186 --> |
| **Profile Url** | `https://mod.io/g/carx-dro2` | The game's public page. MapBuilder builds publication links from it (`<Profile Url>/m/<mod id>`) and shows it in `Not signed in to <Profile Url>.` | Only for another game. |
| **Available** | Selected | Clear it to take the game out of the list without deleting the entry and its credentials. | When you keep several games in the list. |

These settings apply to every game:

| Option | Default | What it does | When to change |
| --- | --- | --- | --- |
| **Default Language** | `en` | Language code sent to the API for localized strings. | Rarely. |
| **Apply Content Tags** | Cleared | Sends the tag `map_2.0` on create and update, and lists only your mods tagged `map_2.0` or `Map`. When cleared, no tags are sent, and every mod that you created for the game is listed. | Only after the tags are registered in the game's admin panel on mod.io (`Admin -> Tags`). mod.io rejects tags that aren't registered there. |
| **Log Level** | `Warning` | How much the mod.io plugin logs: `None`, `Error`, `Warning`, `Message` or `Verbose`. | Set it to `Verbose` while you diagnose a response that fails to parse. |
| **Max Payload Size In Mb** | `4096` | Largest map allowed, measured on the `MapTemp` folder of the build cache when you create or update a publication. | To set a stricter project limit. |
| **Max Meta Size In Mb** | `24` | Largest metadata allowed, measured on the `MetaTemp` folder of the build cache when you create or update a publication. | To set a stricter project limit. |
| **Max Preview Size In Mb** | `8` | Largest image file allowed in **Icon · 16:9**. The icon becomes the mod.io logo, and mod.io rejects logos larger than 8 MB. | To set a stricter project limit. |
| **Max Title Length** | `50` | Longest **Name** allowed. **Validate map** reports a longer name as an error, and the publisher cuts it to this length. | To set a stricter project limit. |
| **Max Summary Length** | `250` | Longest summary allowed. mod.io requires a summary and caps it at 250 characters. | To set a stricter project limit. |
| **Max Description Length** | `50000` | Longest **Description** allowed. | To set a stricter project limit. <!-- src: Assets/Resources/CarX.Modding/ModIoConfig.asset:25-30 --> |

The limits are editable because they are project policy as well as vendor rules. **Validate map** checks **Name**, the summary, **Description** and the size of the icon file against the limits of the active vendor. The map and metadata sizes are checked only when you select **Create publication** or **Update publication**; see [Size limits at upload](../publish.md#size-limits). When no vendor can be created at all, validation falls back to built-in limits: 128 characters for **Name**, 8000 for the summary and **Description**, and 1 MB for the icon. <!-- src: Assets/Editor/MapBuilder.cs:41-46 -->

How changes to the asset apply:

- You don't need a script reload. Before every operation, the publisher compares **Game Id**, **Api Key**, the endpoint, **Default Language** and **Log Level** with the values that the mod.io client started with, and restarts the client when they differ.
- When the endpoint changes while the editor keeps the same script domain, the publisher also signs you out, because a token issued by one endpoint fails on another. The endpoint changes with **Server Url**, or with **Game Id** while **Server Url** is empty. Sign in again.
- Outside Play mode, the mod.io plugin doesn't wire up its own services. `ModIoEditorBootstrap` does that wiring for the editor and hands the values from `ModIoConfig` to the plugin, so MapBuilder works without Play mode and without the plugin's own settings asset. The project has no such asset; **Tools** > **mod.io** > **Edit Settings** would create one in `Assets/Resources/mod.io/`, and MapBuilder doesn't read it.

`ModIoConfig` refuses a **Server Url** on the deprecated shared domain `https://api.mod.io/v1`. That domain still resolves and even signs users in, but every game-scoped call returns `REQUESTED_RESOURCE_NOT_FOUND`, which reads like a missing mod. With that value, the header shows `Unavailable`, and the **Publish** tab says to clear the field: `Server Url on 'ModIoConfig' points at the deprecated https://api.mod.io/v1 domain. Clear the field to use https://g-<id>.modapi.io/v1.` <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/Core/ModIoConfig.cs:285-286 -->

### SteamWorkshopConfig fields

Each entry in **Games** has these fields:

| Option | Default | What it does | When to change |
| --- | --- | --- | --- |
| **Display Name** | `CarX Drift Racing Online 2` | Name of the game in the game picker, which MapBuilder doesn't show. | Only for another game. |
| **App Id** | `1826420` | The Steam app id of the game. | Only for another game. |
| **Available** | Cleared | Takes the game out of the list without deleting the entry. | Only once MapBuilder supports Steam Workshop. |

These settings apply to every game:

| Option | Default | What it does | When to change |
| --- | --- | --- | --- |
| **Local Mods Folder** | `Mods` | Folder inside the game install that the **Local install** destination copies a build to: `<game install folder>/<Local Mods Folder>/<item id>`. | If the game reads mods from another folder. |
| **Max Payload Size In Mb** | `4096` | Largest map allowed. | To set a stricter project limit. |
| **Max Meta Size In Mb** | `24` | Largest metadata allowed. | To set a stricter project limit. |
| **Max Preview Size In Mb** | `1` | Largest image file allowed in **Icon · 16:9**. Steam rejects previews larger than 1 MB. | To set a stricter project limit. |
| **Max Title Length** | `128` | Longest name allowed. | To set a stricter project limit. |
| **Max Description Length** | `8000` | Longest description allowed. Steam has no separate summary, so this also limits the summary. | To set a stricter project limit. <!-- src: Assets/Resources/CarX.Modding/SteamWorkshopConfig.asset:21-26 --> |

<a name="another-game"></a>
## Configure another mod.io game

Do this only when you reuse MapUploader for a game other than CarX Drift Racing Online 2.

### Before you start

- A mod.io account that can see the game on its API access page, `https://mod.io/me/access`.

### Point the project at the game

To point the project at another mod.io game:

1. In the **Project** window, select `Assets/Resources/CarX.Modding/ModIoConfig.asset`.
   The **Inspector** shows its fields.
1. In **Games**, add an entry for your game.
   Unity copies the last entry's values into it. The next steps overwrite each of them.
1. In the new entry, in **Display Name**, enter the name of the game.
1. In **Game Id** and **Api Key**, enter the values from your game's row on the API access page of your mod.io account, `https://mod.io/me/access`. <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/Core/ModIoConfig.cs:271,277 -->
1. Delete any value in **Server Url**, so that the field is empty.
   The publisher then uses `https://g-<Game Id>.modapi.io/v1`.
1. In **Profile Url**, enter the game's public page, for example `https://mod.io/g/<game>`.
1. On your game's entry, select **Available**.
1. On every other entry, clear **Available**.
   MapBuilder has no game picker, so keep exactly one entry available.
1. If the **MapBuilder** window is open, close it.
1. Select **Tools** > **MapBuilder**.
   The window connects to mod.io with the new settings.

Result: The header no longer shows `Unavailable`. While you are signed out, the **Publish** tab shows `Not signed in to <Profile Url>.` If a value is missing, the header shows `Unavailable`, and the box at the top of the **Publish** tab names the missing value, for example `The selected game on 'ModIoConfig' has no api key. Copy it from https://mod.io/me/access.` <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/Core/ModIoConfig.cs:277; Assets/Plugins/CarX.Modding.Creator/Publishing/ModIo/ModIoAuthProvider.cs:154 -->

> [!WARNING]
> Copy **Game Id** and **Api Key** from the same row. An account with access to several games lists them all, and a wrong **Game Id** sends maps to another game.

Add a new entry rather than editing the CarX Drift Racing Online 2 entry in place. The committed asset still stores the game id and API key in hidden fields from before the asset kept a list of games. On the first read of the config in an editor session, `ModIoConfig` moves them into **Games**. If no entry has the game id `14124` at that moment, it inserts one with the CarX Drift Racing Online 2 API key and selects it, so MapBuilder publishes to CarX Drift Racing Online 2 without any error. <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/Core/ModIoConfig.cs:205-234; Assets/Resources/CarX.Modding/ModIoConfig.asset:31-34 -->

The field tooltips call the source page account settings > Access > API access. The Console message that appears after the project creates a missing config points to the game's admin panel instead: `Admin -> API`. <!-- src: Assets/Editor/ModPublisherConfigSetup.cs:79-80 -->

While a game is hidden on mod.io, only members of its team can reach it. Map authors outside the team can't sign in and publish until the game is public or they are added to the team.

The integration doesn't use mod.io OAuth applications or personal access tokens. Authors sign in with the email code flow, which gives each of them a write token. Never ship a client secret in a project that you hand to map authors.

<a name="add-vendor"></a>
## Add a vendor

A new vendor needs code in the submodule and, to appear in the **MapBuilder** window, changes in MapUploader.

To add a vendor:

1. In the submodule, create the folder `Publishing/<Vendor>/`.
1. In that folder, create an assembly definition for the Editor platform only, with a define constraint of your own, for example `CARX_MODDING_<VENDOR>`. Reference `CarX.Modding.Creator`, `CarX.Modding.Creator.Publishing` and, if the SDK has its own assembly definitions, the SDK.
   `Publishing/ModIo/CarX.Modding.Creator.Publishing.ModIo.asmdef` is a working example.
1. Implement `IModPublisher` and `IModAuthProvider`.
   Follow the contract in the interface comments. `InitializeAsync` runs before every operation, so keep it cheap and use it to notice config changes. `CreateItemAsync` must not leave an entry behind when its content can't be published. `DeleteItemAsync` must work from a key alone, so that an entry that can't be listed can still be deleted.
1. Mark the publisher with `[ModPublisher("<vendor id>", "<Display Name>", <order>)]`.
   Every publication link stores the vendor id in a `ModItemKey`, so don't change the id once maps are linked.
1. Give the publisher a public constructor that takes a single `ModPublisherContext`.
   Without it, the registry skips the publisher and logs an error in the Console.
1. Optional: If the vendor needs credentials, add a subclass of `ModVendorConfig<T>` in `Publishing/Core`.
   Keep it out of the vendor folder: the core assembly always compiles, so a saved asset survives when the SDK is removed. To create the asset after a script reload, extend `Assets/Editor/ModPublisherConfigSetup.cs` in MapUploader.
1. In `Editor/Publishing/ModPublishingDefines.cs`, add your define and the prefix of the SDK's assembly names to the private array `s_vendorSdks`.
1. Select **ModSystem** > **Sync Publishing Defines**.
   The Console shows `[CarX.Modding] Detected the SDK for <define>; enabling that publisher.`, and Unity recompiles. <!-- src: Assets/Plugins/CarX.Modding.Creator/Editor/Publishing/ModPublishingDefines.cs:100 -->
1. In MapUploader, change the members that fix the window to mod.io. See [What MapBuilder fixes to mod.io](#mapbuilder).

Result: After the recompile, `ModPublisherSession.AvailableVendors` lists the new vendor, and the **MapBuilder** window offers it as far as your changes to those members allow.

The interfaces are in the submodule: [`IModPublisher.cs`](https://github.com/CarXTechnologies/CarX.Modding.Creator/blob/1f0b558aeffaa069344fb3e909f3fa5ddc1fff95/Publishing/Core/IModPublisher.cs) and [`IModAuthProvider.cs`](https://github.com/CarXTechnologies/CarX.Modding.Creator/blob/1f0b558aeffaa069344fb3e909f3fa5ddc1fff95/Publishing/Core/IModAuthProvider.cs).

<a name="differences"></a>
## Known differences between vendors

The two vendors disagree in the places listed in the following table. The shared contract absorbs most of it through `ModVendorLimits`, `ModItem.StatusLabel` and the auth provider.

| Topic | Steam Workshop | mod.io |
| --- | --- | --- |
| Sign-in | The session of the running Steam client. MapBuilder shows no **Sign in** button; you sign out in the Steam client. | An email one-time code in the editor: the terms of use, then the email, then a 5-character code. The plugin caches the token on disk. See [Sign in to mod.io](../publish.md#sign-in). |
| Switching games | Needs an editor restart: the Steam client keeps the app id it started with. | The client restarts with the new game's settings. |
| Creating an entry | Steam would accept a blank item, but the publisher always sends the build with it. | mod.io refuses an entry without a name, a summary and a logo. The publisher checks these and the build folder before it calls mod.io. |
| Name | Up to 128 characters. | Up to 50 characters; the publisher cuts longer names. |
| Summary | No separate field. On create, the summary is sent as the item description. | Required, up to 250 characters. MapBuilder sends the first line of **Summary (optional)**, or else the first line of **Description**, or else `<Name> track.` <!-- src: Assets/Scripts/Rules/MapMetaRules.cs:47-60 --> |
| Description | Sent on update when **Update description** is selected. | Sent on create when **Description** isn't empty, cut to **Max Description Length**. Sent on update when **Update description** is selected, together with the summary. <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/ModIo/ModIoPublisher.cs:191-194 --> |
| Image | The image in **Icon · 16:9** as an optional preview, up to 1 MB. | The image in **Icon · 16:9** as the required logo, up to 8 MB. An update sends it only when **Update preview** is selected. |
| Version | Not supported; the publisher ignores it. | Stored on every uploaded file. An empty **Version** sends `1.0.0`. <!-- src: Assets/Scripts/Rules/MapMetaRules.cs:62-70 --> |
| Changelog | Stored as the change note of each upload. | Stored on every uploaded file. |
| Visibility of new entries | Private. | Hidden. |
| Visibility on update | Never changed. | Never changed. |
| Status in the publications list | `Public`, `Friends only`, `Private`, `Banned` or `Awaiting agreement`. | Only `File uploaded` or `No file uploaded`. See [mod.io doesn't report a mod's status](#modio-status). |
| Local install | Yes: `<game install folder>/<Local Mods Folder>/<item id>`, which adds the **Local install** option under **Build destination**. | No. |
| Tags | Always sends `map_2.0`, and lists items tagged `map_2.0` or `Map`. | Only with **Apply Content Tags** selected; the tags must be registered on mod.io first. |
| Deleting an entry | `SteamUGC.DeleteFileAsync`. Only the item's owner can delete it. | `DELETE /games/{game-id}/mods/{mod-id}`, through the plugin's `ArchiveMod`. MapBuilder's confirmation calls the delete permanent; the plugin's own documentation says that the call archives the mod and that only the mod.io website deletes a mod permanently. Either way, the mod drops out of the list. <!-- src: Assets/Plugins/Modio/Modio/Mods/Builder/ModBuilder.cs:757-761 --> |
| Where the entry id is | In the item URL, after `?id=`. | On the mod's own page on mod.io. The `Item id` prompt of **Delete publication** says where. |

<a name="modio-status"></a>
### mod.io doesn't report a mod's status

The mod.io website shows a review status (`Pending` or `Accepted`) and a visibility flag for each mod. Both travel in the API response, but the Unity plugin drops them when it builds its `Mod` object. The raw request builder (`ModioAPIRequest.New`, `ModioAPIRequestOptions.AddQueryParameter`) is `internal`, so an external assembly has no supported way to read either.

So the publications list shows only what can be established honestly: whether a file has been uploaded. The tooltip of the status text links to the mod page, where the real status lives: `Full status and visibility: <Profile Url>/m/<mod id>`. If the plugin later exposes these values on `Mod`, change only `ModIoPublisher.DescribeStatus`. <!-- src: Assets/Editor/MapBuilderEditorWindow.Items.cs:247 -->

### `INVALID_JSON` usually means something else

`ModioAPIUnityClient` turns any response that it can't parse into `ErrorCode.INVALID_JSON`. The message that comes with it, `You have used the input_json parameter with semantically incorrect JSON.`, describes a request problem that is almost never the cause. In MapBuilder, it typically shows in the Console as `Could not read your mod.io creations: You have used the input_json parameter with semantically incorrect JSON.` <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/ModIo/ModIoPublisher.cs:125-126; Assets/Plugins/Modio/Modio/Errors/Generated/ErrorCode.cs:499 -->

Before you look for a cause, read the lines that the Console or the Editor log shows right before it:

- `HTTP Code: [502] Unexpected error from server instead of JSON: <html>…` means that the server answered with an error page, and nothing in the request is wrong. If every call about your account fails this way at once, suspect the session: a token issued by another endpoint, such as the deprecated `api.mod.io`, signs in but fails on the calls that matter. Sign out and sign in again. <!-- src: Assets/Plugins/Modio/Unity/ModioApiUnityClient.cs:469 -->
- If nothing is logged right before it, the response failed to deserialize. The plugin logs that exception only at the `Verbose` level, so set **Log Level** on `ModIoConfig` to `Verbose` and repeat the action. One known case is described in the next section.

### A mod.io mod without a file breaks the whole list

In the plugin, `ModObject.Modfile` is a non-nullable `ModfileObject` struct, while the API sends `"modfile": null` for a mod that has no file yet. Newtonsoft.Json can't bind null to a non-nullable struct, so the whole page of results fails to deserialize. One mod without a file makes every mod of that user unreadable, including the healthy ones. The failure surfaces as `INVALID_JSON`, and the real exception is logged only at `Verbose`.

MapUploader avoids creating such a mod:

- **Create publication** stays disabled until both the map and its metadata are built, because the publisher attaches the build while it creates the entry instead of creating an empty entry first.
- If mod.io accepts the new mod but refuses its file, the publisher deletes the new mod and reports `mod.io created mod <id> but refused its content: <reason>. The incomplete mod was deleted.` If that delete also fails, the message asks you to delete the mod on mod.io. <!-- src: Assets/Plugins/CarX.Modding.Creator/Publishing/ModIo/ModIoPublisher.cs:520 -->

A mod without a file can still come from elsewhere, for example from the mod.io website. If the list fails to load, delete that mod by its id: **Delete publication** asks for an id when no publication is selected, and that path doesn't need the list. See [Delete a publication that the list can't show](../publish.md#delete-by-id).

### The publications list is read with `SyncUserCreations`

`User.GetUserCreations` reads through `ModCache`, and nothing clears that cache when a mod is created. An empty result fetched before your first mod existed would be replayed for the rest of the editor session.

So `ModIoPublisher` calls `User.SyncUserCreations()`, which goes to the API, refreshes `User.ModRepository`, and reads the list from there. The request is limited to the configured game and leaves out deleted mods. `User.Sync()` would also clear the cache, but it also fetches collections, the wallet and entitlements, which this tool doesn't use and which log errors whenever their endpoints fail. <!-- src: Assets/Plugins/Modio/Modio/Users/User.cs:662-668 -->

## Next steps

← [Mod format: Wavefront and Binary](mod-format.md) · [Contents](../README.md)
