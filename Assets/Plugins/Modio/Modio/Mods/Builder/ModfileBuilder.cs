using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Modio.API;
using Modio.API.SchemaDefinitions;
using Modio.Errors;

namespace Modio.Mods.Builder
{
    public class ModfileBuilder
    {
        public string FilePath { get; private set; } = null;
        public string Version { get; private set; } = null;
        public string ChangeLog { get; private set; } = null;
        public string MetadataBlob { get; private set; } = null;
        public Platform[] Platforms { get; private set; } = null;
        public bool IsSource { get; private set; } = false;

        ModioId ParentId => _parentModBuilder.EditTarget.Id;
        readonly ModBuilder _parentModBuilder;
        // CARX PATCH: upload progress, see CARX_PATCHES.md section 2.
        IProgress<ModfileUploadProgress> _uploadProgress;
        UploadProgressReporter _progressReporter;
        
        internal ModfileBuilder(ModBuilder parent) => _parentModBuilder = parent;

        /// <remarks>This should be a directory</remarks>
        public ModfileBuilder SetSourceDirectoryPath(string filePath)
        {
            FilePath = filePath;
            return this;
        }
        
        public ModfileBuilder SetVersion(string version)
        {
            Version = version;
            return this;
        }
        
        public ModfileBuilder SetChangelog(string changelog)
        {
            ChangeLog = changelog;
            return this;
        }
        
        public ModfileBuilder SetMetadataBlob(string metadataBlob)
        {
            MetadataBlob = metadataBlob;
            return this;
        }

        /// <remarks>This will overwrite all platforms on this modfile.</remarks>
        public ModfileBuilder SetPlatform(Platform platform) => SetPlatforms(new[] { platform, });

        /// <remarks>This will overwrite all platforms on this modfile.</remarks>
        public ModfileBuilder SetPlatforms(ICollection<Platform> platforms)
        {
            Platforms = platforms.ToArray();
            return this;
        }
        
        /// <remarks>This will overwrite all platforms on this modfile.</remarks>
        public ModfileBuilder SetPlatforms(params Platform[] platforms)
        {
            Platforms = platforms;
            return this;
        }

        public ModfileBuilder AppendPlatform(Platform platform) => AppendPlatforms(new[] { platform, });

        public ModfileBuilder AppendPlatforms(ICollection<Platform> platforms)
        {
            Platforms = Platforms.Concat(platforms).ToArray();
            return this;
        }

        /// <summary>
        /// This will configure the ModfileBuilder to upload the modfile to mod.io's Cloud Cooking solution. Files should
        /// be loose in a directory, with files that need to be cooked living in a 'Bundle' directory.
        /// </summary>
        /// <remarks>Requires Cloud Cooking to be enabled for the game. Publishing will return an error if Cloud
        /// Cooking is not enabled for the game.</remarks>
        public ModfileBuilder SetSourceFile(bool isSourceFile)
        {
            IsSource = isSourceFile;
            return this;
        }

        // CARX PATCH: upload progress, see CARX_PATCHES.md section 2.
        /// <summary>
        /// Receives the progress of publishing this modfile: <see cref="ModfileUploadStage.Compressing"/> the source
        /// directory, <see cref="ModfileUploadStage.Uploading"/> the zip, then <see cref="ModfileUploadStage.Processing"/>
        /// once its last byte is sent and mod.io is yet to answer. <c>null</c> (the default) reports nothing.
        /// </summary>
        /// <remarks>Reports are made on the thread the plugin runs on (the main thread in Unity), as often as every
        /// frame; throttle in <paramref name="progress"/> if needed.</remarks>
        public ModfileBuilder SetUploadProgress(IProgress<ModfileUploadProgress> progress)
        {
            _uploadProgress = progress;
            return this;
        }

        public ModBuilder FinishModfile() => _parentModBuilder;
        
