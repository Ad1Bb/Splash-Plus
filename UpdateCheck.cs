using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SplashAddonPlus
{
    /// <summary>
    /// Tells the user about a new release. At most once a day, after Playnite has started and in the background, asks
    /// GitHub for the latest release of Splash Plus and compares its tag ("2.2") with the installed version (from
    /// extension.yaml next to the add-on). Playnite checks updates itself only for add-ons listed in its add-on
    /// database, so the add-on asks. Sends nothing but the request itself. Whatever goes wrong (offline, a proxy, the
    /// rate limit, an unexpected answer) is only logged: the check can never stop Playnite or a game.
    /// </summary>
    internal static class UpdateCheck
    {
        public const string LatestReleaseApi = "https://api.github.com/repos/Ad1Bb/Splash-Plus/releases/latest";
        public const string ReleasesPage = "https://github.com/Ad1Bb/Splash-Plus/releases/latest";

        /// <summary>Time between two checks.</summary>
        public static readonly TimeSpan Interval = TimeSpan.FromHours(20);

        private const int TimeoutMilliseconds = 10000;

        /// <summary>Answers larger than this are not read (the real one is a few kilobytes).</summary>
        private const int MaxAnswerBytes = 1024 * 1024;

        /// <summary>A version from a tag or a yaml value ("2.1", "v2.1", "2.1.0"); null when it is not one.</summary>
        public static Version Parse(string text)
        {
            Match match = Regex.Match(text ?? string.Empty, @"^\s*[vV]?(\d{1,6})(?:\.(\d{1,6}))?(?:\.(\d{1,6}))?\s*$");
            if (!match.Success)
            {
                return null;
            }

            int Part(int index) => match.Groups[index].Success ? int.Parse(match.Groups[index].Value, CultureInfo.InvariantCulture) : 0;
            return new Version(Part(1), Part(2), Part(3));
        }

        /// <summary>The "Version:" value of an extension.yaml, or null.</summary>
        public static Version FromManifest(string yaml)
        {
            Match match = Regex.Match(yaml ?? string.Empty, @"^Version:[ \t]*['""]?([^'""\r\n]*?)['""]?[ \t]*\r?$", RegexOptions.Multiline);
            return match.Success ? Parse(match.Groups[1].Value) : null;
        }

        /// <summary>The installed version: extension.yaml in the add-on's folder, else the assembly's version.</summary>
        public static Version Installed()
        {
            try
            {
                string folder = Path.GetDirectoryName(typeof(UpdateCheck).Assembly.Location);
                string manifest = string.IsNullOrEmpty(folder) ? null : Path.Combine(folder, "extension.yaml");
                Version version = manifest != null && File.Exists(manifest) ? FromManifest(File.ReadAllText(manifest)) : null;
                if (version != null)
                {
                    return version;
                }
            }
            catch
            {
                // Falls back to the assembly's version.
            }

            Version assembly = typeof(UpdateCheck).Assembly.GetName().Version;
            return new Version(assembly.Major, assembly.Minor, Math.Max(0, assembly.Build));
        }

        /// <summary>The tag shown to the user ("2.2"), or null when the latest release is not newer than the installed version.</summary>
        public static string NewerTag(string latestTag, Version installed)
        {
            Version latest = Parse(latestTag);
            return latest != null && installed != null && latest > installed ? latestTag.Trim().TrimStart('v', 'V') : null;
        }

        /// <summary>The release page to open: GitHub's link when it points to this add-on's releases, else the latest-release page.</summary>
        public static string SafePage(string url)
        {
            return url != null && url.StartsWith("https://github.com/Ad1Bb/Splash-Plus/releases/", StringComparison.OrdinalIgnoreCase)
                && Uri.TryCreate(url, UriKind.Absolute, out _)
                ? url
                : ReleasesPage;
        }

        /// <summary>Whether a check is due, given the time of the last one as saved (round-trip text, UTC); a bad or future value counts as due.</summary>
        public static bool IsDue(string lastCheck, DateTime nowUtc)
        {
            if (!DateTime.TryParse(lastCheck?.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime last))
            {
                return true;
            }

            TimeSpan since = nowUtc - last.ToUniversalTime();
            return since < TimeSpan.Zero || since >= Interval;
        }

        /// <summary>
        /// The tag and page of a newer release from GitHub's answer, or null when there is none; throws on an answer
        /// that is not the expected JSON.
        /// </summary>
        public static Tuple<string, string> Read(string json, Version installed)
        {
            JObject release = JObject.Parse(json);
            string tag = NewerTag(release.Value<string>("tag_name"), installed);
            return tag == null ? null : Tuple.Create(tag, SafePage(release.Value<string>("html_url")));
        }

        /// <summary>
        /// Checks in the background when a check is due (<paramref name="stateFile"/> keeps the time of the last
        /// successful one); <paramref name="notify"/> gets the new version's tag and page. Never throws.
        /// </summary>
        public static Task Start(string stateFile, Action<string, string> notify, Action<string> log, string url = LatestReleaseApi)
        {
            return Task.Run(() =>
            {
                try
                {
                    if (stateFile != null && File.Exists(stateFile) && !IsDue(File.ReadAllText(stateFile), DateTime.UtcNow))
                    {
                        return;
                    }

                    Tuple<string, string> newer = Read(Download(url), Installed());
                    Remember(stateFile, log);
                    if (newer != null)
                    {
                        notify(newer.Item1, newer.Item2);
                    }
                }
                catch (Exception ex)
                {
                    log("Update check skipped: " + ex.GetType().Name + ": " + ex.Message);
                }
            });
        }

        private static string Download(string url)
        {
            // GitHub needs TLS 1.2. Playnite's .NET offers it already; it is added only to an explicit list that lacks
            // it, never by replacing the system default (that would turn off newer protocols for all of Playnite).
            SecurityProtocolType protocols = ServicePointManager.SecurityProtocol;
            if (protocols != SecurityProtocolType.SystemDefault && (protocols & SecurityProtocolType.Tls12) == 0)
            {
                ServicePointManager.SecurityProtocol = protocols | SecurityProtocolType.Tls12;
            }

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "SplashPlus-update-check";
            request.Accept = "application/vnd.github+json";
            request.Timeout = TimeoutMilliseconds;
            request.ReadWriteTimeout = TimeoutMilliseconds;
            request.AllowAutoRedirect = true;
            using (var response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (var buffer = new MemoryStream())
            {
                var chunk = new byte[16384];
                int read;
                while (stream != null && (read = stream.Read(chunk, 0, chunk.Length)) > 0)
                {
                    if (buffer.Length + read > MaxAnswerBytes)
                    {
                        throw new InvalidDataException("answer too large");
                    }

                    buffer.Write(chunk, 0, read);
                }

                return Encoding.UTF8.GetString(buffer.ToArray());
            }
        }

        private static void Remember(string stateFile, Action<string> log)
        {
            if (stateFile == null)
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(stateFile));
                File.WriteAllText(stateFile, DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                log("Could not save the time of the update check: " + ex.Message);
            }
        }
    }
}
