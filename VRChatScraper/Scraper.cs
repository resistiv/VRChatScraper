using Pastel;
using SevenZip;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using VRChatApi.Classes;

namespace VRChatScraper
{
    /// <summary>
    /// Represents the main utility for downloading and archiving VRC API assets.
    /// </summary>
    public class Scraper
    {
        private readonly ApiClient api;
        private readonly ClientProperties properties;

        /// <summary>
        /// Instantiates a new Scraper object.
        /// </summary>
        /// <param name="api">The ApiClient to be utilized for requests.</param>
        /// <param name="properties">The ClientProperties describing request details.</param>
        /// <param name="initialResponse">The initial response data received within the current ApiClient.</param>
        public Scraper(ApiClient api, ClientProperties properties, string initialResponse)
        {
            SevenZipBase.SetLibraryPath(Globals.SevenZipDllPath);
            
            this.api = api;
            this.properties = properties;
            
            Scrape(initialResponse);

            if (Globals.Debug) Logger.Log(LogType.Debug, initialResponse);
            // api.Logout();
        }

        /// <summary>
        /// Scrapes all assets from the initial response data received from the ApiClient.
        /// </summary>
        /// <param name="responseData">The response data to be utilized for the scrape.</param>
        private void Scrape(string responseData)
        {
            // Save the paths of all downloaded packages (can be just one or multiple for ScrapeWorlds() in a certain range)
            string[] outputDir = { };
            if (properties.Grab == FetchType.Avatar)
                outputDir = ScrapeAvatar(responseData);
            else if (properties.Grab == FetchType.World && properties.GrabId != "")
                outputDir = ScrapeWorld(responseData);
            else
                outputDir = ScrapeWorlds(responseData);
            Logger.Log(LogType.Info, "Scrape complete, compressing...");

            // Once everything is downloaded, iterate through the paths and compress them all
            foreach (string dir in outputDir)
            {
                bool compressedSuccessfully = true;
                try
                {
                    SevenZipCompressor compressor = new SevenZipCompressor() { IncludeEmptyDirectories = true, CompressionLevel = CompressionLevel.Ultra, ArchiveFormat = OutArchiveFormat.SevenZip, CompressionMethod = CompressionMethod.Lzma2, DirectoryStructure = true };
                    compressor.CompressDirectory(Path.GetFullPath(dir), $"{dir}.7z");
                }
                catch (Exception e)
                {
                    /*Program.Terminate*/
                    compressedSuccessfully = false;
                    Logger.Log(LogType.Error, $"Could not compress \"{Path.GetFileName(dir)}\", skipping.");
                    if (Globals.Debug) Logger.Log(LogType.Debug, $"\t=> {e.Message}");
                }
                
                try
                {
                    // We can get rid of the individual files and subdirs if we compressed successfully into one file
                    if (compressedSuccessfully)
                        Directory.Delete(dir, true);
                }
                catch
                {
                    Logger.Log(LogType.Error, $"Could not delete directory \"{Path.GetFileName(dir)}\", skipping.");
                }

                if (compressedSuccessfully) Logger.Log(LogType.Info, $"Successfully compressed \"{Path.GetFileName(dir)}\".");
            }

            Logger.Log(LogType.Info, "Finished.");
        }

        /// <summary>
        /// Scrapes the content of an Avatar.
        /// </summary>
        /// <param name="responseData">The JSON response data to fetch scrapable links from.</param>
        /// <returns>A collection of local directories to be compressed.</returns>
        private string[] ScrapeAvatar(string responseData)
        {
            // Fix and parse avatar data
            JsonGroup avatarResponse = JsonTools.FixNoneDatesAndPrettify(responseData);
            AvatarResponse avatar = JsonTools.ParseJson<AvatarResponse>(avatarResponse.FixedJson);

            Logger.Log(LogType.Info, $"Initializing avatar scrape: {avatar.name} ({avatar.id})");

            // Create an output dir and write basic JSON response (first priority; in case anything else goes wrong, we can recover from here)
            string baseDir = $"{Globals.BaseOutputDirectory}\\Avatars\\{avatar.id}_v{avatar.version}";
            if (!Directory.Exists(baseDir))
                Directory.CreateDirectory(baseDir);
            File.WriteAllText($"{baseDir}\\{avatar.id}_v{avatar.version}.json", avatarResponse.PrettyJson);

            // Get all actual data; THE MEAT
            string[] urls = new string[] { avatar.assetUrl, avatar.imageUrl, avatar.thumbnailImageUrl, avatar.unityPackageUrlObject.unityPackageUrl, avatar.unityPackageUrl};
            Logger.Log(LogType.Info, "Scraping main avatar assets...");
            ScrapeAssets(baseDir, urls);
            Logger.Log(LogType.Info, "Scraping UnityPackage assets...");
            ScrapeUnityPackages(baseDir, avatar.unityPackages);

            return new string[] { baseDir };
        }

