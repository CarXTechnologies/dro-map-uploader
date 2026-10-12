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

The SDK compiles against patch 2 behind a scripting define: if it is not re-applied, the project stops compiling until
that define is removed, see [Used by](#used-by) in section 2.

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

---

## 2. Modfile upload progress (`ModfileBuilder.SetUploadProgress`)

**Files:**

- new: `Modio/API/ModioTransferProgress.cs`, `Modio/FileIO/ReadCountingStreamWrapper.cs`,
  `Modio/Mods/Builder/ModfileUploadStage.cs`, `Modio/Mods/Builder/ModfileUploadProgress.cs` (each with its `.meta`);
- changed: `Modio/Mods/Builder/ModfileBuilder.cs`, `Modio/FileIO/BaseDataStorage.cs`, `Unity/ModioApiUnityClient.cs`.

**Why:** `ModBuilder.Publish()` zips the content folder and uploads it without reporting anything, so the uploader
showed 0 % until the very end and it was not visible that anything was happening. Upstream has no hook for it: the
zip is written by `IModioDataStorage.CompressToZip` and the bytes are sent by `IModioAPIInterface.GetJson`, several
calls below `ModfileBuilder`, through generated endpoints. Passing a progress down would change all of those
signatures, so the progress travels ambiently instead.

**What:**

- `ModioTransferProgress` (namespace `Modio.API`, public) holds a listener in an `AsyncLocal`. `BeginScope(listener)`
  returns an `IDisposable` that restores the previous listener; the scope is seen by everything its async flow
  awaits and by nothing else. `ReportCompress(done, total)` and `ReportRequestUpload(fraction)` are a null check when
  no scope is open; `IsListening` tells whether one is.
- `ModfileBuilder.SetUploadProgress(IProgress<ModfileUploadProgress>)` (null, the default, reports nothing). Only when
  it is set does `PublishModfile` open a scope, through the nested `UploadProgressReporter` (a `using` declaration,
  so the scope closes on every return and exception). The reporter turns the ambient reports into
  `ModfileUploadProgress(stage, doneBytes, totalBytes)`:
  - `Compressing`: source bytes read / total size of the source files;
  - `Uploading`: zip bytes sent / zip length. A request's upload fraction counts only between `BeginRequest` and
    `EndRequest` (other requests in the flow, such as the multipart session or a reauthentication, carry no zip
    bytes), mapped onto `start + fraction * length` of the zip: the whole zip for the single request, the part's
    range for a multipart part. Never goes backwards;
  - `Processing` (done = total = zip length), once: when the single request reaches fraction 1 or succeeds, or after
    the last multipart part, before `CompleteMultipartUploadSession`/`AddModfile`.

  A closed reporter ignores everything, and an exception thrown by the consumer is logged, not propagated. Reports
  are made on the thread the plugin runs on (the main thread in the editor), up to every frame.
- `BaseDataStorage.CompressToZip`: when `IsListening`, sums the file sizes from the same `IterateFilesInDirectory`
  enumeration (any exception there gives 0, "unknown", and the compression runs as before), wraps each source
  `FileStream` in `ReadCountingStreamWrapper` and polls its count every frame (`Task.Yield`) while `CompressStream`
  runs. Polling instead of reporting from the read keeps the reports on the calling thread:
  `Stream.CopyToAsync` continues on pool threads. Without a scope, the upstream loop runs unchanged.
- `ModioAPIUnityClient.SendRequest`: when the request has an `uploadHandler` and `IsListening`, reports
  `webRequest.uploadProgress` each poll when it grew, and once more after completion (skipped when cancelled).
  `uploadHandler.data` is deliberately not read, it copies the whole body. Polling and cancellation are unchanged.

Every occurrence, marked `CARX PATCH`:

- `ModfileBuilder.cs`: the fields `_uploadProgress`, `_progressReporter`; `SetUploadProgress`; in `PublishModfile`
  the scope after the argument checks, `BeginUpload` once the zip is open, `BeginRequest`/`EndRequest` and
  `ReportProcessing` around the single `GetAddModfileTask`; in `AddMultipartModfile` `ReportProcessing` after
  `AddAllMulipartUploadParts`; in `AddAllMulipartUploadParts` `BeginRequest`/`EndRequest` around
  `AddMultipartUploadPart`; the nested class `UploadProgressReporter` at the end of the class.
- `BaseDataStorage.cs`: `using Modio.API;`; in `CompressToZip` the progress total before the zip stream and the
  counted branch in the loop; the new private methods `GetCompressProgressTotal` and
  `CompressStreamReportingProgress` after it.
- `ModioApiUnityClient.cs`: in `SendRequest` the `reportUpload` flag, the report in the loop and the final report;
  the new static method `ReportUploadProgress` after it.

Limits: only `ModioAPIUnityClient` reports request progress; with `ModioAPIHttpClient` the stage goes from
`Uploading` 0 straight to `Processing`. `RetryAddMultipartModfile`, unused upstream, maps its parts but reports no
`Processing`.

### Used by

The CarX modding SDK (`Assets/Plugins/CarX.Modding.Creator`, the mod.io publisher) calls `SetUploadProgress` and
`ModfileUploadProgress` **only under the scripting define `CARX_MODDING_MODIO_UPLOAD_PROGRESS`**, which means "the
plugin carries this patch". Without the define the SDK does not touch this API.

- `ModPublishingDefines.Sync` (menu *ModSystem/Sync Publishing Defines*, also scheduled by `ModPublisherSession`) adds
  the define when the type `Modio.Mods.Builder.ModfileUploadProgress` exists and removes it when it does not.
  MapUploader also sets it in `ProjectSettings`.
- **After replacing the plugin without re-applying this patch** the define is still set and the SDK no longer compiles
  (`SetUploadProgress` is missing). Run *ModSystem/Sync Publishing Defines*, or remove
  `CARX_MODDING_MODIO_UPLOAD_PROGRESS` from *Project Settings > Player > Scripting Define Symbols*, so the SDK compiles
  again. Re-applying the patch and syncing turns the progress back on.
- While the project has compile errors Unity keeps running the assemblies compiled before the update, which still
  contain the patched type, so the menu can find nothing to change. In that case remove the define by hand, for every
  build target that lists it (MapUploader: `Standalone`).

---

## 3. Multipart publish returns the result of adding the modfile

**File:** `Modio/Mods/Builder/ModfileBuilder.cs`
**Why:** a zip over 100 MiB is published through a multipart upload session that ends with `AddModfile` (or
`AddSourceModfile`) referencing the session's `upload_id`. Upstream ignored the result of that last call and returned
`Error.None`, so when mod.io refused the modfile the publish looked successful: `ModBuilder.Publish()` returned no
error and `ModBuilder.Results` listed `Modfile` as succeeded, while the mod got no new file. The single part upload
already returns the error of its `AddModfile`.

**What:** return the error of that final call. Two occurrences, marked `CARX PATCH`:

- `AddMultipartModfile`:

  ```csharp
  // CARX PATCH: return the AddModfile result, not always Error.None; see CARX_PATCHES.md section 3.
  return upload.error;
  ```

  (was `return Error.None;`)

- `RetryAddMultipartModfile`, unused upstream, the same pattern, kept consistent:

  ```csharp
  // CARX PATCH: return the AddModfile result, not always Error.None; see CARX_PATCHES.md section 3.
  return (upload.error, upload.modfileObject);
  ```

  (was `return (Error.None, upload.modfileObject);`)
