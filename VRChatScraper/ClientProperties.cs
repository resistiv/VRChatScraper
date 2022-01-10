namespace VRChatScraper
{
    /// <summary>
    /// Represents a collection of properties to be utilized within an ApiClient.
    /// </summary>
    public class ClientProperties
    {
        // Generic properties
        internal string Username = "";
        internal string Password = "";
        internal FetchType Grab = FetchType.None;
        internal string GrabId = ""; // Optional, if provided with World & filters

        // World-specific properties
        internal bool FiltersChanged = false;
        internal bool? Featured = null;
        internal SortOptions Sort = SortOptions.None;
        internal string UserId = "";
        internal int? Number = null;
        internal OrderOptions Order = OrderOptions.None;
        internal int? Offset = null;
        internal string Search = "";
        internal string[] Tags = null;
        internal string[] ExcludedTags = null;
        internal ReleaseStatus ReleaseStatus = ReleaseStatus.None;
        internal PlatformType Platform = PlatformType.None;

        // Processing properties
        internal bool ScrapeAvatars = false;
        internal bool ScrapeUserAvatar = false;

        public override string ToString()
        {
            string temp = $"{GetType().Name}:\n" +
                          $"\tUsername: {Username}\n" +
                          $"\tPassword: {Password}\n" +
                          $"\tGrab: {Grab}\n" +
                          $"\tGrabId: {GrabId}\n" +
                          $"\tFiltersChanged: {FiltersChanged}\n" +
                          $"\tFeatured: {Featured}\n" +
                          $"\tSort: {Sort}\n" +
                          $"\tUserId: {UserId}\n" +
                          $"\tNumber: {Number}\n" +
                          $"\tOrder: {Order}\n" +
                          $"\tOffset: {Offset}\n" +
                          $"\tSearch: {Search}\n" +
                          $"\tTags: ";
            if (Tags != null)
            {
                foreach (string s in Tags)
                    temp += $"{s} ";
            }
            temp += $"\n\tExcludedTags: ";
            if (ExcludedTags != null)
            {
                foreach (string s in ExcludedTags)
                    temp += $"{s} ";
            }
            temp += $"\n\tReleaseStatus: {ReleaseStatus}\n" +
                    $"\tPlatform: {Platform}\n" +
                    $"\tScrapeAvatars: {ScrapeAvatars}\n" +
                    $"\tScrapeUserAvatar: {ScrapeUserAvatar}";
            return temp;
        }
    }
}
