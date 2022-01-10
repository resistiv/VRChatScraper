using Pastel;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace VRChatScraper
{
    /// <summary>
    /// Represents a connection to the VRChat API.
    /// </summary>
    public class ApiClient
    {
        private readonly HttpClient client;
        private readonly CookieContainer cookieJar;
        private readonly ClientProperties properties;
        private readonly string responseData;

        /// <summary>
        /// Creates a new VRChat ApiClient.
        /// </summary>
        /// <param name="properties">Properties used for requests to the API.</param>
        public ApiClient(ClientProperties properties)
        {
            // Instantiate obvious stuff
            this.properties = properties;

            // Restore a cookieJar from a previous session
            bool cookiesLoaded = false;
            if (File.Exists(Globals.CookieJarFileName))
            {
                cookieJar = ReadCookies();
                cookiesLoaded = true;
            }
            else
            {
                cookieJar = new CookieContainer();
            }

            // Create our client
            client = new HttpClient(new HttpClientHandler() { AllowAutoRedirect = true, CookieContainer = cookieJar });
            client.BaseAddress = new Uri(Globals.ApiUrl);

            // Construct all of our request headers, including initial auth
            CreateHeaders();

            // Get auth cookie and remove auth header for future requests
            bool hasAuth = HasAuthCookie(cookieJar);
            if (Globals.Debug) Logger.Log(LogType.Debug, $"cookiesLoaded = {cookiesLoaded}, hasAuth = {hasAuth}");
            if (!cookiesLoaded || !hasAuth)
            {
                Authenticate();
                SaveCookies();
            }
            client.DefaultRequestHeaders.Remove("Authorization");

            // Use ClientProperties to construct the user's request into a valid URL
            string urlEnd = CreateRequestUrl();

            if (Globals.Debug) Logger.Log(LogType.Debug, $"{Globals.ApiUrl}{urlEnd}");

            responseData = Request(urlEnd);
        }

        /* Request helpers and methods */

        /// <summary>
        /// Reaches the VRC authentication endpoint to fetch an authentication cookie.
        /// </summary>
        private void Authenticate()
        {
            if (properties.Username == "" || properties.Password == "")
            {
                Program.Terminate("This session requires authentication, please pass login credentials using -l.");
            }
            Logger.Log(LogType.Info, "Authenticating...");
            Request($"{Globals.ApiAuth}?apiKey={Globals.ApiKey}");

            if (Globals.Debug)
            {
                CookieCollection cookies = cookieJar.GetCookies(new Uri(Globals.ApiBaseUrl));
                foreach (Cookie cook in cookies)
                {
                    Logger.Log(LogType.Debug, $"Cookie:" +
                            $"\n\t{cook.Name} = {cook.Value}" +
                            $"\n\tDomain: {cook.Domain}" +
                            $"\n\tPath: {cook.Path}" +
                            $"\n\tPort: {cook.Port}" +
                            $"\n\tSecure: {cook.Secure}" +
                            $"\n\tIssued: {cook.TimeStamp}" +
                            $"\n\tExpires: {cook.Expires}" +
                            $"\n\tDon't save: {cook.Discard}" +
                            $"\n\tComment: {cook.Comment}" +
                            $"\n\tUri for comments: {cook.CommentUri}");
                }
            }
        }

        /// <summary>
        /// Logs the user out of the VRC API by invalidating the current authentication cookie.
        /// </summary>
        public void Logout()
        {
            client.PutAsync($"{Globals.ApiUrl}{Globals.ApiLogout}?apiKey={Globals.ApiKey}", null);
        }

        /// <summary>
        /// Constructs request headers.
        /// </summary>
        private void CreateHeaders()
        {
            HttpRequestHeaders header = client.DefaultRequestHeaders;
            if (properties.Username != "" && properties.Password != "")
            {
                CreateAuthHeader(header);
            }
            header.UserAgent.ParseAdd(Globals.UserAgent);
            /*header.Add("X-Requested-With", "HttpClient");
            header.Add("X-MacAddress", "5A-7C-4B-FD-93-A2");
            header.Add("X-Client-Version", "");
            header.Add("X-SDK-Version", "");
            header.Add("X-Platform", "standaloneWindows");
            header.Add("Origin", "vrchat.com");*/
        }

        /// <summary>
        /// Constructs an authentication header for a first-time log-in to the VRC API.
        /// </summary>
        /// <param name="header">Collection of currently constructed headers.</param>
        private void CreateAuthHeader(HttpRequestHeaders header)
        {
            // Headers require that Basic Auth be encoded in Base64
            string authEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{properties.Username}:{properties.Password}"));
            if (header.Contains("Authorization"))
                header.Remove("Authorization");
            header.Add("Authorization", $"Basic {authEncoded}");
        }
        
        /// <summary>
        /// Constructs a request URL based on the ClientProperties provided at runtime.
        /// </summary>
        /// <returns>A string representing the resultant request URL.</returns>
        private string CreateRequestUrl()
        {
            string url = "";
            int argCount = 0;
            if (properties.Grab == FetchType.Avatar)
                url += "avatars";
            else if (properties.Grab == FetchType.World)
                url += "worlds";
            else
                Program.Terminate("Invalid FetchType passed when creating request URL");
            if (properties.GrabId != "")
                url += $"/{properties.GrabId}";
            else
            {
                // URL addition template VVV
                // url += $"{((argCount++ == 0) ? "?" : "&")}";
                if (properties.Featured != null)
                    url += $"{((argCount++ == 0) ? "?" : "&")}featured={properties.Featured}";
                if (properties.Sort != SortOptions.None)
                    url += $"{((argCount++ == 0) ? "?" : "&")}sort={SortOptionToString(properties.Sort)}";
                if (properties.Order != OrderOptions.None)
                    url += $"{((argCount++ == 0) ? "?" : "&")}order={OrderOptionToString(properties.Order)}";
                if (properties.Number != null)
                    url += $"{((argCount++ == 0) ? "?" : "&")}n={properties.Number}";
                if (properties.Offset != null)
                    url += $"{((argCount++ == 0) ? "?" : "&")}offset={properties.Offset}";
                if (properties.Search != "")
                    url += $"{((argCount++ == 0) ? "?" : "&")}search={properties.Search}";
                if (properties.Tags != null && properties.Tags.Length != 0)
                    url += $"{((argCount++ == 0) ? "?" : "&")}tag={FormatTags(properties.Tags)}";
                if (properties.ExcludedTags != null && properties.ExcludedTags.Length != 0)
                    url += $"{((argCount++ == 0) ? "?" : "&")}notag={FormatTags(properties.Tags)}";
                if (properties.ReleaseStatus != ReleaseStatus.None)
                    url += $"{((argCount++ == 0) ? "?" : "&")}releaseStatus={ReleaseStatusToString(properties.ReleaseStatus)}";
                if (properties.Platform != PlatformType.None)
                    url += $"{((argCount++ == 0) ? "?" : "&")}platform={PlatformTypeToString(properties.Platform)}";
                if (properties.UserId != "")
                    url += $"{((argCount++ == 0) ? "?" : "&")}userId={properties.UserId}";
                // url += $"{((argCount++ == 0) ? "?" : "&")}organization={Globals.ApiOrganization}";
            }

            url += $"{((argCount++ == 0) ? "?" : "&")}apiKey={Globals.ApiKey}";
            return url;
        }

        /// <summary>
        /// Constructs a request URL for a certain Avatar/World ID from an external Class.
        /// </summary>
        /// <param name="id">The ID of the Avatar/World to be requested.</param>
        /// <param name="type">The type of asset being requested.</param>
        /// <returns>A string representing the resultant request URL.</returns>
        public string CreateRequestUrl(string id, FetchType type)
        {
            string url = "";
            if (type == FetchType.World)
                url += "worlds/";
            else if (type == FetchType.Avatar)
                url += "avatars/";
            else
                Program.Terminate("Received invalid FetchType while performing secondary request, aborting.");
            url += $"{id}?apiKey={Globals.ApiKey}";
            return url;
        }

        /// <summary>
        /// Performs an asynchronous request to the VRC API.
        /// </summary>
        /// <param name="urlEnd">The parameters to be appended to the main VRC API domain for the request.</param>
        /// <returns>The content of the respective request.</returns>
        public string Request(string urlEnd)
        {
            HttpResponseMessage response = client.GetAsync(urlEnd).Result;
            if (response.IsSuccessStatusCode)
                return response.Content.ReadAsStringAsync().Result;
            else
                Program.Terminate($"Request returned bad status code ({$"{response.StatusCode}: {response.ToString()}".Pastel(Color.Red)})");
            return null;
        }

        /* Create a Scraper using the data of this ApiClient */
        /// <summary>
        /// Instantiates a Scraper using the parent properties of this ApiClient.
        /// </summary>
        /// <returns>The newly instantiated Scraper instance.</returns>
        public Scraper GenerateScraper()
        {
            return new Scraper(this, properties, responseData);
        }

        /// <summary>
        /// Getter for this ApiClient's CookieContainer.
        /// </summary>
        /// <returns>This ApiClient's CookieContainer.</returns>
        public CookieContainer GetCookies()
        {
            return cookieJar;
        }

        /// <summary>
        /// Writes this ApiClient's CookieContainer to the disk for later use.
        /// </summary>
        private void SaveCookies()
        {
            using (Stream stream = File.Create(Globals.CookieJarFileName))
            {
                try
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, cookieJar);
                    Logger.Log(LogType.Info, "Saved cookies to disk.");
                }
                catch (Exception e)
                {
                    Logger.Log(LogType.Error, "Could not write cookies to disk.");
                }
            }
        }

        /// <summary>
        /// Restores a past session's CookieContainer from the disk.
        /// </summary>
        /// <returns>The previous session's CookieContainer.</returns>
        private CookieContainer ReadCookies()
        {
            CookieContainer cookieJar = null;
            try
            {
                using (Stream stream = File.Open(Globals.CookieJarFileName, FileMode.Open))
                {
                    Logger.Log(LogType.Info, "Reading cached cookies...");
                    BinaryFormatter form = new BinaryFormatter();
                    cookieJar = (CookieContainer)form.Deserialize(stream);
                }
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, $"Could not read existing cookies.");
                cookieJar = new CookieContainer();
            }
            return cookieJar;
        }

        /// <summary>
        /// Identifies whether or not a CookieContainer contains a VRC authentication cookie.
        /// </summary>
        /// <param name="cookieJar">Collection of Cookies to be scanned for an authentication cookie.</param>
        /// <returns>True, if the CookieContainer contains a non-expired authentication cookie; false, otherwise.</returns>
        private bool HasAuthCookie(CookieContainer cookieJar)
        {
            CookieCollection jar = cookieJar.GetCookies(new Uri(Globals.ApiBaseUrl));
            foreach (Cookie cookie in jar)
            {
                if (Globals.Debug)
                {
                    Logger.Log(LogType.Debug, $"Cookie:" +
                            $"\n\t{cookie.Name} = {cookie.Value}" +
                            $"\n\tExpires: {cookie.Expires}");
                }
                if (cookie.Name == "auth" && DateTime.Compare(cookie.Expires, DateTime.Now) > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /* Helper methods related to enum conversion and string handling */

        /// <summary>
        /// Converts a SortOptions enum type into a string.
        /// </summary>
        /// <param name="option">The enum type to be converted.</param>
        /// <returns>The resultant converted enum type string.</returns>
        private string SortOptionToString(SortOptions option)
        {
            switch (option)
            {
                case SortOptions.Popularity:
                    return "popularity";
                case SortOptions.Heat:
                    return "heat";
                case SortOptions.Trust:
                    return "trust";
                case SortOptions.Shuffle:
                    return "shuffle";
                case SortOptions.Random:
                    return "random";
                case SortOptions.Favorites:
                    return "favorites";
                case SortOptions.ReportScore:
                    return "reportScore";
                case SortOptions.ReportCount:
                    return "reportCount";
                case SortOptions.PublicationDate:
                    return "publicationDate";
                case SortOptions.LabsPublicationDate:
                    return "labsPublicationDate";
                case SortOptions.Created:
                    return "created";
                case SortOptions.CreatedAt:
                    return "_created_at";
                case SortOptions.Updated:
                    return "updated";
                case SortOptions.UpdatedAt:
                    return "_updated_at";
                case SortOptions.Order:
                    return "order";
                case SortOptions.Relevance:
                    return "relevance";
                case SortOptions.Magic:
                    return "magic";
            }
            return null;
        }

        /// <summary>
        /// Converts an OrderOptions enum type into a string.
        /// </summary>
        /// <param name="option">The enum type to be converted.</param>
        /// <returns>The resultant converted enum type string.</returns>
        private string OrderOptionToString(OrderOptions option)
        {
            switch (option)
            {
                case OrderOptions.Ascending:
                    return "ascending";
                case OrderOptions.Descending:
                    return "descending";
            }
            return null;
        }

        /// <summary>
        /// Converts a ReleaseStatus enum type into a string.
        /// </summary>
        /// <param name="status">The enum type to be converted.</param>
        /// <returns>The resultant converted enum type string.</returns>
        private string ReleaseStatusToString(ReleaseStatus status)
        {
            switch (status)
            {
                case ReleaseStatus.Public:
                    return "public";
                case ReleaseStatus.Private:
                    return "private";
                case ReleaseStatus.Hidden:
                    return "hidden";
                case ReleaseStatus.All:
                    return "all";
            }
            return null;
        }

        /// <summary>
        /// Converts a PlatformType enum type into a string.
        /// </summary>
        /// <param name="platform">The enum type to be converted.</param>
        /// <returns>The resultant converted enum type string.</returns>
        private string PlatformTypeToString(PlatformType platform)
        {
            switch (platform)
            {
                case PlatformType.Android:
                    return "android";
                case PlatformType.StandaloneWindows:
                    return "standalonewindows";
            }
            return null;
        }

        /// <summary>
        /// Concatenates an array of tags into a comma-separated string.
        /// </summary>
        /// <param name="tags">The array of tags to be concatenated.</param>
        /// <returns>The resultant concatenated string.</returns>
        private string FormatTags(string[] tags)
        {
            string temp = "";
            for (int i = 0; i < tags.Length; i++)
            {
                temp += $"{tags[i]}{((i + 1 != tags.Length) ? "," : "")}";
            }
            return temp;
        }
    }
}
