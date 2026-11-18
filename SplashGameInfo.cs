using System;
using System.Collections.Generic;
using System.Linq;
using Playnite.SDK.Models;

namespace SplashAddonPlus
{
    /// <summary>
    /// Game details shown by the splash styles, read once when the splash opens
    /// (Playnite may update some of them, such as the play time, after the game starts).
    /// Contains no WPF types, so it can be tested without Windows.
    /// </summary>
    internal sealed class SplashGameInfo
    {
        public string Name { get; set; } = string.Empty;
        public int? ReleaseYear { get; set; }
        public string Developer { get; set; }
        public string Genre { get; set; }
        public string Platform { get; set; }

        /// <summary>The library the game comes from (such as Steam).</summary>
        public string Library { get; set; }

        /// <summary>Total play time in seconds.</summary>
        public ulong Playtime { get; set; }

        /// <summary>The first score above 0 of critic, community and user score (0–100), or null.</summary>
        public int? Score { get; set; }

        /// <summary>Reads the details of a game; fields that cannot be read stay empty.</summary>
        public static SplashGameInfo FromGame(Game game, string platform)
        {
            var info = new SplashGameInfo { Platform = platform };
            if (game == null)
            {
                return info;
            }

            info.Name = Safe(() => game.Name) ?? string.Empty;
            info.ReleaseYear = Safe(() => game.ReleaseDate?.Year);
            info.Developer = Safe(() => game.Developers?.FirstOrDefault()?.Name);
            info.Genre = Safe(() => game.Genres?.FirstOrDefault()?.Name);
            info.Library = Safe(() => game.Source?.Name);
            info.Playtime = Safe(() => game.Playtime);
            info.Score = Safe(() => new[] { game.CriticScore, game.CommunityScore, game.UserScore }.FirstOrDefault(s => s > 0));
            return info;
        }

        // Lists such as genres are resolved through the database and may fail outside Playnite.
        private static T Safe<T>(Func<T> read)
        {
            try
            {
                return read();
            }
            catch
            {
                return default(T);
            }
        }
    }

    /// <summary>Text formats used by the splash styles.</summary>
    internal static class SplashText
    {
        /// <summary>"42h 10m", "10m", or only hours from 100 h; null when nothing was played.</summary>
        public static string Playtime(ulong seconds)
        {
            if (seconds == 0)
            {
                return null;
            }

            ulong minutes = seconds / 60;
            ulong hours = minutes / 60;
            if (hours >= 100)
            {
                return $"{hours}h";
            }

            return hours > 0 ? $"{hours}h {minutes % 60}m" : $"{Math.Max(1UL, minutes)}m";
        }

        /// <summary>Whole hours played ("42h"), "&lt;1h" under an hour; null when nothing was played.</summary>
        public static string Hours(ulong seconds)
        {
            if (seconds == 0)
            {
                return null;
            }

            return seconds < 3600 ? "<1h" : $"{seconds / 3600}h";
        }

        /// <summary>Filled rating marks out of 5 for a 0–100 score (score / 20, rounded); null without a score.</summary>
        public static int? RatingMarks(int? score)
        {
            if (score == null || score <= 0)
            {
                return null;
            }

            return (int)Math.Max(0, Math.Min(5, Math.Round(score.Value / 20.0, MidpointRounding.AwayFromZero)));
        }

        /// <summary>
        /// Width / height of a sharp cover: the image's own proportions limited to 0.55–1.0 (beyond that the
        /// image is cropped to the limit); 2:3 when unknown.
        /// </summary>
        public static double CoverAspect(double? imageAspect)
        {
            if (imageAspect == null || double.IsNaN(imageAspect.Value) || imageAspect <= 0)
            {
                return 2.0 / 3.0;
            }

            return Math.Max(0.55, Math.Min(1.0, imageAspect.Value));
        }

        /// <summary>The non-empty parts joined with the separator, or null when all are empty.</summary>
        public static string Join(string separator, params string[] parts)
        {
            List<string> present = parts.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
            return present.Count == 0 ? null : string.Join(separator, present);
        }

