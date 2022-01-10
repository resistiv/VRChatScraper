using System;

namespace VRChatScraper
{
    /// <summary>
    /// Represents a processor for passed arguments.
    /// </summary>
    public class ArgumentHandler
    { 
        /// <summary>
        /// Resolves a collection of arguments into an ApiClient.
        /// </summary>
        /// <param name="arguments">The program arguments to be processed.</param>
        /// <returns>The resultant ApiClient, utilizing a ClientProperites representing the program arguments.</returns>
        public static ApiClient Resolve(string[] arguments)
        {
            ClientProperties properties = new ClientProperties();

            if (arguments.Length == 0) Program.Usage(false);
            for (int i = 0; i < arguments.Length; i++)
            {
                switch (arguments[i])
                {
                    case "-h":
                    case "--help":
                        Program.Usage(true);
                        i = arguments.Length;
                        break;
                    case "-l":
                    case "--login":
                        if (i + 2 < arguments.Length)
                        {
                            properties.Username = arguments[++i];
                            properties.Password = arguments[++i];
                        }
                        else Program.Terminate($"Too few arguments for option \"--login\", aborting.");
                        break;
                    case "-a":
                    case "--avatar":
                        if (i + 1 < arguments.Length)
                            properties.GrabId = arguments[++i];
                        else Program.Terminate($"Too few arguments for option \"--avatar\", aborting.");
                        properties.Grab = FetchType.Avatar;
                        break;
                    case "-w":
                    case "--world":
                        if (i + 1 < arguments.Length)
                        {
                            string temp = arguments[i + 1];
                            if (!temp.StartsWith("-"))
                                properties.GrabId = arguments[++i];
                        }
                        else Program.Terminate($"Too few arguments for option \"--world\", aborting.");
                        properties.Grab = FetchType.World;
                        break;
                    case "-f":
                    case "--featured":
                        if (i + 1 < arguments.Length)
                            properties.Featured = bool.Parse(arguments[++i]);
                        else Program.Terminate($"Too few arguments for option \"--featured\", aborting.");
                        properties.Featured = true;
                        if (!properties.FiltersChanged) properties.FiltersChanged = true;
                        break;
                    case "-s":
                    case "--sort":
                        if (i + 1 < arguments.Length)
                            properties.Sort = ParseSortOption(arguments[++i]);
                        else Program.Terminate($"Too few arguments for option \"--sort\", aborting.");
                        if (!properties.FiltersChanged) properties.FiltersChanged = true;
                        break;
                    case "-u":
                    case "--user_id":
                        if (i + 1 < arguments.Length)
                            properties.UserId = arguments[++i];
                        else Program.Terminate($"Too few arguments for option \"--user_id\", aborting.");
                        if (!properties.FiltersChanged) properties.FiltersChanged = true;
                        break;
                    case "-n":
                    case "--number":
                        if (i + 1 < arguments.Length)
                            properties.Number = Convert.ToInt32(arguments[++i]);
                        else Program.Terminate($"Too few arguments for option \"--number\", aborting.");
                        if (!properties.FiltersChanged) properties.FiltersChanged = true;
                        break;
                    case "-o":
                    case "--order":
                        if (i + 1 < arguments.Length)
                            properties.Order = ParseOrderOption(arguments[++i]);
                        else Program.Terminate($"Too few arguments for option \"--order\", aborting.");
                        if (!properties.FiltersChanged) properties.FiltersChanged = true;
                        break;
                    case "-i":
                    case "--offset":
                        if (i + 1 < arguments.Length)
                            properties.Offset = Convert.ToInt32(arguments[++i]);
                        else Program.Terminate($"Too few arguments for option \"--offset\", aborting.");
                        if (!properties.FiltersChanged) properties.FiltersChanged = true;
                        break;
                    case "-q":
                    case "--search":
                        if (i + 1 < arguments.Length)
                            properties.Search = arguments[++i];
                        else Program.Terminate($"Too few arguments for option \"--search\", aborting.");
                        if (!properties.FiltersChanged) properties.FiltersChanged = true;
                        break;
                    case "-t":
                    case "--tags":
                        if (i + 1 < arguments.Length)
                            properties.Tags = arguments[++i].Split(',');
                        else Program.Terminate($"Too few arguments for option \"--tags\", aborting.");
                        if (!properties.FiltersChanged) properties.FiltersChanged = true;
                        break;
                    case "-e":
                    case "--exclude_tags":
                        if (i + 1 < arguments.Length)
                            properties.ExcludedTags = arguments[++i].Split(',');
                        else Program.Terminate($"Too few arguments for option \"--excude_tags\", aborting.");
                        if (!properties.FiltersChanged) properties.FiltersChanged = true;
                        break;
                    case "-r":
                    case "--release_status":
                        if (i + 1 < arguments.Length)
                            properties.ReleaseStatus = ParseReleaseStatus(arguments[++i]);
                        else Program.Terminate($"Too few arguments for option \"--release_status\", aborting.");
                        if (!properties.FiltersChanged) properties.FiltersChanged = true;
                        break;
                    case "-p":
                    case "--platform":
                        if (i + 1 < arguments.Length)
                            properties.Platform = ParsePlatform(arguments[++i]);
                        else Program.Terminate($"Too few arguments for option \"--platform\", aborting.");
                        if (!properties.FiltersChanged) properties.FiltersChanged = true; 
                        break;
                    case "-v":
                    case "--scrape_avatars":
                        properties.ScrapeAvatars = true;
                        break;
                    case "-c":
                    case "--scrape_user_avatar":
                        properties.ScrapeUserAvatar = true;
                        break;
                    default:
                        Program.Terminate($"Encountered invalid argument \"{arguments[i]}\", aborting.");
                        break;
                }
            }

            if (Globals.Debug) Logger.Log(LogType.Debug, properties.ToString());

            CheckConflicts(properties);
            ApiClient api = new ApiClient(properties);

            return api;
        }