        internal async Task<Error> PublishModfile()
        {
            if (_parentModBuilder.EditTarget is null)
            {
                ModioLog.Error?.Log($"Unable to publish modfile, no {typeof(ModioId)} found to upload to. How did you get here?");
                return new Error(ErrorCode.BAD_PARAMETER);
            }

            if (string.IsNullOrEmpty(FilePath))
            {
                ModioLog.Error?.Log($"No path to modfile provided, cannot upload mod.");
                return new Error(ErrorCode.BAD_PARAMETER);
            }

            // CARX PATCH: listen to compression and upload progress only while SetUploadProgress is set; the scope is
            // closed when this method leaves, whatever the path.
            _progressReporter = UploadProgressReporter.Open(_uploadProgress);
            using UploadProgressReporter progressScope = _progressReporter;

            (Error error, Stream dataStorageStream) = await ModioClient.DataStorage.CompressToZipStream(FilePath, ParentId);

            if (error)
                return error;

            await using(Stream readStream = dataStorageStream)
            {
                const long oneHundredMiB = 100 * 1024 * 1024;

                // CARX PATCH: compressing is done, the zip is uploaded from here on.
                _progressReporter?.BeginUpload(readStream.Length);

                if (readStream.Length > oneHundredMiB) //recommended limit
                {
                    error = await AddMultipartModfile(readStream);
                }
                else
                {
                    var modioAPIFileParameter = new ModioAPIFileParameter(readStream)
                    {
                        // CARX PATCH: name the modfile after the mod id instead of the fixed "upload.zip".
                        Name = $"{ParentId}.zip",
                    };

                    string[] platformStrings = Platforms?.Select(GetPlatformHeader).ToArray();

                    var addModfileRequest = new AddModfileRequest(
                        modioAPIFileParameter,
                        Version,
                        ChangeLog,
                        MetadataBlob,
                        platformStrings,
                        null
                    );

                    // CARX PATCH: this one request carries the whole zip; set before the task starts sending.
                    _progressReporter?.BeginRequest(0, readStream.Length, true);

                    var uploadTask = GetAddModfileTask(addModfileRequest);

                    (error, _) = await uploadTask;

                    // CARX PATCH: a success means the last byte was sent, even if no poll saw it go.
                    _progressReporter?.EndRequest();
                    if (!error) _progressReporter?.ReportProcessing();
                }
            }
            await ModioClient.DataStorage.CleanUpCompressToZipStream(ParentId);
            
            return error;
        }
        
        async Task<Error> AddMultipartModfile(Stream readStream)
        {
            var nonce = $"{ParentId}_{readStream.Length}_{DateTime.UtcNow.Ticks}";

            (Error error, MultipartUploadObject? multipartUploadObject) session =
                await ModioAPI.FilesMultipartUploads.CreateMultipartUploadSession(
                    ParentId,
                    // CARX PATCH: name the modfile after the mod id instead of the fixed "upload.zip".
                    new CreateMultipartUploadSessionRequest($"{ParentId}.zip", nonce)
                );

            if (session.error) return session.error;
            
            if (!session.multipartUploadObject.HasValue)
                return new Error(ErrorCode.NO_DATA_AVAILABLE);

            string uploadId = session.multipartUploadObject.Value.UploadId;

            Error error = await AddAllMulipartUploadParts(uploadId,  0, readStream);

            if (error) return error;

            // CARX PATCH: the last part is sent; mod.io assembles the parts and adds the modfile from here on.
            _progressReporter?.ReportProcessing();

            if (session.error) return session.error;

            (Error error, MultipartUploadObject? multipartUploadObject) end =
                await ModioAPI.FilesMultipartUploads.CompleteMultipartUploadSession(
                    uploadId,
                    ParentId
                );

            if (end.error) return end.error;
            
            string[] platformStrings = Platforms.Select(GetPlatformHeader).ToArray();

            (Error error, ModfileObject? modfileObject) upload = await GetAddModfileTask(
                new AddModfileRequest(ModioAPIFileParameter.None, Version, ChangeLog, MetadataBlob, platformStrings, uploadId)
            );

            // CARX PATCH: return the AddModfile result, not always Error.None; see CARX_PATCHES.md section 3.
            return upload.error;
        }
        