        /// <summary>
        /// Scrapes the content of a list of Worlds.
        /// </summary>
        /// <param name="responseData">The JSON response data to fetch scrapable links from.</param>
        /// <returns>A collection of local directories to be compressed.</returns>
        private string[] ScrapeWorlds(string responseData)
        {
            // Response data is listed as a list of WorldBriefResponses
            List<string> outDirs = new List<string>();
            JsonGroup worldListResponse = JsonTools.FixNoneDatesAndPrettify(responseData);
            WorldBriefResponse[] worlds = JsonTools.ParseJson<List<WorldBriefResponse>>(worldListResponse.FixedJson).ToArray();

            Logger.Log(LogType.Info, "Initializing multi-world scrape.");

            // Pass each BriefResponse to be processed as a full Response
            foreach (WorldBriefResponse wbr in worlds)
            {
                string worldResponse = api.Request(api.CreateRequestUrl(wbr.id, FetchType.World));
                outDirs.Add(ScrapeWorld(worldResponse)[0]);
            }

            return outDirs.ToArray();
        }

        /// <summary>
        /// Scrapes the content of a World.
        /// </summary>
        /// <param name="responseData">The JSON response data to fetch scrapable links from.</param>
        /// <returns>A collection of local directories to be compressed.</returns>
        private string[] ScrapeWorld(string responseData)
        {
            // Fix and parse JSON
            JsonGroup worldResponse = JsonTools.FixNoneDatesAndPrettify(responseData);
            WorldResponse world = JsonTools.ParseJson<WorldResponse>(worldResponse.FixedJson);

            Logger.Log(LogType.Info, $"Initializing world scrape: {world.name} ({world.id})");

            // Set up output dir and save JSON
            string baseDir = $"{Globals.BaseOutputDirectory}\\Worlds\\{world.id}_v{world.version}";
            if (!Directory.Exists(baseDir))
                Directory.CreateDirectory(baseDir);
            File.WriteAllText($"{baseDir}\\{world.id}_v{world.version}.json", worldResponse.PrettyJson);

            // GET THE MEAT BABEYYYY
            string[] urls = new string[] { world.imageUrl, world.thumbnailImageUrl, world.assetUrl, world.pluginUrl, world.unityPackageUrl, world.unityPackageUrlObject.unityPackageUrl };
            Logger.Log(LogType.Info, "Scraping main world assets...");
            ScrapeAssets(baseDir, urls);
            Logger.Log(LogType.Info, "Scraping UnityPackage assets...");
            ScrapeUnityPackages(baseDir, world.unityPackages);

            return new string[] { baseDir };
        }

        /// <summary>
        /// Downloads the assets from a list of URLs into a specified directory.
        /// </summary>
        /// <param name="baseDir">The local directory in which scraped assets are saved.</param>
        /// <param name="urls">The list of asset URLs to scrape.</param>
        private void ScrapeAssets(string baseDir, string[] urls)
        {
            foreach (string url in urls)
            {
                if (url != null && url != "")
                {
                    DataResponse response = FetchData(url);
                    if (Globals.Debug) Logger.Log(LogType.Debug ,$"{url}\n\t=> {response.fileName}");
                    if (Path.GetFullPath($"{baseDir}\\{response.fileName}").Length >= 260)
                    {
                        response.fileName = response.fileName.Substring(0, response.fileName.IndexOf(".")) + "." + Path.GetExtension(response.fileName);
                    }
                    if (!File.Exists($"{baseDir}\\{response.fileName}"))
                        File.WriteAllBytes($"{baseDir}\\{response.fileName}", response.data);
                }
            }
            return;
        }