        /// <summary>
        /// Validates user input by catching property conflicts.
        /// </summary>
        /// <param name="properties">The collection of properties to be validated.</param>
        private static void CheckConflicts(ClientProperties properties)
        {
            /* General conflicts */
            if (properties.Grab == FetchType.None)
                Program.Terminate($"Neither \"--avatar\" nor \"--world\" were passed, aborting.");
            /*if (properties.Username == "" || properties.Password == "")
                Program.Terminate($"{"[ERR]".Pastel(Color.Red)} Did not receive {"VR".PastelBg(Color.White).Pastel(Color.Black)}{"Chat".Pastel(Color.White)} login credentials, aborting.");*/
            if (properties.Grab == FetchType.Avatar && properties.FiltersChanged)
                Program.Terminate($"Avatar listing is deprecated, aborting.");
            if (properties.Grab == FetchType.World && (properties.GrabId != "" && properties.FiltersChanged))
                Program.Terminate($"Can only fetch a World list or single World, not both, aborting.");
            if (properties.Grab == FetchType.Avatar && properties.ScrapeAvatars)
                Program.Terminate($"Can't apply processing option \"--scrape_avatars\" to an Avatar, aborting.");
            if (properties.Grab == FetchType.Avatar && properties.ScrapeUserAvatar)
                Program.Terminate($"Can't apply processing option \"--scrape_user_avatar\" to an Avatar, aborting.");

            /* API limits and other conditional things */
            if (properties.Offset > Globals.OffsetCap)
                Program.Terminate($"Offset of {properties.Offset} exceeds the API limit of {Globals.OffsetCap}, aborting.");
            if (properties.Number > Globals.NumberCap)
                Program.Terminate($"Number of {properties.Number} exceeds the API limit of {Globals.NumberCap}, aborting.");
        }

        /// <summary>
        /// Parses a string into an OrderOptions enum type.
        /// </summary>
        /// <param name="orderOption">The string to be parsed.</param>
        /// <returns>The resultant enum type.</returns>
        private static OrderOptions ParseOrderOption(string orderOption)
        {
            switch(orderOption)
            {
                case "ascending":
                    return OrderOptions.Ascending;
                case "descending":
                    return OrderOptions.Descending;
                default:
                    Program.Terminate($"Encountered invalid \"--order\" argument \"{orderOption}\", aborting.");
                    return OrderOptions.None;
            }
        }

        /// <summary>
        /// Parses a string into a PlatformType enum type.
        /// </summary>
        /// <param name="platform">The string to be parsed.</param>
        /// <returns>The resultant enum type.</returns>
        private static PlatformType ParsePlatform(string platform)
        {
            switch(platform)
            {
                case "android":
                    return PlatformType.Android;
                case "standalonewindows":
                    return PlatformType.StandaloneWindows;
                default:
                    Program.Terminate($"Encountered invalid \"--platform\" argument \"{platform}\", aborting.");
                    return PlatformType.None;
            }
        }

        /// <summary>
        /// Parses a string into a ReleaseStatus enum type.
        /// </summary>
        /// <param name="releaseStatus">The string to be parsed.</param>
        /// <returns>The resultant enum type.</returns>
        private static ReleaseStatus ParseReleaseStatus(string releaseStatus)
        {
            switch(releaseStatus)
            {
                case "public":
                    return ReleaseStatus.Public;
                case "private":
                    return ReleaseStatus.Private;
                case "hidden":
                    return ReleaseStatus.Hidden;
                case "all":
                    return ReleaseStatus.All;
                default:
                    Program.Terminate($"Encountered invalid \"--release_status\" option \"{releaseStatus}\", aborting.");
                    return ReleaseStatus.None;
            }
        }

        /// <summary>
        /// Parses a string into a SortOptions enum type.
        /// </summary>
        /// <param name="sortOption">The string to be parsed.</param>
        /// <returns>The resultant enum type.</returns>
        private static SortOptions ParseSortOption(string sortOption)
        {
            switch(sortOption.ToLower())
            {
                case "popularity":
                    return SortOptions.Popularity;
                case "heat":
                    return SortOptions.Heat;
                case "trust":
                    return SortOptions.Trust;
                case "shuffle":
                    return SortOptions.Shuffle;
                case "random":
                    return SortOptions.Random;
                case "favorites":
                    return SortOptions.Favorites;
                case "reportscore":
                    return SortOptions.ReportScore;
                case "reportcount":
                    return SortOptions.ReportCount;
                case "publicationdate":
                    return SortOptions.PublicationDate;
                case "labspublicationdate":
                    return SortOptions.LabsPublicationDate;
                case "created":
                    return SortOptions.Created;
                case "_created_at":
                    return SortOptions.CreatedAt;
                case "updated":
                    return SortOptions.Updated;
                case "_updated_at":
                    return SortOptions.UpdatedAt;
                case "order":
                    return SortOptions.Order;
                case "relevance":
                    return SortOptions.Relevance;
                case "magic":
                    return SortOptions.Magic;
                default:
                    Program.Terminate($"Encountered invalid \"--sort\" option \"{sortOption}\", aborting.");
                    return SortOptions.None;
            }
        }
    }
}
