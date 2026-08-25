# Local changes to the vendored mod.io plugin

`Assets/Plugins/Modio` is a third-party SDK checked into this repository. Everything in it is upstream code except
the changes listed here.

**Every change is marked in the source with a `CARX PATCH` comment**, so the full set can be found with:

```bash
grep -rn "CARX PATCH" Assets/Plugins/Modio
```

## When updating the SDK

Replacing the folder with a newer release **silently drops these changes**. After any update, re-run the grep above:
if it returns nothing, re-apply the list below, then delete any entry that upstream has since fixed.

---

## 1. Modfile is uploaded as `<modId>.zip`, not `upload.zip`

**File:** `Modio/Mods/Builder/ModfileBuilder.cs`
**Why:** the published file has to be identifiable by the mod it belongs to. Upstream hardcodes the same
`upload.zip` for every mod, in both upload paths, and exposes no way to override it — the name is set on the request,
not derived from the stream, so replacing `IModioDataStorage.CompressToZipStream` does not help either. The mod.io
*Edit Modfile* endpoint cannot rename an existing file, so this cannot be corrected after the fact.

Two occurrences, both replacing the literal with `$"{ParentId}.zip"`:

- the single part upload, in `PublishModfile`:

  ```csharp
  var modioAPIFileParameter = new ModioAPIFileParameter(readStream)
  {
      // CARX PATCH: name the modfile after the mod id instead of the fixed "upload.zip".
      Name = $"{ParentId}.zip",
  };
  ```

- the multipart upload (files over 100 MiB), in `AddMultipartModfile`:

  ```csharp
  // CARX PATCH: name the modfile after the mod id instead of the fixed "upload.zip".
  new CreateMultipartUploadSessionRequest($"{ParentId}.zip", nonce)
  ```

`ParentId` is `_parentModBuilder.EditTarget.Id`. It is safe in both flows: `PublishModfile` refuses to run while
`EditTarget` is null, so the mod always exists by the time the file is named — including when the entry is created
and its content published in the same operation.

The temporary file `BaseDataStorage.CompressToZipStream` writes is also called `upload.zip`, and is deliberately
**not** patched: it lives under a per-mod install path and never reaches the server.