        /* General iterator for downloading UnityPackages */
        /// <summary>
        /// Downloads the UnityPackages from a list of URLs into a specified directory.
        /// </summary>
        /// <param name="baseDir">The local directory in which sets of UnityPackages are saved.</param>
        /// <param name="unityPackages">The list of UnityPackage URLs to scrape.</param>
        private void ScrapeUnityPackages(string baseDir, List<UnityPackage> unityPackages)
        {
            foreach (UnityPackage up in unityPackages)
            {
                // UnityPackages have unique IDs and versions, so we document this within our folder hierarchy.
                string upDir = $"{baseDir}\\{up.id}_v{up.assetVersion}";
                if (!Directory.Exists(upDir))
                    Directory.CreateDirectory(upDir);
                string[] urls = new string[] { up.assetUrl, up.pluginUrl };
                ScrapeAssets(upDir, urls);
            }
        }

        /* DEPRECATED FUNCTION (Keeping it here because I recall it holding interesting information for downloading data and constructing requests) */
        /*private DataResponse FetchData(string url)
        {
            // Attempt to fetch the data
            HttpResponseMessage response = null;
            try
            {
                response = fetcher.GetAsync(url).Result;
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, $"Could not fetch data from {url} ({$"{e.GetType()}: {e.Message}".Pastel(Color.Red)})");
                return new DataResponse() { fileName = Path.GetFileName(url), data = new byte[0] };
            }

            // Resolve a filename; first try header info, fallback to redirect info, fallback to URL
            string tempFileName = "";
            if (response.Content.Headers.ContentDisposition != null && response.Content.Headers.ContentDisposition.FileName != null)
                tempFileName = response.Content.Headers.ContentDisposition.FileName;
            else if (response.RequestMessage.RequestUri.ToString() != null)
                tempFileName = Path.GetFileName(response.RequestMessage.RequestUri.ToString());
            else
                tempFileName = Path.GetFileName(url);

            // Did we actually get something, and did it succeed?
            if (response != null && response.IsSuccessStatusCode)
            {
                return new DataResponse() { fileName = tempFileName, data = response.Content.ReadAsByteArrayAsync().Result };
            }
            else
            {
                Logger.Log(LogType.Error, $"Could not fetch data, Fetcher returned bad status code ({$"{response.StatusCode}: {response}".Pastel(Color.Red)}), skipping asset \"{url}\".");

                return new DataResponse() { fileName = tempFileName, data = new byte[0] };
            }
        }*/

        /// <summary>
        /// Downloads data from a specified asset URL.
        /// </summary>
        /// <param name="url">The URL from which data is to be downloaded.</param>
        /// <returns>An asset in DataResponse form.</returns>
        private DataResponse FetchData(string url)
        {
            HttpClient cli = new HttpClient();
            cli.DefaultRequestHeaders.UserAgent.ParseAdd(Globals.UserAgent);
            HttpResponseMessage res = null;
            try
            {
                res = cli.GetAsync(url).Result;
            }
            catch (Exception e)
            {
                Logger.Log(LogType.Error, $"Could not fetch data from {url} ({$"{e.GetType()}: {e.Message}".Pastel(Color.Red)})");
                return new DataResponse() { fileName = Path.GetFileName(url), data = new byte[0] };
            }

            // Resolve a filename; first try header info, fallback to redirect info, fallback to URL
            string tempFileName = "";
            if (res.Content.Headers.ContentDisposition != null && res.Content.Headers.ContentDisposition.FileName != null)
                tempFileName = res.Content.Headers.ContentDisposition.FileName;
            else if (res.RequestMessage.RequestUri.ToString() != null)
                tempFileName = Path.GetFileName(res.RequestMessage.RequestUri.ToString());
            else
                tempFileName = Path.GetFileName(url);

            // FIX: Long filenames cause problems, for example:
            // file_040a6439-6532-4a48-b0df-e721ed608d46.179019e1b9a17e3f74a79eaec239d7407112d1e2e4b75ee2eeb4bab3b1a2e774.20.thumbnail-256.png
            // Yeah :/

            // Did we actually get something, and did it succeed?
            if (res != null && res.IsSuccessStatusCode)
            {
                return new DataResponse() { fileName = tempFileName, data = res.Content.ReadAsByteArrayAsync().Result };
            }
            else
            {
                Logger.Log(LogType.Error, $"Could not fetch data, Fetcher returned bad status code ({$"{res.StatusCode}: {res}".Pastel(Color.Red)}), skipping asset \"{url}\".");

                return new DataResponse() { fileName = tempFileName, data = new byte[0] };
            }
        }
    }

    /// <summary>
    /// Represents a downloaded asset.
    /// </summary>
    internal class DataResponse
    {
        internal string fileName { get; set; }
        internal byte[] data { get; set; }
    }
}