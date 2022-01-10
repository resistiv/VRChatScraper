namespace VRChatScraper
{
    /// <summary>
    /// Represents a collection of constants used throughout the application.
    /// </summary>
    public class Globals
    {
        // General program variables
        public const string Version = "1.0.0";
        public const bool Debug = false;
        public const string BaseOutputDirectory = "VRChatScraper";
        public const string CookieJarFileName = "VRChatScraper.cookies";
        public const string SevenZipDllPath = "C:\\Program Files\\7-Zip\\7z.dll";

        // API variables
        public const string ApiUrl = "https://api.vrchat.cloud/api/1/";
        public const string ApiBaseUrl = "https://api.vrchat.cloud/";
        public const string ApiAuth = "auth/user";
        public const string ApiLogout = "logout";
        public const string ApiKey = "JlE5Jldo5Jibnk5O5hTx6XVqsJu4WJ26";
        public const string ApiOrganization = "vrchat";
        public const int OffsetCap = 5000;
        public const int NumberCap = 100;
        public const string UserAgent = "VRChatScraper/1.0.0";
    }

    /// <summary>
    /// Represents the type of asset to be fetched.
    /// </summary>
    public enum FetchType
    {
        None,
        Avatar,
        World
    }

    /// <summary>
    /// Represents the available options for sorting a requested list.
    /// </summary>
    public enum SortOptions
    {
        None,
        Popularity,
        Heat,
        Trust,
        Shuffle,
        Random,
        Favorites,
        ReportScore,
        ReportCount,
        PublicationDate,
        LabsPublicationDate,
        Created,
        CreatedAt,
        Updated,
        UpdatedAt,
        Order,
        Relevance,
        Magic
    }

    /// <summary>
    /// Represents the available options for ordering a requested list.
    /// </summary>
    public enum OrderOptions
    {
        None,
        Ascending,
        Descending
    }

    /// <summary>
    /// Represents the available options for the release status of the items of a requested list.
    /// </summary>
    public enum ReleaseStatus
    {
        None,
        Public,
        Private,
        Hidden,
        All
    }

    /// <summary>
    /// Represents the available platforms for a requested list.
    /// </summary>
    public enum PlatformType
    {
        None,
        Android,
        StandaloneWindows
    }
}
