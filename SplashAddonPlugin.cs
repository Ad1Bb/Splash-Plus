using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;

namespace SplashAddonPlus
{
    public class SplashAddonPlugin : GenericPlugin
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        private const string MenuSection = "Splash Plus";

        // Settings folder of the original Splash Addon by Artzox; imported once so switching keeps the configuration.
        private const string OriginalAddonGuid = "550e8400-e29b-41d4-a716-446655440000";
        private const int PreviewSeconds = 4;

        private readonly SplashAddonSettings _settings;
        private SplashSession _session;

        public override Guid Id { get; } = Guid.Parse("586357a7-11fe-488d-999e-ad9cb2070621");

        public SplashAddonPlugin(IPlayniteAPI api)
            : base(api)
        {
            // Playnite restarts when its language changes, so reading it once is enough.
            try
            {
                Loc.SetLanguage(api.ApplicationSettings?.Language);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not read Playnite's language; using English.");
            }

            _settings = LoadPluginSettings<SplashAddonSettings>() ?? ImportOriginalSettings() ?? new SplashAddonSettings();
            _settings.Upgrade();
            _settings.SaveRequested = SaveSettings;
            Properties = new GenericPluginProperties { HasSettings = true };
        }

        /// <summary>
        /// First start of Splash Plus: reuse the configuration of the original Splash Addon
        /// (or of a modified build that still used its ID), if there is one.
        /// </summary>
        private SplashAddonSettings ImportOriginalSettings()
        {
            try
            {
                string path = Path.Combine(PlayniteApi.Paths.ExtensionsDataPath, OriginalAddonGuid, "config.json");
                if (!File.Exists(path))
                {
                    return null;
                }

                var imported = JsonConvert.DeserializeObject<SplashAddonSettings>(File.ReadAllText(path));
                if (imported == null)
                {
                    return null;
                }

                SavePluginSettings(imported);
                Logger.Info("Imported settings from the original Splash Addon.");
                return imported;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not import settings from the original Splash Addon.");
                return null;
            }
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return _settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new SplashAddonSettingsView(BuildPlatformList(), ResolveGameName, PreviewRandomGame) { DataContext = _settings };
        }

        private static readonly Random PreviewPick = new Random();

        /// <summary>
        /// The settings page's Preview button and Extensions → Splash Plus → Default style preview: the splash screen
        /// with a random game (one with artwork when there is any), using the settings as they are being edited, in the
        /// default style even when that game has its own.
        /// </summary>
        private void PreviewRandomGame()
        {
            RunOnUi(() =>
            {
                List<Game> games = PlayniteApi.Database.Games.ToList();
                List<Game> withArt = games.Where(g => !string.IsNullOrEmpty(g.BackgroundImage) || !string.IsNullOrEmpty(g.CoverImage)).ToList();
                List<Game> pool = withArt.Count > 0 ? withArt : games;
                if (pool.Count == 0)
                {
                    PlayniteApi.Dialogs.ShowMessage(Loc.PreviewNoGames, MenuSection);
                    return;
                }

                StartSplash(pool[PreviewPick.Next(pool.Count)], null, preview: true, defaultStyle: true);
            });
        }

        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            base.OnApplicationStarted(args);
            try
            {
                // In Fullscreen mode (often a TV and a gamepad) the notice only informs: opening a web browser there
                // could leave someone without a mouse stuck outside Playnite.
                bool fullscreen = PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen;
                string stateFile = Path.Combine(GetPluginUserDataPath(), "update-check.txt");
                UpdateCheck.Start(stateFile, (tag, page) => ShowUpdateNotice(tag, page, fullscreen), message => Logger.Info(message));
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not start the update check.");
            }

