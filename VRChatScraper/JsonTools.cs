using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace VRChatScraper
{
    /// <summary>
    /// Represents a collection of static utilities for handling JSON.
    /// </summary>
    public class JsonTools
    {
        /// <summary>
        /// Parses a JSON response to a corresponding class.
        /// </summary>
        /// <typeparam name="T">A VRC API class.</typeparam>
        /// <param name="rawJson">The raw JSON of a request response.</param>
        /// <returns>An object representation of the respective JSON.</returns>
        public static T ParseJson<T>(string rawJson)
        where T : class
        {
            return JsonConvert.DeserializeObject<T>(rawJson);
        }

        /// <summary>
        /// Prettifies a JSON response into a more human-readable string.
        /// </summary>
        /// <param name="rawJson">The raw JSON of a request response.</param>
        /// <returns>A prettified JSON representation.</returns>
        public static string PrettifyJson(string rawJson)
        {
            return JToken.Parse(rawJson).ToString();
        }

        /// <summary>
        /// Fixes "none" dates in a JSON response in order to avoid crashing. (Hacky)
        /// </summary>
        /// <param name="json">The JSON response to be fixed.</param>
        /// <returns>A "none" fixed JSON response.</returns>
        public static JsonGroup FixNoneDatesAndPrettify(string json)
        {
            // Fixes an issue where having "none" would halt the JSON tools when trying to parse a DateTime from date fields
            json = json.Replace("Date\":\"none\"", $"Date\":\"{new DateTime(DateTime.MinValue.Ticks, DateTimeKind.Utc).ToLongTimeString()}\"");

            // So, prettify the output JSON seperately, then undo the above workaround to maintain an accurate output
            string prettyJson = PrettifyJson(json);
            prettyJson = prettyJson.Replace($"Date\": \"{new DateTime(DateTime.MinValue.Ticks, DateTimeKind.Utc).ToLongTimeString()}\"", "Date\": \"none\"");

            return new JsonGroup { FixedJson = json, PrettyJson = prettyJson };
        }
    }

    /// <summary>
    /// Represents a pair of JSON responses: one fixed, one prettified.
    /// </summary>
    public class JsonGroup
    {
        internal string FixedJson;
        internal string PrettyJson;
    }
}
