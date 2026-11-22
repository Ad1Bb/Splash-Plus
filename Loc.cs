using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SplashAddonPlus
{
    /// <summary>
    /// All user-facing texts of the add-on in one place. English is the source; the other languages live in
    /// Localization/*.cs. The language follows Playnite's own language setting, with English for the rest.
    /// </summary>
    internal static partial class Loc
    {
        /// <summary>Supported languages in display order, as Playnite language-code prefixes.</summary>
        public static readonly string[] Languages = { "en", "de", "fr", "es", "it", "pt", "pl", "ru", "zh" };

        // Null until SetLanguage (English then). Not "= English": static fields of a partial class have no set
        // initialization order across files, so the table might not exist yet.
        private static Dictionary<string, string> _current;

        /// <summary>The language in use (one of <see cref="Languages"/>).</summary>
        public static string Language { get; private set; } = "en";

        /// <summary>
        /// Picks the language for a Playnite language code such as "pl_PL" or "pt_BR". Unsupported languages,
        /// and Traditional Chinese, get English.
        /// </summary>
        public static void SetLanguage(string playniteLanguage)
        {
            string code = (playniteLanguage ?? string.Empty).Trim().Replace('-', '_');
            string prefix = code.Length >= 2 ? code.Substring(0, 2).ToLowerInvariant() : string.Empty;
            if (prefix == "zh" && !code.Equals("zh_CN", StringComparison.OrdinalIgnoreCase)
                && !code.Equals("zh", StringComparison.OrdinalIgnoreCase))
            {
                prefix = "en";
            }

            Dictionary<string, string> table = Table(prefix);
            Language = table == null ? "en" : prefix;
            _current = table ?? English;
        }

        /// <summary>The texts of one language (see <see cref="Languages"/>), or null when it is not supported.</summary>
        internal static Dictionary<string, string> Table(string language)
        {
            switch (language)
            {
                case "en": return English;
                case "de": return German;
                case "fr": return French;
                case "es": return Spanish;
                case "it": return Italian;
                case "pt": return Portuguese;
                case "pl": return Polish;
                case "ru": return Russian;
                case "zh": return Chinese;
                default: return null;
            }
        }

        private static string T([CallerMemberName] string key = null)
        {
            return _current != null && _current.TryGetValue(key, out string text) ? text : English[key];
        }

        // ----- Style names (the same in every language) -----

        public const string StyleCinema = "Cinema";

        public const string StyleShowcase = "Showcase";

        public const string StyleGallery = "Gallery";

        public const string StylePlinth = "Plinth";

        public const string StyleDrawer = "Drawer";

        public const string StyleBand = "Band";

        public const string StyleTextMode = "Retro Gray";

        public const string StyleTextModeDark = "Retro Black";

        public const string StylePopFrost = "Pane";

        public const string StyleSwingFrost = "Pendant";

        public const string StyleMeetFrost = "Clasp";

        public const string StyleRiseFrost = "Ledge";

        public const string StyleLootDrop = "Loot Drop";

        public const string StyleLockOn = "Lock-On";

        public const string StyleShowdown = "Showdown";

        public const string StyleFocus = "Focus";

        public const string StyleSlant = "Slant";

        public const string StyleIrisGloss = "Iris";

        public const string StyleSlateGloss = "Slate";

        // ----- Translated texts -----

        public static string SectionAutoDetect => T();
        public static string WindowedToggle => T();
        public static string WindowedHint => T();
        public static string MinimumDisplay => T();
        public static string MinimumDisplayHint => T();
        public static string DetectionDelay => T();
        public static string DetectionDelayHint => T();
        public static string IgnoredProcesses => T();
        public static string IgnoredProcessesHint => T();

        public static string SectionClosing => T();
        public static string CloseWhenAppears => T();
        public static string CloseFixed => T();
        public static string FixedTimeSeconds => T();
        public static string MaximumWait => T();
        public static string MaximumWaitHint => T();
        public static string ManualCloseHint => T();

        public static string SectionAppearance => T();
        public static string StyleLabel => T();
        public static string StyleClassic => T();
        public static string StyleClassicHint => T();
        public static string StyleCinemaHint => T();
        public static string StyleShowcaseHint => T();
        public static string StyleGalleryHint => T();
        public static string StylePlinthHint => T();
        public static string StyleDrawerHint => T();
        public static string StyleBandHint => T();
        public static string StyleTextModeHint => T();
        public static string StyleTextModeDarkHint => T();
        public static string StylePopFrostHint => T();
        public static string StyleSwingFrostHint => T();
        public static string StyleMeetFrostHint => T();
        public static string StyleRiseFrostHint => T();
        public static string StyleLootDropHint => T();
        public static string StyleLockOnHint => T();
        public static string StyleShowdownHint => T();
        public static string StyleFocusHint => T();
        public static string StyleSlantHint => T();
        public static string StyleIrisGlossHint => T();
        public static string StyleSlateGlossHint => T();
        public static string Preview => T();
        public static string PreviewTooltip => T();
        public static string PreviewNoGames => T();
        public static string CustomizeStyle => T();
        public static string LogoPositionLabel => T();
        public static string LogoBottomLeft => T();
        public static string LogoBottomCenter => T();
        public static string LogoBottomRight => T();
        public static string LogoCenter => T();
        public static string LogoTopLeft => T();
        public static string LogoTopRight => T();
        public static string LogoWidth => T();
        public static string LogoWidthHint => T();
        public static string BackgroundDim => T();
        public static string VignetteToggle => T();
        public static string ZoomToggle => T();

        public static string Starting => T();
        public static string PleaseWait => T();

        public static string SectionGames => T();
        public static string ExceptionsTip => T();
        public static string GameColumn => T();
        public static string SplashColumn => T();
        public static string GameFixedTime => T();
        public static string GameSplashOff => T();
        public static string NoGames => T();
        public static string UnknownGame(string id) => string.Format(T(), id);
        public static string UpdateAvailable(string version) => string.Format(T(), version);
        public static string UpdateAvailableFullscreen(string version) => string.Format(T(), version);

        public static string SectionAdvanced => T();
        public static string DesktopSplashSize => T();
        public static string SizeFullScreen => T();
        public static string SizeLarge => T();
        public static string SizeSmall => T();
        public static string SizePlayniteWindow => T();
        public static string DesktopSplashSizeHint => T();
        public static string SectionPlatformTimes => T();
        public static string PlatformTimersToggle => T();
        public static string PlatformColumn => T();
        public static string TimeColumn => T();
        public static string PriorityHint => T();
        public static string AddPlatform => T();
        public static string Remove => T();

        public static string SectionDiagnostics => T();
        public static string DiagnosticsToggle => T();
        public static string DiagnosticsHint => T();

        public static string ErrorDuration => T();
        public static string ErrorLogoSize => T();
        public static string ErrorBackgroundDim => T();
        public static string ErrorMinimumDisplay => T();
        public static string ErrorDetectionDelay => T();
        public static string ErrorMaximumWait => T();
        public static string ErrorMaximumBelowMinimum => T();
        public static string ErrorGameDuration(string gameId) => string.Format(T(), gameId);

        public static string MenuDisableOne => T();
        public static string MenuDisableMany => T();
        public static string MenuEnable => T();
        public static string MenuSetTime => T();
        public static string MenuRemoveTime => T();
        public static string MenuPreview => T();
        public static string AskDuration(string gameName, int gameCount) => gameCount == 1
            ? string.Format(T("AskDurationOne"), gameName)
            : T("AskDurationMany");
        public static string AskDurationNote => T();
        public static string InvalidNumber => T();

        // ----- English (the source; every other language has exactly these keys) -----

        private static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            // Settings: game detection
            ["SectionAutoDetect"] = "Game detection",
            ["WindowedToggle"] = "Also detect games running in a window",
            ["WindowedHint"] = "Works for games with a known installation folder (not for emulators).",
            ["MinimumDisplay"] = "Minimum display time (s)",
            ["MinimumDisplayHint"] = "Prevents a quick flash when a game starts almost instantly.",
            ["DetectionDelay"] = "Delay after the game is detected (s)",
            ["DetectionDelayHint"] = "Gives the game time to get past black screens and intros after its window appears.",
            ["IgnoredProcesses"] = "Programs whose windows are never the game (one process name per line)",
            ["IgnoredProcessesHint"] = "Add a launcher here if it closes the splash screen too early. Its process name is in the log (see Diagnostics).",

            // Settings: closing
            ["SectionClosing"] = "Close the splash screen",
            ["CloseWhenAppears"] = "When the game appears (recommended)",
            ["CloseFixed"] = "After a fixed time",
            ["FixedTimeSeconds"] = "Fixed time (s)",
            ["MaximumWait"] = "Maximum waiting time (s)",
            ["MaximumWaitHint"] = "After this time the splash screen closes no matter what.",
            ["ManualCloseHint"] = "Esc, a mouse click or the B button on a gamepad closes the splash screen at any time.",

            // Settings: appearance
            ["SectionAppearance"] = "Appearance",
            ["StyleLabel"] = "Style",
            ["StyleClassic"] = "Custom",
            ["StyleClassicHint"] = "The game's background and logo, set up with the options below.",
            ["StyleCinemaHint"] = "Letterbox bars, drifting backdrop and a title revealed like a film opening.",
            ["StyleShowcaseHint"] = "The cover stands in the middle of the artwork with a reflection and a sheen.",
            ["StyleGalleryHint"] = "The artwork and the cover hang side by side on a dark wall under a spotlight.",
            ["StylePlinthHint"] = "The cover rises over a horizon line onto a frosted floor below the artwork.",
            ["StyleDrawerHint"] = "A frosted glass panel slides out from behind the cover over the artwork.",
            ["StyleBandHint"] = "A frosted strip spans the artwork; the cover cuts across it.",
            ["StyleTextModeHint"] = "Retro text-mode setup screen over a blur of the game's colors.",
            ["StyleTextModeDarkHint"] = "Black text-mode setup screen over a blur of the game's colors.",
            ["StylePopFrostHint"] = "Centered cover and title on a frosted glass pane over the sharp artwork.",
            ["StyleSwingFrostHint"] = "A hanging cover and its title on one frosted glass pane over the sharp artwork.",
            ["StyleMeetFrostHint"] = "Cover and details slide in from both sides onto a frosted glass pane.",
            ["StyleRiseFrostHint"] = "A frosted glass strip along the bottom; the cover rises above it and hovers.",
            ["StyleLootDropHint"] = "The cover drops in a beam of light and flips like a rare item card.",
            ["StyleLockOnHint"] = "Targeting brackets lock onto the cover; HUD readouts beside it.",
            ["StyleShowdownHint"] = "Boss-intro title card: a diagonal band, speed lines and a slamming title.",
            ["StyleFocusHint"] = "The game's art pulls into focus; a frosted glass pill holds the title.",
            ["StyleSlantHint"] = "Two parallel glass slabs knock together; the cover sits at one tip.",
            ["StyleIrisGlossHint"] = "The art pulls into focus; the cover sits in a glossy smoked-glass disc.",
            ["StyleSlateGlossHint"] = "The art pulls into focus; a glossy smoked-glass slate slides in from the left.",
            ["Preview"] = "Preview",
            ["PreviewTooltip"] = "Shows the splash screen with a random game from your library.",
            ["PreviewNoGames"] = "Add a game to your library to see a preview.",
            ["CustomizeStyle"] = "Customize this style",
            ["LogoPositionLabel"] = "Logo position",
            ["LogoBottomLeft"] = "Bottom left",
            ["LogoBottomCenter"] = "Bottom center",
            ["LogoBottomRight"] = "Bottom right",
            ["LogoCenter"] = "Center",
            ["LogoTopLeft"] = "Top left",
            ["LogoTopRight"] = "Top right",
            ["LogoWidth"] = "Logo size",
            ["LogoWidthHint"] = "0% hides the logo.",
            ["BackgroundDim"] = "Background dimming",
            ["VignetteToggle"] = "Darken the edges (vignette)",
            ["ZoomToggle"] = "Slow zoom on the background",

            // Splash screen texts (Text Mode)
            ["Starting"] = "Starting...",
            ["PleaseWait"] = "Please wait",

            // Settings: games with their own settings
            ["SectionGames"] = "Games with their own settings",
            ["ExceptionsTip"] = "Add games by right-clicking them in the library and opening \"Splash Plus\".",
            ["GameColumn"] = "Game",
            ["SplashColumn"] = "Splash screen",
            ["GameFixedTime"] = "Fixed time",
            ["GameSplashOff"] = "Off",
            ["NoGames"] = "None yet.",
            ["UnknownGame"] = "Unknown game ({0})",
            ["UpdateAvailable"] = "Splash Plus {0} is available. Click to open the download page.",
            ["UpdateAvailableFullscreen"] = "Splash Plus {0} is available on GitHub.",

            // Settings: advanced
            ["SectionAdvanced"] = "Advanced",
            ["DesktopSplashSize"] = "Splash screen size in Desktop mode",
            ["SizeFullScreen"] = "Full screen",
            ["SizeLarge"] = "Large window (75% of the screen)",
            ["SizeSmall"] = "Small window (50% of the screen)",
            ["SizePlayniteWindow"] = "Same as the Playnite window",
            ["DesktopSplashSizeHint"] = "Only for Desktop mode. Fullscreen mode is not affected: there the splash screen always fills the screen.",
            ["SectionPlatformTimes"] = "Fixed time per platform",
            ["PlatformTimersToggle"] = "Separate times per platform",
            ["PlatformColumn"] = "Platform",
            ["TimeColumn"] = "Time (s)",
            ["PriorityHint"] = "Priority: the game's own time, then the platform time, then the fixed time.",
            ["AddPlatform"] = "Add platform",
            ["Remove"] = "Remove",

            // Settings: diagnostics
            ["SectionDiagnostics"] = "Diagnostics",
            ["DiagnosticsToggle"] = "Write details to the log file",
            ["DiagnosticsHint"] = "Turn on only when troubleshooting. The log is %AppData%\\Playnite\\extensions.log (portable: in the Playnite folder).",

            // Validation
            ["ErrorDuration"] = "The fixed time must be greater than 0.",
            ["ErrorLogoSize"] = "The logo size must be between 0 and 90%.",
            ["ErrorBackgroundDim"] = "The background dimming must be between 0 and 90%.",
            ["ErrorMinimumDisplay"] = "The minimum display time cannot be negative.",
            ["ErrorDetectionDelay"] = "The delay after the game is detected cannot be negative.",
            ["ErrorMaximumWait"] = "The maximum waiting time must be greater than 0.",
            ["ErrorMaximumBelowMinimum"] = "The maximum waiting time cannot be shorter than the minimum display time.",
            ["ErrorGameDuration"] = "The time for game ID \"{0}\" must be greater than 0.",

            // Game context menu
            ["MenuDisableOne"] = "Turn off the splash screen for this game",
            ["MenuDisableMany"] = "Turn off the splash screen for the selected games",
            ["MenuEnable"] = "Turn the splash screen back on",
            ["MenuSetTime"] = "Set a fixed display time…",
            ["MenuRemoveTime"] = "Remove the fixed time",
            ["MenuPreview"] = "Show splash screen preview",
            ["AskDurationOne"] = "How many seconds should the splash screen last for \"{0}\"?",
            ["AskDurationMany"] = "How many seconds should the splash screen last for the selected games?",
            ["AskDurationNote"] = "Automatic detection is not used for games with a fixed time.",
            ["InvalidNumber"] = "Enter a whole number greater than zero."
        };
    }
}
