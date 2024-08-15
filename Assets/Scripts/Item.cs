using System.Runtime.InteropServices;
using Steamworks;
using Steamworks.Data;
using Steamworks.Ugc;

namespace Steamworks.Parser
{
    public struct Item
    {
        public SteamUGCDetails_t details;
        internal PublishedFileId _id;

        public Item(PublishedFileId id)
            : this()
        {
            _id = id;
        }

        public PublishedFileId Id => _id;

        public string Title { get; internal set; }

        public string Description { get; internal set; }

        public string[] Tags { get; internal set; }

        public AppId CreatorApp => details.CreatorAppID;

        public AppId ConsumerApp => details.ConsumerAppID;

        public Friend Owner => new Friend(details.SteamIDOwner);

        public float Score => details.Score;

        public bool IsPublic => details.Visibility == RemoteStoragePublishedFileVisibility.Public;

        public bool IsFriendsOnly => details.Visibility == RemoteStoragePublishedFileVisibility.FriendsOnly;

        public bool IsPrivate => details.Visibility == RemoteStoragePublishedFileVisibility.Private;

        public bool IsBanned => details.Banned;

        public bool IsAcceptedForUse => details.AcceptedForUse;

        public uint VotesUp => details.VotesUp;

        public uint VotesDown => details.VotesDown;

        public bool Download(bool highPriority = false)
        {
            return SteamUGC.Download(Id, highPriority);
        }

        public string Url =>
            string.Format("http://steamcommunity.com/sharedfiles/filedetails/?source=Facepunch.Steamworks&id={0}", Id);

        public string ChangelogUrl =>
            string.Format("http://steamcommunity.com/sharedfiles/filedetails/changelog/{0}", Id);

        public string CommentsUrl =>
            string.Format("http://steamcommunity.com/sharedfiles/filedetails/comments/{0}", Id);

        public string DiscussUrl =>
            string.Format("http://steamcommunity.com/sharedfiles/filedetails/discussions/{0}", Id);

        public string StatsUrl => string.Format("http://steamcommunity.com/sharedfiles/filedetails/stats/{0}", Id);

        public ulong NumSubscriptions { get; internal set; }

        public ulong NumFavorites { get; internal set; }

        public ulong NumFollowers { get; internal set; }

        public ulong NumUniqueSubscriptions { get; internal set; }

        public ulong NumUniqueFavorites { get; internal set; }

        public ulong NumUniqueFollowers { get; internal set; }

        public ulong NumUniqueWebsiteViews { get; internal set; }

        public ulong ReportScore { get; internal set; }

        public ulong NumSecondsPlayed { get; internal set; }

        public ulong NumPlaytimeSessions { get; internal set; }

        public ulong NumComments { get; internal set; }

        public ulong NumSecondsPlayedDuringTimePeriod { get; internal set; }

        public ulong NumPlaytimeSessionsDuringTimePeriod { get; internal set; }

        public string PreviewImageUrl { get; internal set; }

        public Editor Edit()
        {
            return new Editor(Id);
        }

        public Result Result => details.Result;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct SteamUGCDetails_t
    {
        internal PublishedFileId PublishedFileId;
        internal Result Result;
        internal WorkshopFileType FileType;
        internal AppId CreatorAppID;
        internal AppId ConsumerAppID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 129)]
        internal byte[] Title;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8000)]
        internal byte[] Description;

        internal ulong SteamIDOwner;
        internal uint TimeCreated;
        internal uint TimeUpdated;
        internal uint TimeAddedToUserList;
        internal RemoteStoragePublishedFileVisibility Visibility;
        [MarshalAs(UnmanagedType.I1)] internal bool Banned;
        [MarshalAs(UnmanagedType.I1)] internal bool AcceptedForUse;
        [MarshalAs(UnmanagedType.I1)] internal bool TagsTruncated;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1025)]
        internal byte[] Tags;

        internal ulong File;
        internal ulong PreviewFile;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 260)]
        internal byte[] PchFileName;

        public int FileSize;
        internal int PreviewFileSize;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        internal byte[] URL;

        internal uint VotesUp;
        internal uint VotesDown;
        internal float Score;
        internal uint NumChildren;
    }

    public enum RemoteStoragePublishedFileVisibility
    {
        Public,
        FriendsOnly,
        Private,
        Unlisted
    }

    internal enum ItemState
    {
        None = 0,
        Subscribed = 1,
        LegacyItem = 2,
        Installed = 4,
        NeedsUpdate = 8,
        Downloading = 16,
        DownloadPending = 32
    }

    public enum WorkshopFileType
    {
        Community = 0,
        First = 0,
        Microtransaction = 1,
        Collection = 2,
        Art = 3,
        Video = 4,
        Screenshot = 5,
        Game = 6,
        Software = 7,
        Concept = 8,
        WebGuide = 9,
        IntegratedGuide = 10,
        Merch = 11,
        ControllerBinding = 12,
        SteamworksAccessInvite = 13,
        SteamVideo = 14,
        GameManagedItem = 15,
        Max = 16
    }
}