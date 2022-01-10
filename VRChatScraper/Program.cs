using Pastel;
using System;
using System.Drawing;
using System.IO;

namespace VRChatScraper
{
    public class Program
    {
        /// <summary>
        /// Application entry point.
        /// </summary>
        /// <param name="args">Program arguments.</param>
        public static void Main(string[] args)
        {
            // Fancy logo text
            Console.WriteLine($"\n{"VR".PastelBg(Color.White).Pastel(Color.Black)}{"Chat".Pastel(Color.White)}{"Scraper".Pastel(Color.Salmon)} v{Globals.Version} (C) 2021 {"Resistiv".Pastel(Color.Magenta)}");

            // Pass all arguments to be resolved by our handler, thus instantiating our API interaction client
            ApiClient client = ArgumentHandler.Resolve(args);

            // Make sure we have proper output destinations
            InitializeOutputDirs();

            // Add a new Scraper to our client
            Scraper scraper = client.GenerateScraper();
        }

        /// <summary>
        /// Creates output directories for asset storage.
        /// </summary>
        private static void InitializeOutputDirs()
        {
            try
            {
                if (!Directory.Exists($"{Globals.BaseOutputDirectory}"))
                    Directory.CreateDirectory($"{Globals.BaseOutputDirectory}");
                if (!Directory.Exists($"{Globals.BaseOutputDirectory}\\Worlds"))
                    Directory.CreateDirectory($"{Globals.BaseOutputDirectory}\\Worlds");
                if (!Directory.Exists($"{Globals.BaseOutputDirectory}\\Avatars"))
                    Directory.CreateDirectory($"{Globals.BaseOutputDirectory}\\Avatars");
            }
            catch
            {
                Terminate($"Encountered an error while creating output directories, aborting.");
            }
        }

