using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace SplashAddonPlus
{
    internal enum SplashMode
    {
        /// <summary>Close after a fixed number of seconds from the moment the splash appears.</summary>
        FixedTime,

        /// <summary>Close once a game window covers the whole screen.</summary>
        AutoDetect
    }

    internal sealed class SplashSessionOptions
    {
        public SplashMode Mode { get; set; }
        public int DurationSeconds { get; set; }
        public int MinimumDisplaySeconds { get; set; }
        public int DetectionDelaySeconds { get; set; }
        public int MaximumWaitSeconds { get; set; }
        public IList<string> IgnoredProcessNames { get; set; }

        /// <summary>Normalised installation folder of the game, or null (unknown, too general, or an emulated game).</summary>
        public string GameFolder { get; set; }

        /// <summary>Accept a large window from the game's folder even when it is not fullscreen.</summary>
        public bool DetectWindowedGames { get; set; }

        public SplashAppearance Appearance { get; set; }
        public SplashImageSources Images { get; set; }
        public SplashStyle Style { get; set; }

        /// <summary>Share of the screen the splash covers: 1 fills it (always in Fullscreen mode), less is a centered window.</summary>
        public double WindowFraction { get; set; } = 1.0;

        /// <summary>
        /// Screen rectangle for the splash window (x, y, width, height), taken from the Playnite window when the splash
        /// should cover just that; null otherwise. Wins over <see cref="WindowFraction"/>.
        /// </summary>
        public int[] WindowRect { get; set; }

        /// <summary>The splash is a window rather than the whole screen.</summary>
        public bool Windowed => WindowRect != null || WindowFraction < 1;

        /// <summary>Game details shown by the Cinema and Showcase styles.</summary>
        public SplashGameInfo GameInfo { get; set; }

        /// <summary>Write details about detection to extensions.log (off by default).</summary>
        public bool DiagnosticLogging { get; set; }

        /// <summary>Started when Playnite reported the game start (or the preview was asked for), for the timing log.</summary>
        public System.Diagnostics.Stopwatch SinceRequest { get; set; } = System.Diagnostics.Stopwatch.StartNew();

        /// <summary>Milliseconds from Playnite's game-start event until the add-on's code ran on the UI thread.</summary>
        public long WaitedForUiMilliseconds { get; set; }
    }

    /// <summary>
    /// One splash screen shown for one game launch, together with everything that decides when it closes.
    /// All members must be used on the UI thread.
    /// </summary>
    internal sealed class SplashSession
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        private static readonly TimeSpan FadeDuration = TimeSpan.FromSeconds(1.0);

        /// <summary>The window is shown at once and brightens evenly over a second (the user's choice, as in 2.2).</summary>
        private static readonly TimeSpan FadeInDuration = TimeSpan.FromSeconds(1.0);

        /// <summary>A splash slower than this to reach the screen is noted in the log even without diagnostic logging.</summary>
        private const long SlowAppearanceMilliseconds = 1500;
        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan GamepadPollInterval = TimeSpan.FromMilliseconds(100);
        private const double InputGraceSeconds = 0.75;

        // A fullscreen window from outside a known game folder is often a launcher, so it has to last a bit longer.
        private const int OutsideFolderExtraSeconds = 3;

        private readonly SplashSessionOptions _options;
        /// <summary>Built right after the window is on screen (null until then), so a black window shows at once.</summary>
        private ISplashScene _scene;
        private readonly Window _window;
        private long _shownAfterMilliseconds;
        private MonitorArea _monitor;
        private readonly List<DispatcherTimer> _timers = new List<DispatcherTimer>();

        private DateTime _shownAt;
        private bool _closing;
        private bool _countdownStarted;
        private bool _mousePressedOnSplash;

        private GamepadWatcher _gamepad;
        private DispatcherTimer _gamepadTimer;

        private GameWindowDetector _detector;
        private IntPtr _candidateWindow = IntPtr.Zero;
        private DateTime _candidateSince;
        private IntPtr _detectedGameWindow = IntPtr.Zero;
        private bool _ignoredProgramFillsScreen;
        private DateTime _ignoredProgramLastSeen = DateTime.MinValue;

        public SplashSession(Game game, SplashSessionOptions options)
        {
            GameId = game.Id;
            GameName = game.Name;
            _options = options;
            _window = CreateCoverWindow(null, options);
            _window.Title = "SplashAddonPlusSplashScreen";
        }

        /// <summary>
        /// Prepares a style once, off screen and without images, so the first splash after Playnite starts does not
        /// pay for loading its code, fonts and effects. Best effort: any failure is ignored.
        /// </summary>
        internal static void WarmUp(SplashStyle style)
        {
            try
            {
                ISplashScene scene = CreateScene(new SplashSessionOptions
                {
                    Style = style,
                    Appearance = new SplashAppearance(),
                    Images = new SplashImageSources(),
                    GameInfo = new SplashGameInfo { Name = "Splash Plus" }
                });
                if (scene.Root is FrameworkElement root)
                {
                    root.Width = 384;
                    root.Height = 216;
                    root.Measure(new Size(384, 216));
                    root.Arrange(new Rect(0, 0, 384, 216));
                    root.UpdateLayout();
                    var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(384, 216, 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(root);
                }
            }
            catch (Exception ex)
            {
                Logger.Info("Warm-up skipped: " + ex.Message);
            }
        }

        private static ISplashScene CreateScene(SplashSessionOptions options)
        {
            switch (options.Style)
            {
                case SplashStyle.Cinema:
                    return new CinemaScene(options.GameInfo, options.Images);
                case SplashStyle.Showcase:
                    return new ShowcaseScene(options.GameInfo, options.Images);
                case SplashStyle.Gallery:
                    return new GalleryScene(options.GameInfo, options.Images);
                case SplashStyle.Plinth:
                    return new PlinthScene(options.GameInfo, options.Images);
                case SplashStyle.Drawer:
                    return new DrawerScene(options.GameInfo, options.Images);
                case SplashStyle.Band:
                    return new BandScene(options.GameInfo, options.Images);
                case SplashStyle.TextMode:
                    return new TextModeScene(options.GameInfo, options.Images, TextModeScene.Palette.Light);
                case SplashStyle.TextModeDark:
                    return new TextModeScene(options.GameInfo, options.Images, TextModeScene.Palette.Dark);
                case SplashStyle.PopFrost:
                    return new PopFrostScene(options.GameInfo, options.Images);
                case SplashStyle.SwingFrost:
                    return new SwingFrostScene(options.GameInfo, options.Images);
                case SplashStyle.MeetFrost:
                    return new MeetFrostScene(options.GameInfo, options.Images);
                case SplashStyle.RiseFrost:
                    return new RiseFrostScene(options.GameInfo, options.Images);
                case SplashStyle.LootDrop:
                    return new LootDropScene(options.GameInfo, options.Images);
                case SplashStyle.LockOn:
                    return new LockOnScene(options.GameInfo, options.Images);
                case SplashStyle.Showdown:
                    return new ShowdownScene(options.GameInfo, options.Images);
                case SplashStyle.Focus:
                    return new FocusScene(options.GameInfo, options.Images);
                case SplashStyle.Slant:
                    return new SlantScene(options.GameInfo, options.Images);
                case SplashStyle.IrisGloss:
                    return new IrisGlossScene(options.GameInfo, options.Images);
                case SplashStyle.SlateGloss:
                    return new SlateGlossScene(options.GameInfo, options.Images);
                default:
                    return new SplashScene(options.Appearance);
            }
        }

        public event EventHandler Closed;

        public Guid GameId { get; }

        public string GameName { get; }

        public void Show()
        {
            if (_options.Mode == SplashMode.AutoDetect)
            {
                _detector = new GameWindowDetector(_options.IgnoredProcessNames, _options.DiagnosticLogging,
                    _options.GameFolder, _options.DetectWindowedGames);
                _detector.TakeSnapshot();
            }


            _window.Loaded += (s, e) =>
            {
                if (!_closing)
                {
                    _window.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0.0, 1.0, FadeInDuration));
                }
            };

            // Manual escape hatch for launchers asking for input, windowed games, etc.
            // Input arriving right after the splash appears is ignored, so the tail of the
            // double-click or key press that launched the game cannot dismiss it.
            _window.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape && !e.IsRepeat && SecondsSinceShown >= InputGraceSeconds)
                {
                    Log(() => $"Splash for \"{GameName}\" closed with Esc.");
                    FadeOutAndClose();
                }
            };
            _window.MouseDown += (s, e) =>
            {
                _mousePressedOnSplash = SecondsSinceShown >= InputGraceSeconds;
            };
            _window.MouseUp += (s, e) =>
            {
                if (_mousePressedOnSplash)
                {
                    Log(() => $"Splash for \"{GameName}\" closed by mouse click.");
                    FadeOutAndClose();
                }
            };
            _window.Closed += (s, e) =>
            {
                StopTimers();
                StopGamepad();
                Closed?.Invoke(this, EventArgs.Empty);
            };

            _shownAt = DateTime.Now;

            try
            {
                _window.Show();
                if (_options.Windowed)
                {
                    // Again after Show, in case WPF applied its own initial size; the window is still fully transparent.
                    PlaceWindow(new WindowInteropHelper(_window).Handle, _options);
                }

                _window.Activate();
                _shownAfterMilliseconds = _options.SinceRequest.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to show splash screen.");
                return;
            }

            // Only read: the size of the screen the splash opened on, so images are decoded at that resolution (or at
            // the window's size, when the splash is a smaller window).
            _monitor = MonitorHelper.FromWindow(new WindowInteropHelper(_window).Handle) ?? MonitorHelper.Primary();
            string shape = _options.WindowRect != null
                ? $", over the Playnite window ({_options.WindowRect[2]}x{_options.WindowRect[3]} at {_options.WindowRect[0]},{_options.WindowRect[1]})"
                : _options.WindowFraction < 1 ? $", as a window of {_options.WindowFraction:P0}" : string.Empty;
            Log(() => $"Splash for \"{GameName}\" on a {_monitor?.ToString() ?? "unknown"} screen{shape}; " +
                      $"game folder: {_options.GameFolder ?? "not used"}.");

            // The window is drawn black first; the style is built right after that frame, so nothing it reads (the
            // cover's proportions from disk, its layout) holds the window back.
            _window.Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)BuildScene);

            StartGamepad();

            switch (_options.Mode)
            {
                case SplashMode.FixedTime:
                    StartCountdown(_options.DurationSeconds);
                    break;

                case SplashMode.AutoDetect:
                    StartSafetyTimer();
                    StartTimer(PollInterval, PollForGameWindow, repeat: true);
                    break;
            }
        }

        /// <summary>Builds the style into the window that is already on screen and starts loading its images.</summary>
        private void BuildScene()
        {
            if (_closing || _scene != null)
            {
                return;
            }

            try
            {
                _scene = CreateScene(_options);
                _window.Content = _scene.Root;
                _scene.LoadImagesAsync(_options.Images, ScaledForWindow(_monitor, _options));

                // Runs once layout and rendering of the style have had their turn.
                _window.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, (Action)LogTiming);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to build the splash style.");
                CloseImmediately();
            }
        }

        /// <summary>
        /// When the style is first on screen: how long each step took since Playnite reported the game start. Written
        /// with diagnostic logging, and always when the splash took long, so a report shows where the time went.
        /// </summary>
        private void LogTiming()
        {
            long drawn = _options.SinceRequest.ElapsedMilliseconds;
            string timing = $"Splash for \"{GameName}\" timing since Playnite reported the game start: add-on code ran after " +
                            $"{_options.WaitedForUiMilliseconds} ms, window shown after {_shownAfterMilliseconds} ms, style on screen after {drawn} ms.";
            if (drawn >= SlowAppearanceMilliseconds)
            {
                Logger.Warn(timing + (_options.WaitedForUiMilliseconds >= SlowAppearanceMilliseconds / 2
                    ? " Most of the wait was before the add-on's turn (Playnite, a script or another add-on)."
                    : string.Empty));
            }
            else
            {
                Log(() => timing);
            }
        }

        /// <summary>Starts the regular countdown; 0 or less closes the splash right away.</summary>
        private void StartCountdown(int seconds)
        {
            if (_closing || _countdownStarted)
            {
                return;
            }

            _countdownStarted = true;
            StopTimers();

            if (seconds <= 0)
            {
                FadeOutAndClose();
                return;
            }

            StartTimer(TimeSpan.FromSeconds(seconds), FadeOutAndClose);
        }

        private double SecondsSinceShown => (DateTime.Now - _shownAt).TotalSeconds;

        public void FadeOutAndClose()
        {
            if (_closing)
            {
                return;
            }

            _closing = true;
            StopTimers();
            StopGamepad();
            _scene?.OnClosing();

            var fadeOut = new DoubleAnimation { To = 0.0, Duration = FadeDuration };
            fadeOut.Completed += (s, e) => CloseWindow();
            _window.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        public void CloseImmediately()
        {
            _closing = true;
            StopTimers();
            StopGamepad();
            CloseWindow();
        }

        private void CloseWindow()
        {
            try
            {
                _window.Close();
            }
            catch
            {
            }

            if (_detectedGameWindow != IntPtr.Zero)
            {
                GameWindowDetector.FocusIfPlayniteHasForeground(_detectedGameWindow);
            }
        }

        private void StartSafetyTimer()
        {
            int maxWait = Math.Max(1, _options.MaximumWaitSeconds);
            StartTimer(TimeSpan.FromSeconds(maxWait), () =>
            {
                Log(() => $"Splash for \"{GameName}\" reached the maximum wait time ({maxWait} s), closing.");
                FadeOutAndClose();
            });
        }

        private void PollForGameWindow()
        {
            if (_closing || _detector == null)
            {
                return;
            }

            DateTime now = DateTime.Now;
            WindowScanResult scan = _detector.Scan();

            foreach (string window in scan.NewWindows)
            {
                Log(() => $"New window while loading \"{GameName}\" at {SecondsSinceShown:0.0} s: {window}");
            }

            // A program from the ignore list covering the screen (typically a launcher's loading screen) is never
            // the game, and the splash waits until it is gone.
            if (scan.IgnoredProgramFillsScreen)
            {
                if (!_ignoredProgramFillsScreen)
                {
                    Log(() => $"Ignored program fills the screen while loading \"{GameName}\" at {SecondsSinceShown:0.0} s, waiting: {scan.IgnoredProgramWindow}");
                }

                _ignoredProgramFillsScreen = true;
                _ignoredProgramLastSeen = now;
                GameWindowDetector.KeepOnTop(new WindowInteropHelper(_window).Handle);
            }
            else if (_ignoredProgramFillsScreen)
            {
                _ignoredProgramFillsScreen = false;
                Log(() => $"Ignored program no longer fills the screen at {SecondsSinceShown:0.0} s.");
            }

            IntPtr hWnd = scan.GameWindow;
            if (hWnd == IntPtr.Zero)
            {
                _candidateWindow = IntPtr.Zero;
                return;
            }

            bool outsideKnownFolder = _detector.KnowsGameFolder && !scan.GameWindowFromFolder;
            if (hWnd != _candidateWindow)
            {
                _candidateWindow = hWnd;
                _candidateSince = now;
                string kind = scan.GameWindowFromFolder
                    ? (scan.GameWindowIsFullscreen ? "Game window found (fullscreen)" : "Game window found (windowed)")
                    : "Fullscreen window found";
                string note = outsideKnownFolder ? $"; not from the game folder, waiting {OutsideFolderExtraSeconds} s longer" : string.Empty;
                Log(() => $"{kind} for \"{GameName}\" after {SecondsSinceShown:0.0} s: {_detector.Describe(hWnd)}{note}");
            }

            if (scan.IgnoredProgramFillsScreen)
            {
                return;
            }

            // The delay counts from the later of: the game window appearing, the ignored program leaving the screen.
            DateTime readySince = _candidateSince > _ignoredProgramLastSeen ? _candidateSince : _ignoredProgramLastSeen;
            int requiredSeconds = _options.DetectionDelaySeconds + (outsideKnownFolder ? OutsideFolderExtraSeconds : 0);
            bool stableLongEnough = (now - readySince).TotalSeconds >= requiredSeconds;
            bool shownLongEnough = (now - _shownAt).TotalSeconds >= _options.MinimumDisplaySeconds;
            if (stableLongEnough && shownLongEnough)
            {
                _detectedGameWindow = hWnd;
                Log(() => $"Closing splash for \"{GameName}\", the game window is ready.");
                FadeOutAndClose();
            }
        }

        // Kept apart from _timers: starting the regular countdown must not switch the pad off.
        private void StartGamepad()
        {
            try
            {
                _gamepad = new GamepadWatcher();
                _gamepadTimer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = GamepadPollInterval };
                _gamepadTimer.Tick += (s, e) =>
                {
                    if (_closing || _gamepad == null)
                    {
                        return;
                    }

                    int controller = _gamepad.PollForBPress(acceptPress: SecondsSinceShown >= InputGraceSeconds);
                    if (controller >= 0)
                    {
                        Log(() => $"Splash for \"{GameName}\" closed with gamepad button B (controller {controller + 1}).");
                        FadeOutAndClose();
                    }
                };
                _gamepadTimer.Start();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Gamepad support could not be started.");
                StopGamepad();
            }
        }

        private void StopGamepad()
        {
            _gamepadTimer?.Stop();
            _gamepadTimer = null;
            _gamepad = null;
        }

        /// <summary>Diagnostic entry; the message is not even built unless logging is enabled.</summary>
        private void Log(Func<string> message)
        {
            if (_options.DiagnosticLogging)
            {
                Logger.Info(message());
            }
        }

        private void StartTimer(TimeSpan interval, Action onTick, bool repeat = false)
        {
            var timer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = interval };
            timer.Tick += (s, e) =>
            {
                if (!repeat)
                {
                    timer.Stop();
                }

                onTick();
            };
            _timers.Add(timer);
            timer.Start();
        }

        private void StopTimers()
        {
            foreach (DispatcherTimer timer in _timers)
            {
                timer.Stop();
            }

            _timers.Clear();
        }

        /// <summary>
        /// Borderless, topmost black window: maximized, or (Desktop mode) a window over the Playnite window or of a
        /// share of the screen, centered on the screen Windows opens it on, with rounded corners (Windows 11).
        /// </summary>
        private static Window CreateCoverWindow(UIElement content, SplashSessionOptions options)
        {
            bool windowed = options.Windowed;
            var window = new Window
            {
                WindowStyle = WindowStyle.None,
                WindowState = windowed ? WindowState.Normal : WindowState.Maximized,
                Topmost = true,
                Background = Brushes.Black,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                Opacity = 0.0,
                Content = content
            };

            if (windowed)
            {
                // Placed in screen pixels once the window exists but before it is shown, so it never flashes elsewhere.
                window.SourceInitialized += (s, e) => PlaceWindow(new WindowInteropHelper(window).Handle, options);
            }

            return window;
        }

        private static void PlaceWindow(IntPtr hWnd, SplashSessionOptions options)
        {
            try
            {
                int[] r = options.WindowRect;
                if (r == null)
                {
                    MonitorArea monitor = MonitorHelper.FromWindow(hWnd) ?? MonitorHelper.Primary();
                    if (monitor == null || monitor.Width <= 0 || monitor.Height <= 0)
                    {
                        return;
                    }

                    r = SplashWindowPlacement.Compute(monitor.Width, monitor.Height,
                        monitor.WorkLeft, monitor.WorkTop, monitor.WorkWidth, monitor.WorkHeight, options.WindowFraction);
                }

                NativeMethods.SetWindowPos(hWnd, NativeMethods.HWND_TOPMOST, r[0], r[1], r[2], r[3], NativeMethods.SWP_NOACTIVATE);
                int round = NativeMethods.DWMWCP_ROUND;
                NativeMethods.DwmSetWindowAttribute(hWnd, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int));
            }
            catch (Exception ex)
            {
                // Rounded corners need Windows 11; the window itself still works without them.
                Logger.Warn(ex, "Could not place the splash window.");
            }
        }

        /// <summary>The monitor size shrunk to the window's, so a smaller splash decodes smaller images.</summary>
        private static MonitorArea ScaledForWindow(MonitorArea monitor, SplashSessionOptions options)
        {
            if (monitor == null || !options.Windowed)
            {
                return monitor;
            }

            return new MonitorArea
            {
                Handle = monitor.Handle,
                Left = monitor.Left,
                Top = monitor.Top,
                Width = options.WindowRect?[2] ?? (int)Math.Round(monitor.Width * options.WindowFraction),
                Height = options.WindowRect?[3] ?? (int)Math.Round(monitor.Height * options.WindowFraction),
                IsPrimary = monitor.IsPrimary
            };
        }

        private static class NativeMethods
        {
            public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
            public const int DWMWCP_ROUND = 2;
            public const uint SWP_NOACTIVATE = 0x10;
            public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

            [DllImport("user32.dll")]
            public static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

            [DllImport("dwmapi.dll")]
            public static extern int DwmSetWindowAttribute(IntPtr hWnd, int attribute, ref int value, int size);
        }
    }
}
