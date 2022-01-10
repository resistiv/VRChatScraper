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
    public class OldScraper
    {
        private const string OUTPUT_DIR = "_scraped";
        private const string SCRAPE_SORT = "created";
        private const string SCRAPE_ORDER = "ascending";
        private const int SCRAPE_SIZE_MAX = 100;

        private readonly ApiClient client;
        private readonly HttpClient fetcher;

        public Scraper(string username, string password, int scrapeSize, int scrapeOffset)
        {
            SevenZipBase.SetLibraryPath("C:\\Program Files\\7-Zip\\7z.dll");

            client = new ApiClient(username, password);
            fetcher = new HttpClient(new HttpClientHandler(){ AllowAutoRedirect = true });

            client.n = scrapeSize > SCRAPE_SIZE_MAX ? SCRAPE_SIZE_MAX : scrapeSize;
            client.offset = scrapeOffset;
            client.sort = SCRAPE_SORT;
            client.order = SCRAPE_ORDER;

            ScrapeWorlds();
        }

        private void ScrapeWorlds()
        {
            if (!Directory.Exists($"{OUTPUT_DIR}\\worlds"))
                Directory.CreateDirectory($"{OUTPUT_DIR}\\worlds");

            // Get formatted JSON list of WorldBriefResponses
            JsonGroup worldList = FixNoneDatesAndPrettify(client.SearchWorlds());
            
            // Output formatted JSON of WorldBriefResponses
            File.WriteAllText($"{OUTPUT_DIR}\\worlds\\worlds_{client.offset + 1}-{client.offset + client.n}.json", worldList.prettyJson);

            List<WorldBriefResponse> worldsBrief = JsonTools.ParseJson<List<WorldBriefResponse>>(worldList.fixedJson);

            foreach (WorldBriefResponse br in worldsBrief)
            {
                JsonGroup worldText = FixNoneDatesAndPrettify(client.GetWorld(br.id));
                WorldResponse world = JsonTools.ParseJson<WorldResponse>(worldText.fixedJson);

                Console.WriteLine($"{"[INF]".Pastel(Color.Green)} Started processing worldId: {br.id}");

                string outDir = $"{OUTPUT_DIR}\\worlds\\{world.id}_v{world.version}";
                if (!Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir);

                File.WriteAllText($"{outDir}\\{world.id}.json", worldText.prettyJson);

                if (world.imageUrl != null && world.imageUrl != "")
                {
                    //if (Program.DEBUG) Console.WriteLine($"{"[DBG]".Pastel(Color.Orange)} Fetching imageUrl {world.imageUrl}");
                    DataResponse response = FetchData(world.imageUrl);
                    if (!File.Exists($"{outDir}\\{response.fileName}"))
                        File.WriteAllBytes($"{outDir}\\{response.fileName}", response.data);
                }
                if (world.thumbnailImageUrl != null && world.thumbnailImageUrl != "")
                {
                    //if (Program.DEBUG) Console.WriteLine($"{"[DBG]".Pastel(Color.Orange)} Fetching thumbnailImageUrl {world.thumbnailImageUrl}");
                    DataResponse response = FetchData(world.thumbnailImageUrl);
                    if (!File.Exists($"{outDir}\\{response.fileName}"))
                        File.WriteAllBytes($"{outDir}\\{response.fileName}", response.data);
                }
                if (world.assetUrl != null && world.assetUrl != "")
                {
                    //if (Program.DEBUG) Console.WriteLine($"{"[DBG]".Pastel(Color.Orange)} Fetching assetUrl {world.assetUrl}");
                    DataResponse response = FetchData(world.assetUrl);
                    if (!File.Exists($"{outDir}\\{response.fileName}"))
                        File.WriteAllBytes($"{outDir}\\{response.fileName}", response.data);
                }
                if (world.pluginUrl != null && world.pluginUrl != "")
                {
                    //if (Program.DEBUG) Console.WriteLine($"{"[DBG]".Pastel(Color.Orange)} Fetching pluginUrl {world.pluginUrl}");
                    DataResponse response = FetchData(world.pluginUrl);
                    if (!File.Exists($"{outDir}\\{response.fileName}"))
                        File.WriteAllBytes($"{outDir}\\{response.fileName}", response.data);
                }
                if (world.unityPackageUrl != null && world.unityPackageUrl != "")
                {
                    //if (Program.DEBUG) Console.WriteLine($"{"[DBG]".Pastel(Color.Orange)} Fetching pluginUrl {world.unityPackageUrl}");
                    DataResponse response = FetchData(world.unityPackageUrl);
                    if (!File.Exists($"{outDir}\\{response.fileName}"))
                        File.WriteAllBytes($"{outDir}\\{response.fileName}", response.data);
                }
                
                foreach (UnityPackage up in world.unityPackages)
                {
                    if (up.assetUrl != null && up.assetUrl != "")
                    {
                        //if (Program.DEBUG) Console.WriteLine($"{"[DBG]".Pastel(Color.Orange)} Fetching UnityPackage assetUrl {up.assetUrl}");
                        DataResponse response = FetchData(up.assetUrl);
                        if (!File.Exists($"{outDir}\\{response.fileName}"))
                            File.WriteAllBytes($"{outDir}\\{response.fileName}", response.data);
                    }
                    if (up.pluginUrl != null && up.pluginUrl != "")
                    {
                        //if (Program.DEBUG) Console.WriteLine($"{"[DBG]".Pastel(Color.Orange)} Fetching UnityPackage pluginUrl {up.pluginUrl}");
                        DataResponse response = FetchData(up.pluginUrl);
                        if (!File.Exists($"{outDir}\\{response.fileName}"))
                            File.WriteAllBytes($"{outDir}\\{response.fileName}", response.data);
                    }
                }

                //if (Program.DEBUG) Console.WriteLine($"{"[DBG]".Pastel(Color.Orange)} Compressing worldId: {br.id}_v{world.version}");
                SevenZipCompressor compressor = new SevenZipCompressor() { IncludeEmptyDirectories = true, CompressionLevel = CompressionLevel.Ultra, ArchiveFormat = OutArchiveFormat.SevenZip, CompressionMethod = CompressionMethod.Lzma2, DirectoryStructure = true };
                compressor.CompressDirectory(Path.GetFullPath(outDir), $"{outDir}.7z");

                Console.WriteLine($"{"[INF]".Pastel(Color.Green)} Finished processing worldId: {br.id}\n");
            }
        }

        private JsonGroup FixNoneDatesAndPrettify(string json)
        {
            // Fixes an issue where having "none" would halt the JSON tools when trying to parse a DateTime from date fields
            json = json.Replace("Date\":\"none\"", $"Date\":\"{new DateTime(DateTime.MinValue.Ticks, DateTimeKind.Utc).ToLongTimeString()}\"");

            // So, prettify the output JSON seperately, then undo the above workaround to maintain an accurate output
            string prettyJson = JsonTools.PrettifyJson(json);
            prettyJson = prettyJson.Replace($"Date\": \"{new DateTime(DateTime.MinValue.Ticks, DateTimeKind.Utc).ToLongTimeString()}\"", "Date\": \"none\"");

            return new JsonGroup { fixedJson = json, prettyJson = prettyJson };
        }

        private DataResponse FetchData(string url)
        {
            HttpResponseMessage response = null;
            try
            {
                response = fetcher.GetAsync(url).Result;
            }
            catch (Exception e)
            {
                Console.WriteLine($"{"[ERR]".Pastel(Color.Red)} Could not fetch data from {url} ({$"{e.GetType()}: {e.Message}".Pastel(Color.Red)})");
                return new DataResponse()
                {
                    fileName = Path.GetFileName(url),
                    data = new byte[0]
                };
            }
            if (response != null && response.IsSuccessStatusCode)
            {
                string tempFileName = "";
                if (response.Content.Headers.ContentDisposition != null && response.Content.Headers.ContentDisposition.FileName != null)
                    tempFileName = response.Content.Headers.ContentDisposition.FileName;
                else if (response.RequestMessage.RequestUri.ToString() != null)
                    tempFileName = Path.GetFileName(response.RequestMessage.RequestUri.ToString());
                else
                    tempFileName = Path.GetFileName(url);

                return new DataResponse()
                {
                    fileName = tempFileName,
                    data = response.Content.ReadAsByteArrayAsync().Result
                };
            }
            else
            {
                Console.WriteLine($"{"[ERR]".Pastel(Color.Red)} Could not fetch data, Fetcher returned bad status code ({$"{response.StatusCode}: {response.ReasonPhrase}".Pastel(Color.Red)})");

                string tempFileName = "";
                if (response.Content.Headers.ContentDisposition != null && response.Content.Headers.ContentDisposition.FileName != null)
                    tempFileName = response.Content.Headers.ContentDisposition.FileName;
                else if (response.RequestMessage.RequestUri.ToString() != null)
                    tempFileName = Path.GetFileName(response.RequestMessage.RequestUri.ToString());
                else
                    tempFileName = Path.GetFileName(url);

                return new DataResponse()
                {
                    fileName = tempFileName,
                    data = new byte[0]
                };
            }
        }

        public string GetOutputDir()
        {
            return OUTPUT_DIR;
        }
    }

    internal class DataResponse
    {
        internal string fileName { get; set; }
        internal byte[] data { get; set; }
    }

    internal class JsonGroup
    {
        internal string fixedJson { get; set; }
        internal string prettyJson { get; set; }
    }
}