        /// <summary>
        /// Logs a usage statement to output.
        /// </summary>
        /// <param name="verbose">If true, logs a detailed manual page alongside the usage statement.</param>
        public static void Usage(bool verbose)
        {
            Console.WriteLine($"Usage: {"VR".PastelBg(Color.White).Pastel(Color.Black)}{"Chat".Pastel(Color.White)}{"Scraper".Pastel(Color.Salmon)} {"-l".Pastel(Color.LightGreen)} {"username password".Pastel(Color.Orange)} {{{"-a".Pastel(Color.LightGreen)} {"id".Pastel(Color.Orange)} | {"-w".Pastel(Color.LightGreen)} {{{"id".Pastel(Color.Orange)} | [{"list_filters".Pastel(Color.DeepSkyBlue)}]}}}} [{"processing_options".Pastel(Color.DeepSkyBlue)}]");
            if (verbose)
                Console.WriteLine($"\n{"General Arguments".Pastel(Color.DeepSkyBlue)}:\n" +
                                  $"  {"-h".Pastel(Color.LightGreen)} | {"--help".Pastel(Color.LightGreen)}\t\t\t\tDisplay a verbose help message (hey, you're here!)\n" +
                                  $"  {"-l".Pastel(Color.LightGreen)} | {"--login".Pastel(Color.LightGreen)} {{{"username password".Pastel(Color.Orange)}}}\t{"VR".PastelBg(Color.White).Pastel(Color.Black)}{"Chat".Pastel(Color.White)} credentials, required to access API endpoints\n" +
                                  $"  {"-a".Pastel(Color.LightGreen)} | {"--avatar".Pastel(Color.LightGreen)} {{{"id".Pastel(Color.Orange)}}}\t\t\tGet an avatar by ID ({"listing avatars is deprecated through the ".Pastel(Color.Red)}{"VR".PastelBg(Color.White).Pastel(Color.Black)}{"Chat".Pastel(Color.White)}{" API".Pastel(Color.Red)})\n" +
                                  $"  {"-w".Pastel(Color.LightGreen)} | {"--world".Pastel(Color.LightGreen)} {{{"id".Pastel(Color.Orange)} | [{"list_filters".Pastel(Color.DeepSkyBlue)}]}}\tGet a world or list of worlds\n" +

                                  $"\n{"World List Filters".Pastel(Color.DeepSkyBlue)}:\n" +
                                  $"  {"-f".Pastel(Color.LightGreen)} | {"--featured".Pastel(Color.LightGreen)} {{{"true".Pastel(Color.MediumPurple)} | {"false".Pastel(Color.MediumPurple)}}}\t\t\tReturns or excludes featured worlds\n" +
                                  $"  {"-s".Pastel(Color.LightGreen)} | {"--sort".Pastel(Color.LightGreen)} {{{"popularity".Pastel(Color.MediumPurple)} | {"created".Pastel(Color.MediumPurple)}\t\t\tSort worlds by specified method\n" +
                                  $"  \t\t| {"updated".Pastel(Color.MediumPurple)} | {"order".Pastel(Color.MediumPurple)} | {"_created_at".Pastel(Color.MediumPurple)}\n" +
                                  $"  \t\t| {"_updated_at".Pastel(Color.MediumPurple)} | {"heat".Pastel(Color.MediumPurple)} | {"trust".Pastel(Color.MediumPurple)}\n" +
                                  $"  \t\t| {"shuffle".Pastel(Color.MediumPurple)} | {"random".Pastel(Color.MediumPurple)} | {"favorites".Pastel(Color.MediumPurple)}\n" +
                                  $"  \t\t| {"reportScore".Pastel(Color.MediumPurple)} | {"reportCount".Pastel(Color.MediumPurple)}\n" +
                                  $"  \t\t| {"publicationDate".Pastel(Color.MediumPurple)} | {"magic".Pastel(Color.MediumPurple)}\n" +
                                  $"  \t\t| {"labsPublicationDate".Pastel(Color.MediumPurple)} | {"relevance".Pastel(Color.MediumPurple)}}}\t\t\t\n" +
                                  $"  {"-u".Pastel(Color.LightGreen)} | {"--user_id".Pastel(Color.LightGreen)} {{{"id".Pastel(Color.Orange)}}}\t\t\t\t\tRetrieve worlds by a particular creator\n" +
                                  $"  {"-n".Pastel(Color.LightGreen)} | {"--number".Pastel(Color.LightGreen)} {{{"number".Pastel(Color.Orange)}}}\t\t\t\tNumber of worlds to fetch\n" +
                                  $"  {"-o".Pastel(Color.LightGreen)} | {"--order".Pastel(Color.LightGreen)} {{{"ascending".Pastel(Color.MediumPurple)} | {"descending".Pastel(Color.MediumPurple)}}}\t\t\tHow to sort the list of worlds\n" +
                                  $"  {"-i".Pastel(Color.LightGreen)} | {"--offset".Pastel(Color.LightGreen)} {{{"offset".Pastel(Color.Orange)}}}\t\t\t\tNumber of worlds to skip past\n" +
                                  $"  {"-q".Pastel(Color.LightGreen)} | {"--search".Pastel(Color.LightGreen)} {{{"search".Pastel(Color.Orange)}}}\t\t\t\tFilter worlds by name\n" +
                                  $"  {"-t".Pastel(Color.LightGreen)} | {"--tags".Pastel(Color.LightGreen)} {{{"tag1".Pastel(Color.Orange)},{"tag2".Pastel(Color.Orange)},{"...".Pastel(Color.Orange)}}}\t\t\t\tFilter worlds by tags (comma-separated, no spaces)\n" +
                                  $"  {"-e".Pastel(Color.LightGreen)} | {"--exclude_tags".Pastel(Color.LightGreen)} {{{"tag1".Pastel(Color.Orange)},{"tag2".Pastel(Color.Orange)},{"...".Pastel(Color.Orange)}}}\t\t\tFilter worlds by excluding tags (comma-separated, no spaces)\n" +
                                  $"  {"-r".Pastel(Color.LightGreen)} | {"--release_status".Pastel(Color.LightGreen)} {{{"public".Pastel(Color.MediumPurple)} | {"private".Pastel(Color.MediumPurple)}\t\tFilter by the release status of worlds\n" +
                                  $"  \t\t| {"hidden".Pastel(Color.MediumPurple)} | {"all".Pastel(Color.MediumPurple)}}}\n" +
                                  $"  {"-p".Pastel(Color.LightGreen)} | {"--platform".Pastel(Color.LightGreen)} {{{"android".Pastel(Color.MediumPurple)} | {"standalonewindows".Pastel(Color.MediumPurple)}}}\t\tFilter by platform compatibility\n" +

                                  $"\n{"Processing Options".Pastel(Color.DeepSkyBlue)}:\n" +
                                  $"  {"-v".Pastel(Color.LightGreen)} | {"--scrape_avatars".Pastel(Color.LightGreen)}\t\t\tUpon downloading a world, open the Unity asset bundle and attempt to find\n" +
                                  $"\t\t\t\t\tand download avatars from pedestals in the world (experimental)\n" +
                                  $"  {"-c".Pastel(Color.LightGreen)} | {"--scrape_user_avatar".Pastel(Color.LightGreen)}\t\tUpon getting world information, fetch the creator's avatar as well");
            Terminate(null);
        }

        /// <summary>
        /// Terminate the entire application.
        /// </summary>
        /// <param name="errorString">If not null, logs an error to output.</param>
        public static void Terminate(string errorString)
        {
            if (errorString != null) Logger.Log(LogType.Error, errorString);
            Environment.Exit(1);
        }
    }
}