        async Task<Error> AddAllMulipartUploadParts(
            string uploadId,
            int partCount,
            Stream readStream
        )
        {
            const int fiftyMiB = 52_428_800; //max chunkSize
            int chunkSize = fiftyMiB;
            int endByte = chunkSize - 1; //last byte of a chunk
            int startByte = fiftyMiB * partCount;

            if (readStream.CanSeek) readStream.Position = startByte;

            var buffer = new byte[chunkSize];

            while (await readStream.ReadAsync(buffer, 0, chunkSize) > 0)
            {
                byte[] data;

                if (endByte >= readStream.Length)
                {
                    endByte = (int)readStream.Length - 1; //adjust end byte to match the last byte of the zip file
                }

                //shrink byte array if last part
                if (endByte + 1 - startByte < chunkSize)
                {
                    data = new byte[endByte + 1 - startByte];
                    Array.Copy(buffer, data, endByte + 1 - startByte);
                }
                else
                {
                    data = buffer;
                }

                // CARX PATCH: map this part's upload progress onto the whole zip.
                _progressReporter?.BeginRequest(startByte, data.Length, false);

                (Error error, MultipartUploadPartObject? multipartUploadPartObject) part =
                    await ModioAPI.FilesMultipartUploads.AddMultipartUploadPart(
                        uploadId,
                        ParentId,
                        $"bytes {startByte}-{endByte}/{readStream.Length}",
                        data
                    );

                // CARX PATCH: see above.
                _progressReporter?.EndRequest();

                if (part.error)
                {
                    return part.error;
                }

                startByte = endByte + 1;
                endByte = startByte + chunkSize - 1;
            }

            return Error.None;
        }
        
        async Task<(Error, ModfileObject?)> RetryAddMultipartModfile(
            string uploadId,
            string version,
            string changelog,
            string metadataBlob,
            string[] platforms,
            Stream readStream
        )
        {
            const int pageSize = 410; // approximate max for an upload 

            var filter = new ModioAPI.FilesMultipartUploads.GetMultipartUploadPartsFilter(0, pageSize, uploadId);

            (Error error, Pagination<MultipartUploadPartObject[]>? multipartUploadPartObjects) getPartsResponse =
                await ModioAPI.FilesMultipartUploads.GetMultipartUploadParts(ParentId, filter);

            if (getPartsResponse.error) return (getPartsResponse.error, null);

            if (!getPartsResponse.multipartUploadPartObjects.HasValue)
                return (new Error(ErrorCode.NO_DATA_AVAILABLE), null);

            int partCount = getPartsResponse.multipartUploadPartObjects.Value.Data.Length;

            Error error = await AddAllMulipartUploadParts(uploadId, partCount, readStream);

            if (error) return (error, null);

            (Error error, MultipartUploadObject? multipartUploadObject) end =
                await ModioAPI.FilesMultipartUploads.CompleteMultipartUploadSession(
                    uploadId,
                    ParentId
                );

            if (end.error) return (end.error, null);

            (Error error, ModfileObject? modfileObject) upload = await GetAddModfileTask(
                new AddModfileRequest(ModioAPIFileParameter.None, version, changelog, metadataBlob, platforms, uploadId)
            );

            // CARX PATCH: return the AddModfile result, not always Error.None; see CARX_PATCHES.md section 3.
            return (upload.error, upload.modfileObject);
        }

        /// <summary>
        /// Will return <see cref="ModioAPI.Files.AddSourceModfile"/> if <see cref="IsSource"/> is true, otherwise it'll
        /// <see cref="ModioAPI.Files.AddModfile"/>.
        /// </summary>
        Task<(Error error, ModfileObject? modfileObject)> GetAddModfileTask(AddModfileRequest body) => IsSource
            ? ModioAPI.Files.AddSourceModfile(ParentId, body)
            : ModioAPI.Files.AddModfile(ParentId, body);

        public enum Platform
        {
            Windows,
            Mac,
            Linux,
            Android,
            IOS,
            XboxOne,
            XboxSeriesX,
            PlayStation4,
            PlayStation5,
            Switch,
            Oculus,
        }

