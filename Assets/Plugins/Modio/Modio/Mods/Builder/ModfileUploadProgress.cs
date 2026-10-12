namespace Modio.Mods.Builder
{
    // CARX PATCH: upload progress of ModfileBuilder.PublishModfile, see CARX_PATCHES.md section 2.
    /// <summary>One upload progress report of <see cref="ModfileBuilder.SetUploadProgress"/>.</summary>
    public readonly struct ModfileUploadProgress
    {
        public ModfileUploadProgress(ModfileUploadStage stage, long doneBytes, long totalBytes)
        {
            Stage = stage;
            DoneBytes = doneBytes;
            TotalBytes = totalBytes;
        }

        public ModfileUploadStage Stage { get; }

        /// <summary>Bytes of the current <see cref="Stage"/> done so far.</summary>
        public long DoneBytes { get; }

        /// <summary>Bytes of the current <see cref="Stage"/> in total, 0 when unknown.</summary>
        public long TotalBytes { get; }

        /// <summary>Progress of the current <see cref="Stage"/>: <see cref="DoneBytes"/> / <see cref="TotalBytes"/>
        /// clamped to 0..1, 0 when <see cref="TotalBytes"/> is unknown, always 1 for
        /// <see cref="ModfileUploadStage.Processing"/>.</summary>
        public float Fraction
        {
            get
            {
                if (Stage == ModfileUploadStage.Processing) return 1f;
                if (TotalBytes <= 0 || DoneBytes <= 0) return 0f;
                if (DoneBytes >= TotalBytes) return 1f;

                return (float)((double)DoneBytes / TotalBytes);
            }
        }

        public override string ToString() => $"{Stage} {DoneBytes}/{TotalBytes}";
    }
}
