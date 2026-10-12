namespace Modio.Mods.Builder
{
    // CARX PATCH: upload progress of ModfileBuilder.PublishModfile, see CARX_PATCHES.md section 2.
    /// <summary>Stage of a modfile upload reported through <see cref="ModfileBuilder.SetUploadProgress"/>.</summary>
    public enum ModfileUploadStage
    {
        /// <summary>The source directory is being zipped; bytes are source bytes read so far / total source size.</summary>
        Compressing,
        /// <summary>The zip is being sent; bytes are zip bytes that left the machine / zip length.</summary>
        Uploading,
        /// <summary>The last byte is sent and mod.io is processing the file; bytes are the zip length.</summary>
        Processing,
    }
}
