using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SplashAddonPlus
{
    /// <summary>Where the game logo sits on the splash screen.</summary>
    public enum LogoPlacement
    {
        BottomLeft,
        BottomCenter,
        BottomRight,
        Center,
        TopLeft,
        TopRight
    }

    /// <summary>Look of the splash screen.</summary>
    public enum SplashStyle
    {
        /// <summary>Shown as "Custom"; the saved name stays "Classic".</summary>
        Classic,
        Cinema,
        Showcase,
        Gallery,
        Plinth,
        Drawer,
        Band,
        TextMode,
        TextModeDark,
        PopFrost,
        SwingFrost,
        MeetFrost,
        RiseFrost,
        LootDrop,
        LockOn,
        Showdown,
        Focus,
        Slant,
        IrisGloss,
        SlateGloss,

        /// <summary>Not a look of its own: one of the ready-made styles, picked anew for every splash screen.</summary>
        Random
    }

    /// <summary>When the splash screen closes (shown as one choice; stored in AutoDetectFullscreen).</summary>
    public enum CloseMode
    {
        /// <summary>As soon as the game's window is on screen.</summary>
        WhenGameAppears,

        /// <summary>After a fixed time from showing the splash.</summary>
        FixedTime
    }

    /// <summary>Size of the splash screen in Playnite's Desktop mode (Fullscreen mode always uses the whole screen).</summary>
    public enum DesktopSplashSize
    {
        FullScreen,

        /// <summary>A window 75% of the screen's width and height, centered.</summary>
        Large,

        /// <summary>A window 50% of the screen's width and height, centered.</summary>
        Small,

        /// <summary>Exactly where the Playnite window is and as large as it, whatever its proportions.</summary>
        PlayniteWindow
    }

    /// <summary>Where a splash window that does not fill the screen goes, in screen pixels.</summary>
    internal static class SplashWindowPlacement
    {
        /// <summary>Share of the screen's width and height (1 = the whole screen).</summary>
        public static double Fraction(DesktopSplashSize size)
        {
            switch (size)
            {
                case DesktopSplashSize.Large: return 0.75;
                case DesktopSplashSize.Small: return 0.5;
                default: return 1.0;
            }
        }

        /// <summary>
        /// A window with the monitor's proportions at <paramref name="fraction"/> of its size, centered in the work area
        /// (the screen without the taskbar) and never larger than it. Returns x, y, width, height.
        /// </summary>
        public static int[] Compute(int monitorWidth, int monitorHeight, int workLeft, int workTop, int workWidth, int workHeight, double fraction)
        {
            double scale = Math.Min(fraction, Math.Min(workWidth / (double)monitorWidth, workHeight / (double)monitorHeight));
            int width = (int)Math.Round(monitorWidth * scale);
            int height = (int)Math.Round(monitorHeight * scale);
            return new[] { workLeft + (workWidth - width) / 2, workTop + (workHeight - height) / 2, width, height };
        }
    }

    /// <summary>Saves an enum by name; a name it does not know reads as the enum's first value.</summary>
    internal sealed class SafeEnumConverter<T> : StringEnumConverter where T : struct
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return reader.TokenType == JsonToken.String && Enum.TryParse((string)reader.Value, true, out T value)
                && Enum.IsDefined(typeof(T), value)
                ? value
                : default(T);
        }
    }

    /// <summary>One game's splash screen in the per-game table.</summary>
    internal enum GameSplashMode
    {
        /// <summary>As set for all games.</summary>
        Default,

        /// <summary>Closes after the game's own fixed time.</summary>
        FixedTime,

        /// <summary>No splash screen for the game.</summary>
        Off
    }

    /// <summary>The appearance options (logo position and size, dimming, vignette, zoom) are used only by Custom.</summary>
    internal static class StyleOptions
    {
        public static bool Customizable(SplashStyle style) => style == SplashStyle.Classic;
    }

    /// <summary>Style names and the order they are listed in: Custom and Random first, then the ready-made styles A–Z.</summary>
    internal static class StyleList
    {
        /// <summary>Listed above the line: the style you set up yourself, and the random pick.</summary>
        public static readonly SplashStyle[] Special = { SplashStyle.Classic, SplashStyle.Random };

        /// <summary>The ready-made styles in alphabetical order of their names.</summary>
        public static readonly SplashStyle[] Designed = ((SplashStyle[])Enum.GetValues(typeof(SplashStyle)))
            .Where(style => Array.IndexOf(Special, style) < 0)
            .OrderBy(Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        /// <summary>
        /// What Random picks from: the ready-made styles except the two Retro text screens, which look nothing like
        /// the rest and are shown only when chosen.
        /// </summary>
        public static readonly SplashStyle[] RandomPool = Designed
            .Where(style => style != SplashStyle.TextMode && style != SplashStyle.TextModeDark)
            .ToArray();

        public static string Name(SplashStyle style)
        {
            switch (style)
            {
                case SplashStyle.Classic: return Loc.StyleClassic;
                case SplashStyle.Random: return Loc.StyleRandom;
                case SplashStyle.Cinema: return Loc.StyleCinema;
                case SplashStyle.Showcase: return Loc.StyleShowcase;
                case SplashStyle.Gallery: return Loc.StyleGallery;
                case SplashStyle.Plinth: return Loc.StylePlinth;
                case SplashStyle.Drawer: return Loc.StyleDrawer;
                case SplashStyle.Band: return Loc.StyleBand;
                case SplashStyle.TextMode: return Loc.StyleTextMode;
                case SplashStyle.TextModeDark: return Loc.StyleTextModeDark;
                case SplashStyle.PopFrost: return Loc.StylePopFrost;
                case SplashStyle.SwingFrost: return Loc.StyleSwingFrost;
                case SplashStyle.MeetFrost: return Loc.StyleMeetFrost;
                case SplashStyle.RiseFrost: return Loc.StyleRiseFrost;
                case SplashStyle.LootDrop: return Loc.StyleLootDrop;
                case SplashStyle.LockOn: return Loc.StyleLockOn;
                case SplashStyle.Showdown: return Loc.StyleShowdown;
                case SplashStyle.Focus: return Loc.StyleFocus;
                case SplashStyle.Slant: return Loc.StyleSlant;
                case SplashStyle.IrisGloss: return Loc.StyleIrisGloss;
                case SplashStyle.SlateGloss: return Loc.StyleSlateGloss;
                default: return style.ToString();
            }
        }

        /// <summary>
        /// The style to show: <paramref name="style"/> itself, or for Random one from <see cref="RandomPool"/>, never the
        /// same as <paramref name="last"/> (the previous random pick) so two splash screens in a row always differ.
        /// </summary>
        public static SplashStyle Resolve(SplashStyle style, System.Random random, SplashStyle? last)
        {
            if (style != SplashStyle.Random)
            {
                return style;
            }

            SplashStyle[] pool = RandomPool.Where(s => s != last).ToArray();
            return pool[random.Next(pool.Length)];
        }
    }

    /// <summary>Saves the style by name; a name it does not know (a style that was removed) reads as Classic.</summary>
    internal sealed class SplashStyleConverter : StringEnumConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return reader.TokenType == JsonToken.String && Enum.TryParse((string)reader.Value, true, out SplashStyle style)
                && Enum.IsDefined(typeof(SplashStyle), style)
                ? style
                : SplashStyle.Classic;
        }
    }

    /// <summary>One row of the per-platform display times.</summary>
    public class PlatformDuration : ObservableObject
    {
        private string _platform = string.Empty;
        private int _seconds = 10;

        [JsonProperty("Platform")]
        public string Platform { get => _platform; set => SetValue(ref _platform, value, "Platform"); }

        [JsonProperty("Seconds")]
        public int Seconds { get => _seconds; set => SetValue(ref _seconds, value, "Seconds"); }
    }

    /// <summary>One game in the per-game lists of the settings page.</summary>
    internal sealed class GameEntry
    {
        public string Id { get; set; }

        /// <summary>Name from the library, or null when the game is no longer there.</summary>
        public string Name { get; set; }
    }

    internal static class GameEntries
    {
        /// <summary>Games sorted by name; IDs no longer in the library come last.</summary>
        public static List<GameEntry> Sort(IEnumerable<string> ids, Func<string, string> resolveName)
        {
            return (ids ?? Enumerable.Empty<string>())
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(id => new GameEntry { Id = id, Name = SafeName(id, resolveName) })
                .OrderBy(entry => entry.Name == null)
                .ThenBy(entry => entry.Name ?? entry.Id, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        private static string SafeName(string id, Func<string, string> resolveName)
        {
            try
            {
                string name = resolveName?.Invoke(id);
                return string.IsNullOrWhiteSpace(name) ? null : name;
            }
            catch
            {
                return null;
            }
        }
    }
}
