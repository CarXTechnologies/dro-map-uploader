# Mod delivery vendors

The uploader does not talk to Steam or mod.io directly. Everything goes through one vendor agnostic contract that
lives in the **CarX.Modding.Creator** submodule, so a project can ship with only the vendors it actually needs — and
another project can add its own without touching the uploader.

- [How it fits together](#how-it-fits-together)
- [Installing the vendor SDKs](#installing-the-vendor-sdks)
- [Configuring a vendor](#configuring-a-vendor)
- [Signing in](#signing-in)
- [Publishing](#publishing)
- [Adding a new vendor](#adding-a-new-vendor)
- [Known differences between vendors](#known-differences-between-vendors)

## How it fits together

```
Assets/Plugins/CarX.Modding.Creator/          <- submodule
├── Runtime/Publishing/                       <- contracts, always compiled
│   ├── IModPublisher / IModAuthProvider      <- what every vendor implements
│   ├── ModItem / ModItemKey / ModUploadRequest
│   ├── ModVendorLimits / ModPublisherContext
│   ├── ModPublisherRegistry                  <- finds the vendors that got compiled
│   └── SteamWorkshopConfig / ModIoConfig     <- credential assets
├── Editor/Publishing/                        <- editor plumbing, always compiled
│   ├── ModPublisherSession                   <- what a tool window talks to
│   ├── ModPublishingDefines                  <- turns vendors on/off automatically
│   └── EditorModAuthPrompt                   <- modal dialogs for interactive sign in
└── Publishing/
    ├── Steam/                                <- compiled only with CARX_MODDING_STEAM
    └── ModIo/                                <- compiled only with CARX_MODDING_MODIO

Assets/Plugins/Facepunch.Steamworks/          <- Steam SDK, vendored in this repository
Assets/Plugins/Modio/                         <- mod.io SDK, vendored in this repository
```

Each vendor sits in its own assembly behind a define constraint. `ModPublishingDefines` sets those defines from
which SDK assemblies are present, so dropping a vendor SDK into the project is all it takes to enable it, and
removing one does not leave broken references behind.

`ModPublisherRegistry` discovers vendors by reflection over the `[ModPublisher]` attribute rather than by direct
reference, for the same reason: nothing may hard-link an assembly that might not exist.

## Installing the vendor SDKs

Both SDKs are vendored as plain files under `Assets/Plugins`, committed to this repository — nothing extra to
install, and nothing to fetch alongside the submodule. The submodule holds the contracts and the vendor
implementations; the SDKs they compile against belong to the project, so a project that ships only one vendor carries
only that vendor's binaries.

**Steam** uses [Facepunch.Steamworks](https://wiki.facepunch.com/steamworks/) at `Assets/Plugins/Facepunch.Steamworks`,
managed assemblies plus the native `redistributable_bin`. To update it, replace the folder contents with a newer
release and commit the result.

**mod.io** lives at `Assets/Plugins/Modio`.

> [!NOTE]
> The mod.io plugin is not a UPM package — its repository has no `package.json`, and its releases ship a
> `.unitypackage`. The repository itself is shaped like the contents of `Assets/Plugins`, which is why it cannot be
> referenced from `Packages/manifest.json`.

To update it, replace the folder contents with a newer release from
[modio/modio-unity](https://github.com/modio/modio-unity/releases) and commit the result. Only `Modio/` and `Unity/`
are needed; `Unity/Examples` and `Unity/UI` can be deleted if you do not want the sample scenes and runtime UI
compiled into the project.

> [!WARNING]
> The vendored SDK carries local modifications, listed in
> [`Assets/Plugins/Modio/CARX_PATCHES.md`](Assets/Plugins/Modio/CARX_PATCHES.md) and marked in the source with
> `CARX PATCH` comments. **An update silently drops them** — re-apply them afterwards, or delete the ones upstream
> has fixed. `grep -rn "CARX PATCH" Assets/Plugins/Modio` finds the full set.

The mod.io plugin needs **Newtonsoft.Json**; `com.unity.nuget.newtonsoft-json` is listed in
`Packages/manifest.json` for that reason.

Nothing needs to be switched on by hand after the SDK lands: `ModPublishingDefines` notices the `Modio` assemblies
and adds `CARX_MODDING_MODIO`, which brings the mod.io publisher into the build on the next recompile.

## Configuring a vendor

Each vendor reads its credentials from a `ScriptableObject` under a `Resources` folder. The project creates both on
first editor load; you can also recreate them from **Tools → CarX Modding → Create publisher configs**.

They land in `Assets/Resources/CarX.Modding/`:

Each config holds a **list of games** and the one currently picked. The list is authored by hand — the ids are not
discoverable — and it is **separate per vendor**, because a Steam app id and a mod.io game id are different things
issued by different people, and mod.io additionally needs an api key per game.

The picked game drives everything at once: the vendor's api calls *and* the uploader's Local Test folder, so the two
cannot drift apart. It is chosen from the **Game** dropdown at the top of the MapBuilder window, next to the vendor.

| Asset | Per-game field | Where it comes from |
| --- | --- | --- |
| `SteamWorkshopConfig` | Display Name | Whatever you want to see in the picker |
| `SteamWorkshopConfig` | App Id | Steam app id — the first entry is pre-filled with `635260` |
| `ModIoConfig` | Display Name | Whatever you want to see in the picker |
| `ModIoConfig` | **Game Id** | mod.io → account settings → **Access → API access**, on the row for this game |
| `ModIoConfig` | **API Key** | same row — reveal it with the eye icon |
| `ModIoConfig` | Server Url | leave empty — derived as `https://g-<gameId>.modapi.io/v1`. Set by hand only for the test environment |
| `ModIoConfig` | Profile Url | `https://mod.io/g/<game>` — used for the item links |

Steam entries show the game's store artwork next to the picker; mod.io has no image reachable without an api call,
so those entries show the name alone. An entry missing its credentials is still listed, marked `(incomplete)`, so it
can be selected and fixed rather than quietly disappearing.

Settings shared by every game on a vendor stay outside the list: `Apply Content Tags`, `Log Level`, the size limits,
and Steam's `Local Mods Folder`.

> [!IMPORTANT]
> **Game Id**, **API Key** and **Server Url** are the values that are not pre-filled. Copy all three off the API
> access page before the first upload; until then the MapBuilder window will say the vendor is not configured.
> Make sure you are reading the row for the right game — an account with access to several games lists them all, and
> a wrong Game Id uploads maps into the wrong title.

> [!WARNING]
> The shared `https://api.mod.io/v1` domain is **deprecated**. It still resolves and even signs users in, but every
> game scoped call comes back as `REQUESTED_RESOURCE_NOT_FOUND` — which reads like a missing mod rather than a wrong
> url. `ModIoConfig` refuses to initialize against it and tells you what to use instead.

> [!WARNING]
> While the game is **Hidden** on mod.io it is only reachable by team members or through a key scoped to it. That is
> why the per game API path matters, and why map authors outside the team will not be able to sign in and publish
> until the game is made public or they are added under **Team**.

Editing the config asset takes effect immediately: the publisher notices its credentials changed and restarts the
mod.io client on the next action, instead of quietly holding the url it was first initialized with.

The **OAuth applications** / **Personal access tokens** pages are not used by this integration. The Unity plugin
authenticates users with the email code flow, and a client secret must never be shipped inside a project that is
handed to map authors.

Both assets also carry the size and length limits the uploader validates a build against. They are editable because
they are project policy as much as a vendor rule.

## Signing in

Open **Tools → MapBuilder**. The bar at the top picks the vendor and shows the sign in state.

- **Steam Workshop** — there is no sign in button. The session belongs to the running Steam client: start Steam,
  sign in there, then reopen the window.
- **mod.io** — press **Sign in**. You are asked for your email address, shown the mod.io terms of use, and then
  asked for the five character code that was mailed to you. The token is cached by the mod.io plugin, so this is a
  one-off until it expires.

  Email was chosen over the Steam ticket flow deliberately: a Steam ticket requires the game to be launched through
  a running Steam client, which an editor tool never is.

## Publishing

The **Destination** section offers three targets:

| Destination | What it does |
| --- | --- |
| **Vendor** | Uploads the build to the selected item on the active vendor. |
| **Local Test** | Copies the build into the installed game's own mods folder, `<game install>/Mods/<item id>`, so it can be loaded without publishing. Steam only, because that is where the game install path comes from — the option is not offered for other vendors. |
| **External Folder** | Copies the build to any folder on disk. Works regardless of vendor. |

The **Upload Name / Description / Preview** toggles control which metadata fields are overwritten on the vendor page;
everything left off keeps whatever is already there.

**Version** and **Changelog** are written by the map author and travel with the uploaded file:

| Vendor | Version | Changelog |
| --- | --- | --- |
| mod.io | shown against the file on the mod page | shown to players as the release notes |
| Steam Workshop | no such field — the input is hidden | the update's change note, under Change Notes on the item |

Both are stored per map, not per published item, because they are needed before an entry exists — the file is
attached while the entry is created — and they have to survive a rebuild. Leaving **Version** empty falls back to
the uploader's own version, since a file with no version at all reads as a mistake on the mod page.

### Order of operations

The same on every vendor: **build → if it succeeded → create the entry and send the files**.

1. Assign a **Map Meta Config**.
2. **Build** Map and Meta. The meta stamps the map config's id as a placeholder, since no entry exists yet.
3. **New Item** — creates the entry *and* publishes that build to it in one step. Disabled until both targets are
   built, with the hint saying what is missing.
4. The panel then asks you to **rebuild Meta**, which now stamps the real vendor id, and **Upload**.

Building itself does not require an entry: pick a `MapMetaConfig` on the left and the Build section unlocks, with or
without a selection on the right. Only the Destination section needs an existing entry.

An entry is never created empty, even on vendors that would allow it. A blank entry is only ever half of an
operation, and nothing downstream can tell "created, upload pending" apart from "created, upload failed" — on mod.io
it is worse still, since a mod with no file cannot be read back at all.

## Deleting an item

**Delete…**, next to **New Item**, removes an entry from the vendor after a confirmation. It is permanent on both
vendors — Steam calls `DeleteFileAsync`, mod.io calls `DELETE /games/{id}/mods/{id}`.

With an item selected it deletes that one. **With nothing selected it asks for an id instead**, and that is not a
convenience: an entry can be impossible to select, because a single fileless mod makes the whole mod.io listing
unreadable. Deleting by id is the way out of that, and it works without the list because both vendors address an
entry by id alone.

The local `MapMetaConfig` attachment is dropped along with the entry.

Where to read an id:

| Vendor | Where |
| --- | --- |
| mod.io | On the mod's own page, right hand side — **My Content → Mods**, click the mod name. `https://mod.io/search/mods/<id>` opens a mod by id if you want to double check |
| Steam Workshop | In the item url, after `?id=` |

## Adding a new vendor

1. Create a folder under `Publishing/<Vendor>/` in the submodule with its own assembly definition, an
   `includePlatforms` of `Editor`, and a define constraint of your own.
2. Implement `IModPublisher` and `IModAuthProvider`.
3. Annotate the publisher with `[ModPublisher("vendorid", "Display Name")]` and give it a public constructor taking
   a single `ModPublisherContext`.
4. Add a `ModVendorConfig<T>` subclass if the vendor needs credentials.
5. Register the SDK assembly name in `ModPublishingDefines.VendorSdks` so the define is managed automatically.

Nothing in the uploader needs to change — the registry picks the new vendor up and it appears in the picker.

## Known differences between vendors

These are the places where the two vendors genuinely disagree, and how the shared contract handles it.

| Topic | Steam Workshop | mod.io |
| --- | --- | --- |
| Creating an entry | Would accept a blank item, but the tool always sends the build with it | Refuses without a **name, summary and logo** — the MapMetaConfig must have an icon |
| Summary | No separate field | Required, max 250 chars — the uploader uses the first line of the map description |
| Title length | 128 | 50 |
| Version on a file | Not supported — the field is hidden | Stored and shown on the mod page |
| Status in the item list | Public / Friends only / Private / Banned / Awaiting agreement | Only "file uploaded" — see below |
| Visibility of existing items | Reported | Not exposed by the plugin |
| Local install folder | Yes, for subscribed items | No |
| Tags | Free-form | Must be registered in **Admin → Tags** first, which is why `Apply Content Tags` is off by default |

New items are created **private/hidden** on both vendors, so you can test before making them public.

### mod.io does not report a mod's status

The website shows a review status (`Pending` / `Accepted`) and a visibility flag per mod. Both travel in the api
response, but the Unity plugin drops them when it builds its `Mod` object, and the raw request builder
(`ModioAPIRequest.New`, `ModioAPIRequestOptions.AddQueryParameter`) is `internal` — so there is no supported way to
read either from an external assembly.

The item list therefore shows only what can be established honestly — whether a file has been uploaded — and the
tooltip links to the mod page, which is where the real status lives. If mod.io later exposes these on `Mod`, the
only thing that needs changing is `ModIoPublisher.DescribeStatus`.

### `INVALID_JSON` usually means something else entirely

`ModioAPIUnityClient` turns **any** response it cannot parse into `ErrorCode.INVALID_JSON`, and the message it prints
— *"You have used the input_json parameter with semantically incorrect JSON"* — describes a request problem that is
almost never the actual cause. The real reason is logged one line earlier, or only at `Verbose`.

Before theorising, read the lines immediately above it in the Editor log:

- `HTTP Code: [502] ... <html>` — the server answered with a gateway error page. Nothing about the request payload
  is wrong. If it hits every `/me/*` call at once, suspect the session rather than the code: a token minted against
  a different endpoint (for instance the deprecated `api.mod.io`) authenticates but fails on the calls that matter.
  **Sign out and sign in again.** The tool now drops the session automatically when the endpoint changes.
- nothing at all — it is a genuine deserialization failure. Raise **Log Level** to `Verbose` on `ModIoConfig` to see
  the exception; one known case is described below.

### A mod.io mod without a file breaks the whole list

`ModObject.Modfile` is a **non-nullable** `ModfileObject` struct, while the api sends `"modfile": null` for a mod
that has no file uploaded yet. Newtonsoft cannot bind null to a non-nullable struct, so deserialization of the
entire page throws — and one fileless mod makes *every* mod of that user unreadable, including the healthy ones.

The failure surfaces as `INVALID_JSON: You have used the input_json parameter with semantically incorrect JSON`,
which is misleading: it is the SDK's catch-all for a response it could not parse. The real exception is logged only
at `Verbose`, which is why `ModIoConfig` has a **Log Level** field — raise it when a response fails to parse.

Because of this, the uploader **attaches the built payload while creating the entry** rather than creating an empty
one first. If you create an item with no finished build, it warns you: the resulting mod cannot be listed until a
file reaches it, and the list is how you would otherwise select it.

> [!IMPORTANT]
> If your list already fails to load, you have a fileless mod on the account. Remove it with **Delete…** and enter
> its id — that path deliberately does not need the list, see [Deleting an item](#deleting-an-item).

### The item list is refreshed through SyncUserCreations

`User.GetUserCreations` reads through `ModCache`, and nothing invalidates that cache when a mod is created — an
empty result fetched before your first mod existed would otherwise be replayed for the rest of the editor session.

`ModIoPublisher` therefore does not use it. It calls `User.SyncUserCreations()`, which goes straight to the api and
refreshes `User.ModRepository`, and reads the list from there. `User.Sync()` would also drop the cache, but it drags
in collections, wallet and entitlements — work this tool has no use for, and each one logs loudly when its endpoint
is having a bad day.