        static string GetPlatformHeader(Platform platform) => platform switch
        {
            Platform.Windows      => "windows",
            Platform.Mac          => "mac",
            Platform.Linux        => "linux",
            Platform.Android      => "android",
            Platform.IOS          => "ios",
            Platform.XboxOne      => "xboxone",
            Platform.XboxSeriesX  => "xboxseriesx",
            Platform.PlayStation4 => "ps4",
            Platform.PlayStation5 => "ps5",
            Platform.Switch       => "switch",
            Platform.Oculus       => "oculus",
            _                     => string.Empty,
        };

        // CARX PATCH: upload progress, see CARX_PATCHES.md section 2.
        /// <summary>
        /// Turns the ambient <see cref="ModioTransferProgress"/> reports of one <see cref="PublishModfile"/> into
        /// <see cref="ModfileUploadProgress"/>. Opening it opens the scope, disposing it closes the scope; a closed
        /// reporter ignores everything.
        /// </summary>
        sealed class UploadProgressReporter : ModioTransferProgress.IListener, IDisposable
        {
            readonly IProgress<ModfileUploadProgress> _progress;
            readonly IDisposable _scope;
            bool _closed;
            bool _uploading;
            bool _processing;
            long _zipLength;
            long _uploadedBytes;
            bool _requestActive;
            long _requestStart;
            long _requestLength;
            bool _requestEndsUpload;

            UploadProgressReporter(IProgress<ModfileUploadProgress> progress)
            {
                _progress = progress;
                _scope = ModioTransferProgress.BeginScope(this);
            }

            /// <returns><c>null</c> when <paramref name="progress"/> is null: no scope is opened then.</returns>
            public static UploadProgressReporter Open(IProgress<ModfileUploadProgress> progress)
                => progress != null ? new UploadProgressReporter(progress) : null;

            public void OnCompress(long doneBytes, long totalBytes)
            {
                if (_closed || _uploading) return;

                Report(new ModfileUploadProgress(ModfileUploadStage.Compressing, doneBytes, totalBytes));
            }

            public void BeginUpload(long zipLength)
            {
                if (_closed || _uploading) return;

                _uploading = true;
                _zipLength = zipLength;
                _uploadedBytes = 0;
                Report(new ModfileUploadProgress(ModfileUploadStage.Uploading, 0, zipLength));
            }

            /// <summary>Maps the upload fraction of the requests that follow onto bytes <paramref name="startByte"/>
            /// to <paramref name="startByte"/> + <paramref name="length"/> of the zip.</summary>
            /// <param name="endsUpload">Whether this request sending its last byte means the whole zip is sent.</param>
            public void BeginRequest(long startByte, long length, bool endsUpload)
            {
                _requestActive = true;
                _requestStart = startByte;
                _requestLength = length;
                _requestEndsUpload = endsUpload;
            }

            public void EndRequest() => _requestActive = false;

            public void OnRequestUpload(float fraction)
            {
                // Requests outside BeginRequest/EndRequest (upload session, reauthentication, ...) carry no zip bytes.
                if (_closed || !_uploading || _processing || !_requestActive) return;

                if (!(fraction > 0f))
                    fraction = 0f;
                else if (fraction > 1f)
                    fraction = 1f;

                long doneBytes = Math.Min(_requestStart + (long)(fraction * (double)_requestLength), _zipLength);

                // Never backwards, e.g. when a part is sent again after reauthentication.
                if (doneBytes > _uploadedBytes)
                {
                    _uploadedBytes = doneBytes;
                    Report(new ModfileUploadProgress(ModfileUploadStage.Uploading, doneBytes, _zipLength));
                }

                if (fraction >= 1f && _requestEndsUpload) ReportProcessing();
            }

            public void ReportProcessing()
            {
                if (_closed || !_uploading || _processing) return;

                _processing = true;
                Report(new ModfileUploadProgress(ModfileUploadStage.Processing, _zipLength, _zipLength));
            }

            public void Dispose()
            {
                if (_closed) return;

                _closed = true;
                _scope.Dispose();
            }

            void Report(ModfileUploadProgress progress)
            {
                // A failing progress consumer must not fail the upload.
                try
                {
                    _progress.Report(progress);
                }
                catch (Exception exception)
                {
                    ModioLog.Error?.Log($"Exception reporting modfile upload progress {progress}: {exception}");
                }
            }
        }
    }
}