        /// <summary>Cinema: height of each letterbox bar as a share of the screen height.</summary>
        public static double LetterboxShare(double width, double height)
        {
            if (width <= 0 || height <= 0)
            {
                return 0.04;
            }

            double share = (1 - width / height / 2.39) / 2;
            return Math.Max(0.04, Math.Min(0.13, share));
        }
    }

    /// <summary>
    /// Loot Drop: the "rarity" color of a game, the dominant saturated hue of a small copy of its cover (or
    /// background), brightened to a light accent. Neutral light grey when the image has too little color.
    /// </summary>
    internal static class RarityTint
    {
        /// <summary>#DCDCDC, the "common item" color.</summary>
        public const int Neutral = 0xDCDCDC;

        /// <summary>
        /// The tint (0xRRGGBB) of straight (not premultiplied) BGRA pixels; fully transparent pixels count as colorless.
        /// </summary>
        public static int FromPixels(byte[] bgra, int pixelCount)
        {
            if (bgra == null || pixelCount <= 0 || bgra.Length < pixelCount * 4)
            {
                return Neutral;
            }

            var weights = new double[12];
            var reds = new double[12];
            var greens = new double[12];
            var blues = new double[12];
            for (int i = 0; i < pixelCount; i++)
            {
                if (bgra[4 * i + 3] == 0)
                {
                    continue;
                }

                double b = bgra[4 * i] / 255.0, g = bgra[4 * i + 1] / 255.0, r = bgra[4 * i + 2] / 255.0;
                double max = Math.Max(r, Math.Max(g, b));
                double min = Math.Min(r, Math.Min(g, b));
                if (max < 0.18)
                {
                    continue;
                }

                double sat = (max - min) / max;
                if (sat < 0.18)
                {
                    continue;
                }

                int bin = Math.Min(11, (int)Math.Floor(Hue(r, g, b, max, min) * 12));
                double weight = sat * max * max;
                weights[bin] += weight;
                reds[bin] += weight * r;
                greens[bin] += weight * g;
                blues[bin] += weight * b;
            }

            if (weights.Sum() <= 0.03 * pixelCount)
            {
                return Neutral;
            }

            int best = Array.IndexOf(weights, weights.Max());
            double w = weights[best];
            RgbToHsl(reds[best] / w, greens[best] / w, blues[best] / w, out double h, out double s, out double _);
            HslToRgb(h, Math.Max(0.4, Math.Min(0.8, s)), 0.70, out double ro, out double go, out double bo);
            return (ToByte(ro) << 16) | (ToByte(go) << 8) | ToByte(bo);
        }

        private static int ToByte(double v) => (int)Math.Round(Math.Max(0, Math.Min(1, v)) * 255);

        /// <summary>Hue 0–1 (0 = red).</summary>
        private static double Hue(double r, double g, double b, double max, double min)
        {
            double d = max - min;
            if (d <= 0)
            {
                return 0;
            }

            double h;
            if (max == r)
            {
                h = (g - b) / d;
                if (h < 0)
                {
                    h += 6;
                }
            }
            else if (max == g)
            {
                h = (b - r) / d + 2;
            }
            else
            {
                h = (r - g) / d + 4;
            }

            return h / 6;
        }

        private static void RgbToHsl(double r, double g, double b, out double h, out double s, out double l)
        {
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            l = (max + min) / 2;
            double d = max - min;
            h = Hue(r, g, b, max, min);
            s = d <= 0 ? 0 : d / (1 - Math.Abs(2 * l - 1));
        }

        private static void HslToRgb(double h, double s, double l, out double r, out double g, out double b)
        {
            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double hp = h * 6;
            double x = c * (1 - Math.Abs(hp % 2 - 1));
            double r1 = 0, g1 = 0, b1 = 0;
            switch ((int)Math.Floor(hp) % 6)
            {
                case 0: r1 = c; g1 = x; break;
                case 1: r1 = x; g1 = c; break;
                case 2: g1 = c; b1 = x; break;
                case 3: g1 = x; b1 = c; break;
                case 4: r1 = x; b1 = c; break;
                default: r1 = c; b1 = x; break;
            }

            double m = l - c / 2;
            r = r1 + m;
            g = g1 + m;
            b = b1 + m;
        }
    }
}