            ScheduleWarmUp();
        }

        /// <summary>
        /// A few seconds after Playnite starts, when it is idle: compiles the add-on's code in the background, then
        /// prepares the chosen style once off screen (fonts, effects), so the first splash appears as fast as the
        /// later ones. Nothing is shown; any failure only skips it.
        /// </summary>
        private void ScheduleWarmUp()
        {
            try
            {
                System.Windows.Threading.Dispatcher dispatcher = Application.Current?.Dispatcher;
                if (dispatcher == null)
                {
                    return;
                }

                var timer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.ApplicationIdle, dispatcher)
                {
                    Interval = TimeSpan.FromSeconds(8)
                };
                timer.Tick += (s, e) =>
                {
                    timer.Stop();
                    var started = System.Diagnostics.Stopwatch.StartNew();
                    System.Threading.Tasks.Task.Run(() => CodeWarmUp.CompileAll(typeof(SplashAddonPlugin).Assembly)).ContinueWith(t =>
                        dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, (Action)(() =>
                        {
                            if (_session == null)
                            {
                                SplashSession.WarmUp(StyleList.Resolve(_settings.Style, new Random(), null));
                            }

                            LogInfo($"Warm-up done in {started.ElapsedMilliseconds} ms.");
                        })));
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                Logger.Info("Warm-up skipped: " + ex.Message);
            }
        }

        /// <summary>The notice about a new release, added on the UI thread without waiting for it (never blocks the check).</summary>
        private void ShowUpdateNotice(string tag, string page, bool fullscreen)
        {
            void Add()
            {
                try
                {
                    PlayniteApi.Notifications.Add(fullscreen
                        ? new NotificationMessage("SplashPlus_Update", Loc.UpdateAvailableFullscreen(tag), NotificationType.Info)
                        : new NotificationMessage("SplashPlus_Update", Loc.UpdateAvailable(tag), NotificationType.Info, () => OpenPage(page)));
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Could not show the update notice.");
                }
            }

            System.Windows.Threading.Dispatcher dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.HasShutdownStarted)
            {
                dispatcher.BeginInvoke((Action)Add);
            }
        }

        private static void OpenPage(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(url);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not open " + url);
            }
        }

        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
            SaveSettings();
            base.OnApplicationStopped(args);
        }

        // ---------------------------------------------------------------- game events

        public override void OnGameStarting(OnGameStartingEventArgs args)
        {
            var sinceRequest = System.Diagnostics.Stopwatch.StartNew();
            RunOnUi(() => StartSplash(args.Game, args.SourceAction, preview: false, sinceRequest));
        }

        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
            RunOnUi(() => CloseSplashFor(args.Game, "game stopped"));
        }

        public override void OnGameStartupCancelled(OnGameStartupCancelledEventArgs args)
        {
            RunOnUi(() => CloseSplashFor(args.Game, "startup cancelled"));
        }

        // ---------------------------------------------------------------- context menu

        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            var items = new List<GameMenuItem>();
            if (args?.Games == null || args.Games.Count == 0)
            {
                return items;
            }

            bool single = args.Games.Count == 1;
            List<string> ids = args.Games.Select(g => g.Id.ToString()).ToList();

            if (ids.Any(id => !_settings.IsExcluded(id)))
            {
                items.Add(new GameMenuItem
                {
                    MenuSection = MenuSection,
                    Description = single ? Loc.MenuDisableOne : Loc.MenuDisableMany,
                    Action = a => UpdateSettings(() => _settings.SetExcluded(IdsOf(a), true))
                });
            }

            if (ids.Any(_settings.IsExcluded))
            {
                items.Add(new GameMenuItem
                {
                    MenuSection = MenuSection,
                    Description = Loc.MenuEnable,
                    Action = a => UpdateSettings(() => _settings.SetExcluded(IdsOf(a), false))
                });
            }

            items.Add(new GameMenuItem
            {
                MenuSection = MenuSection,
                Description = Loc.MenuSetTime,
                Action = a => AskForGameDuration(a.Games)
            });

            if (ids.Any(_settings.HasGameSpecificDuration))
            {
                items.Add(new GameMenuItem
                {
                    MenuSection = MenuSection,
                    Description = Loc.MenuRemoveTime,
                    Action = a => UpdateSettings(() => _settings.SetGameSpecificDuration(IdsOf(a), null))
                });
            }

            items.AddRange(StyleMenu(ids));

            if (single)
            {
                items.Add(new GameMenuItem
                {
                    MenuSection = MenuSection,
                    Description = Loc.MenuPreview,
                    Action = a => RunOnUi(() => StartSplash(a.Games[0], null, preview: true))
                });
            }

            return items;
        }

        // ---------------------------------------------------------------- main menu

        /// <summary>
        /// Extensions → Splash Plus (Desktop mode's main menu and Fullscreen mode's extensions menu), on purpose just
        /// two items whose names say they are about the default: the default style (games with their own keep it) and
        /// a preview of it with a random game. The current style is ticked; Playnite asks for the items each time the
        /// menu opens.
        /// </summary>
        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            var items = new List<MainMenuItem>();
            try
            {
                bool desktop = PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop;
                string section = "@" + MenuSection;
                string styles = section + "|" + Loc.MenuDefaultStyle;

                MainMenuItem Style(SplashStyle style) => new MainMenuItem
                {
                    MenuSection = styles,
                    Description = (_settings.Style == style ? "✓ " : string.Empty) + StyleList.Name(style),
                    Action = a => UpdateSettings(() => _settings.Style = style)
                };

                items.AddRange(StyleList.Special.Select(Style));

                // Fullscreen mode's menu may not draw separator lines, so it gets none.
                if (desktop)
                {
                    items.Add(new MainMenuItem { MenuSection = styles, Description = "-" });
                }

                items.AddRange(StyleList.Designed.Select(Style));
                items.Add(new MainMenuItem { MenuSection = section, Description = Loc.MenuDefaultStylePreview, Action = a => PreviewRandomGame() });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Could not build the Splash Plus menu.");
            }

            return items;
        }

        /// <summary>
        /// The "Style" submenu: Default (the style from the settings), Custom and Random, a line, then the ready-made
        /// styles A–Z. The style the selected games share is ticked.
        /// </summary>
        private IEnumerable<GameMenuItem> StyleMenu(List<string> ids)
        {
            string section = MenuSection + "|" + Loc.StyleLabel;
            List<SplashStyle?> own = ids.Select(_settings.GetGameStyle).Distinct().ToList();
            SplashStyle? shared = own.Count == 1 ? own[0] : null;
            bool allDefault = own.Count == 1 && own[0] == null;

            GameMenuItem Entry(SplashStyle? style, string name, bool ticked) => new GameMenuItem
            {
                MenuSection = section,
                Description = (ticked ? "✓ " : string.Empty) + name,
                Action = a => UpdateSettings(() => _settings.SetGameStyle(IdsOf(a), style))
            };

            yield return Entry(null, Loc.GameDefault, allDefault);
            foreach (SplashStyle style in StyleList.Special)
            {
                yield return Entry(style, StyleList.Name(style), shared == style);
            }

            yield return new GameMenuItem { MenuSection = section, Description = "-" };
            foreach (SplashStyle style in StyleList.Designed)
            {
                yield return Entry(style, StyleList.Name(style), shared == style);
            }
        }

        private void AskForGameDuration(List<Game> games)
        {
            if (games == null || games.Count == 0)
            {
                return;
            }

            string firstId = games[0].Id.ToString();
            int current = _settings.HasGameSpecificDuration(firstId)
                ? _settings.GameSpecificDurations[firstId]
                : _settings.SplashScreenDuration;

            StringSelectionDialogResult result = PlayniteApi.Dialogs.SelectString(
                Loc.AskDuration(games[0].Name, games.Count) + "\n\n" + Loc.AskDurationNote,
                MenuSection,
                current.ToString());

            if (result == null || !result.Result)
            {
                return;
            }

            if (!int.TryParse(result.SelectedString?.Trim(), out int seconds) || seconds <= 0)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(Loc.InvalidNumber, MenuSection);
                return;
            }

            UpdateSettings(() => _settings.SetGameSpecificDuration(games.Select(g => g.Id.ToString()), seconds));
        }

        private static IEnumerable<string> IdsOf(GameMenuItemActionArgs args)
        {
            return args.Games.Select(g => g.Id.ToString()).ToList();
        }

        private void UpdateSettings(Action change)
        {
            change();
            SaveSettings();
        }

        // ---------------------------------------------------------------- splash handling

        private static readonly Random StylePick = new Random();
        private SplashStyle? _lastRandomStyle;

        /// <summary>The game's style (its own, else the default from the settings; only the default with <paramref name="defaultStyle"/>); Random picks a different one each time.</summary>
        private SplashStyle PickStyle(string gameId, bool defaultStyle = false)
        {
            SplashStyle wanted = defaultStyle ? _settings.Style : _settings.StyleForGame(gameId);
            SplashStyle style = StyleList.Resolve(wanted, StylePick, _lastRandomStyle);
            if (wanted == SplashStyle.Random)
            {
                _lastRandomStyle = style;
            }

            return style;
        }

        private void StartSplash(Game game, GameAction sourceAction, bool preview, System.Diagnostics.Stopwatch sinceRequest = null, bool defaultStyle = false)
        {
            sinceRequest = sinceRequest ?? System.Diagnostics.Stopwatch.StartNew();
            long waitedForUi = sinceRequest.ElapsedMilliseconds;
            if (_session != null)
            {
                _session.CloseImmediately();
                _session = null;
            }

            string gameId = game.Id.ToString();
            if (!preview && _settings.IsExcluded(gameId))
            {
                return;
            }

            string platform = game.Platforms?.FirstOrDefault()?.Name ?? string.Empty;
            int duration = _settings.GetDurationForGame(gameId, platform);
            if (duration <= 0)
            {
                duration = _settings.SplashScreenDuration > 0 ? _settings.SplashScreenDuration : 1;
            }

            SplashMode mode;
            if (preview)
            {
                mode = SplashMode.FixedTime;
                duration = PreviewSeconds;
            }
            else if (_settings.AutoDetectFullscreen && !_settings.HasGameSpecificDuration(gameId))
            {
                mode = SplashMode.AutoDetect;
            }
            else
            {
                mode = SplashMode.FixedTime;
            }

            // The splash size option applies to Desktop mode only; Fullscreen mode always fills the screen.
            bool desktop = PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop;
            var session = new SplashSession(game, new SplashSessionOptions
            {
                Mode = mode,
                DurationSeconds = duration,
                MinimumDisplaySeconds = Math.Max(0, _settings.MinimumDisplaySeconds),
                DetectionDelaySeconds = Math.Max(0, _settings.DetectionDelaySeconds),
                MaximumWaitSeconds = Math.Max(1, _settings.MaximumWaitSeconds),
                IgnoredProcessNames = (_settings.IgnoredProcessNames ?? new List<string>()).ToList(),
                GameFolder = mode == SplashMode.AutoDetect ? ResolveGameFolder(game, sourceAction) : null,
                DetectWindowedGames = _settings.DetectWindowedGames,
                Appearance = new SplashAppearance
                {
                    LogoPosition = _settings.LogoPosition,
                    LogoWidthPercent = _settings.LogoWidthPercent,
                    BackgroundDimPercent = _settings.BackgroundDimPercent,
                    Vignette = _settings.BackgroundVignette,
                    Zoom = _settings.BackgroundZoom
                },
                Images = SplashImageSources.ForGame(game),
                Style = PickStyle(gameId, defaultStyle),
                WindowFraction = desktop ? SplashWindowPlacement.Fraction(_settings.DesktopSplashSize) : 1.0,
                WindowRect = desktop && _settings.DesktopSplashSize == DesktopSplashSize.PlayniteWindow ? PlayniteWindowRect() : null,
                GameInfo = SplashGameInfo.FromGame(game, platform),
                DiagnosticLogging = _settings.EnableDiagnosticLogging,
                SinceRequest = sinceRequest,
                WaitedForUiMilliseconds = waitedForUi
            });
            session.Closed += (s, e) =>
            {
                if (_session == session)
                {
                    _session = null;
                }
            };

            _session = session;
            LogInfo($"Showing splash for \"{game.Name}\" (mode: {mode}, duration: {duration} s).");
            session.Show();
        }

        /// <summary>
        /// Where the splash goes to cover the Playnite window, read when the game starts (before Playnite may minimize
        /// itself), exactly its size whatever its proportions (the styles lay themselves out for the window's shape).
        /// Null when the window is minimized or hidden, which means the whole screen.
        /// </summary>
        private int[] PlayniteWindowRect()
        {
            try
            {
                Window main = Application.Current?.MainWindow;
                IntPtr handle = main == null ? IntPtr.Zero : new System.Windows.Interop.WindowInteropHelper(main).Handle;
                int[] bounds = MonitorHelper.VisibleWindowBounds(handle);
                if (bounds == null)
                {
                    LogInfo("The Playnite window is not on screen; the splash fills the screen instead.");
                }

                return bounds;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not read the Playnite window.");
                return null;
            }
        }
        /// <summary>
        /// Installation folder used to recognise the game's own windows, or null. Emulated games are skipped:
        /// their folder holds the ROM, while the window belongs to the emulator installed elsewhere.
        /// </summary>
        private string ResolveGameFolder(Game game, GameAction sourceAction)
        {
            try
            {
                if (sourceAction?.Type == GameActionType.Emulator || string.IsNullOrWhiteSpace(game.InstallDirectory))
                {
                    return null;
                }

                string folder = PlayniteApi.ExpandGameVariables(game, game.InstallDirectory);
                return GameFolder.Normalize(folder, GameFolder.SystemFolders());
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not resolve the game folder.");
                return null;
            }
        }

        private void CloseSplashFor(Game game, string reason)
        {
            SplashSession session = _session;
            if (session != null && game != null && session.GameId == game.Id)
            {
                LogInfo($"Closing splash for \"{game.Name}\": {reason}.");
                session.FadeOutAndClose();
            }
        }

        // ---------------------------------------------------------------- helpers

        private List<string> BuildPlatformList()
        {
            var names = new HashSet<string>(SplashAddonSettings.AvailablePlatforms, StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (Platform platform in PlayniteApi.Database.Platforms)
                {
                    if (!string.IsNullOrWhiteSpace(platform.Name))
                    {
                        names.Add(platform.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Could not read platforms from the database.");
            }

            // Keep platforms that are already configured selectable even if they are no longer in the library.
            foreach (PlatformDuration row in _settings.PlatformDurations)
            {
                if (!string.IsNullOrEmpty(row?.Platform))
                {
                    names.Add(row.Platform);
                }
            }

            names.Remove(string.Empty);
            var list = names.OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase).ToList();
            list.Insert(0, string.Empty);
            return list;
        }

        private void LogInfo(string message)
        {
            if (_settings.EnableDiagnosticLogging)
            {
                Logger.Info(message);
            }
        }

        private string ResolveGameName(string gameId)
        {
            try
            {
                return Guid.TryParse(gameId, out Guid id) ? PlayniteApi.Database.Games.Get(id)?.Name : null;
            }
            catch
            {
                return null;
            }
        }

        private void SaveSettings()
        {
            try
            {
                SavePluginSettings(_settings);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to save Splash Addon settings.");
            }
        }

        private static void RunOnUi(Action action)
        {
            void Guarded()
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Splash Addon error.");
                }
            }

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                Guarded();
            }
            else
            {
                dispatcher.Invoke((Action)Guarded);
            }
        }
    }
}
