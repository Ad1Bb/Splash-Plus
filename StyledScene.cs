using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Playnite.SDK;

namespace SplashAddonPlus
{
    /// <summary>
    /// Base of the designed splash styles. Content is laid out on a stage 1080 units high whose width
    /// follows the screen's aspect ratio (1 unit = screen height / 1080), scaled to the window by a Viewbox.
    /// The intro starts once the window is shown and the stage has its size.
    /// </summary>
    internal abstract class StyledScene : ISplashScene
    {
        protected const double StageHeight = 1080;

        /// <summary>Width of the centered safe area on wide screens.</summary>
        protected const double SafeAreaWidth = 1920;

        /// <summary>Separator between the values of a text line; empty values leave out their separator.</summary>
        protected const string Separator = "   ·   ";

        /// <summary>How long a text layout waits for the logo before it settles for the name.</summary>
        private static readonly TimeSpan LogoWait = TimeSpan.FromMilliseconds(300);

        private static readonly ILogger Logger = LogManager.GetLogger();

        private readonly Grid _root = new Grid { Background = Brushes.Black, ClipToBounds = true };
        private readonly Rectangle _toBlack = new Rectangle { Fill = Brushes.Black, Opacity = 0, IsHitTestVisible = false };
        private readonly System.Collections.Generic.List<Grid> _safeAreas = new System.Collections.Generic.List<Grid>();
        private bool _started;
        private DateTime _shownAt;
        private BitmapSource _arrivedLogo;
        private bool _logoFailed;
        private bool _logoDecided;
        private DispatcherTimer _logoTimer;
        private double? _coverAspect;
        private readonly Grid _sharp = new Grid { Opacity = 0 };
        private readonly Image _sharpImage = StyleKit.CoverImage();

        protected StyledScene(SplashGameInfo info, SplashImageSources sources)
        {
            Info = info ?? new SplashGameInfo();
            Sources = sources ?? new SplashImageSources();

            _root.Children.Add(new Viewbox { Stretch = Stretch.Uniform, Child = Stage });
            _root.Children.Add(_toBlack);
            _root.SizeChanged += (s, e) =>
            {
                if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
                {
                    Stage.Width = StageHeight * e.NewSize.Width / e.NewSize.Height;
                    foreach (Grid area in _safeAreas)
                    {
                        area.Width = SafeWidth;
                    }

                    Guard(() => OnStageWidth(Stage.Width));
                    TryStart();
                }
            };
            _root.Loaded += (s, e) => TryStart();
            _root.Unloaded += (s, e) => _logoTimer?.Stop();
        }

        public UIElement Root => _root;

        protected Grid Stage { get; } = new Grid { Height = StageHeight, ClipToBounds = true };

        /// <summary>Stage width in units (1920 on a 16:9 screen); NaN before the first layout.</summary>
        protected double StageWidth => Stage.Width;

        protected SplashGameInfo Info { get; }

        protected SplashImageSources Sources { get; }

        /// <summary>The logo is shown when the game has one (its size is fixed by each style).</summary>
        protected bool UseLogo => !string.IsNullOrEmpty(Sources.Logo);

        /// <summary>Width of the centered safe area: the stage width, at most 1920 units.</summary>
        protected double SafeWidth => Math.Min(StageWidth, SafeAreaWidth);

        /// <summary>Left edge of the safe area on the stage.</summary>
        protected double SafeLeft => (StageWidth - SafeWidth) / 2;

        /// <summary>
        /// How much a composition <paramref name="contentWidth"/> units wide has to shrink to fit a narrow stage (a
        /// squarish Playnite window in Desktop mode) with <paramref name="margin"/> on each side: 1 whenever it fits,
        /// so wide screens look exactly as designed.
        /// </summary>
        protected static double FitWidth(double stageWidth, double contentWidth, double margin = 60)
        {
            return double.IsNaN(stageWidth) ? 1 : Math.Min(1, (stageWidth - 2 * margin) / contentWidth);
        }

        /// <summary>One screen pixel (DIP) in units, so hairlines never get thinner than a pixel.</summary>
        protected double OnePixel => _root.ActualHeight > 0 ? Math.Max(1, StageHeight / _root.ActualHeight) : 1;

        /// <summary>
        /// A size in units rounded to whole screen pixels (at least one), so tiled patterns stay crisp at any
        /// resolution and display scaling.
        /// </summary>
        protected double SnapToPixels(double units)
        {
            if (_root.ActualHeight <= 0)
            {
                return units;
            }

            double pixelsPerUnit = _root.ActualHeight * VisualTreeHelper.GetDpi(_root).PixelsPerDip / StageHeight;
            return Math.Max(1, Math.Round(units * pixelsPerUnit)) / pixelsPerUnit;
        }

        /// <summary>
        /// The clock the styles read (the system clock). The preview recorder replaces it with the animation clock it
        /// steps by hand, so timing that is not a WPF animation stays in step with the frames.
        /// </summary>
        internal static Func<DateTime> Now = () => DateTime.Now;

        /// <summary>Seconds since the splash was shown.</summary>
        protected double SinceShown => _started ? (Now() - _shownAt).TotalSeconds : 0;

        /// <summary>A delay counted from showing the splash, as seen from now.</summary>
        protected double FromShow(double seconds) => Math.Max(0, seconds - SinceShown);

        public abstract void LoadImagesAsync(SplashImageSources sources, MonitorArea screen);

        /// <summary>
        /// Plays the style's exit and lays black over the whole scene by the end of the window's one-second fade,
        /// so every style ends in black (the game usually starts on a black screen), never on its grey base.
        /// </summary>
        public void OnClosing()
        {
            Guard(Close);
            Guard(() => StyleKit.Animate(_toBlack, UIElement.OpacityProperty, 0, 1, 0.9, 0, StyleKit.CubicIn));
        }

        /// <summary>Starts the intro animations (window shown, stage width known).</summary>
        protected abstract void Start();

        /// <summary>Exit animations; they must finish within the window's one-second fade.</summary>
        protected abstract void Close();

        /// <summary>Lays out what depends on the stage width.</summary>
        protected virtual void OnStageWidth(double width)
        {
        }

        /// <summary>
        /// Adds a full-height layer of the safe-area width, centered on the stage: what it holds stays in the middle
        /// on 21:9 and 32:9 screens.
        /// </summary>
        protected Grid SafeArea()
        {
            var area = new Grid { HorizontalAlignment = HorizontalAlignment.Center };
            if (StageWidth > 0)
            {
                area.Width = SafeWidth;
            }

            _safeAreas.Add(area);
            Stage.Children.Add(area);
            return area;
        }

        protected bool HasCover => !string.IsNullOrEmpty(Sources.Cover);

        /// <summary>Width / height of the sharp cover, read once from the file's header (2:3 when unknown).</summary>
        protected double CoverAspect => _coverAspect ?? (_coverAspect = SplashText.CoverAspect(ImageLoader.ReadAspect(Sources.Cover))).Value;

        /// <summary>
        /// Adds the sharp artwork layer (full screen, cover-fit): the background as it is, or the blurred cover when
        /// there is no background. It appears with <see cref="ShowSharpBackdrop"/>.
        /// </summary>
        protected void AddSharpBackdrop()
        {
            RenderOptions.SetBitmapScalingMode(_sharpImage, BitmapScalingMode.HighQuality);
            _sharp.Children.Add(_sharpImage);
            Stage.Children.Add(_sharp);
        }

        /// <summary>
        /// Off the UI thread: the background at screen size plus a small copy for blurring, or (without a usable
        /// background) only a small copy of the cover.
        /// </summary>
        protected Artwork LoadArtwork(int screenWidth, int screenHeight)
        {
            var artwork = new Artwork { Small = ImageLoader.Load(Sources.Background, 480, 0, cover: false) };
            if (artwork.Small != null)
            {
                artwork.Sharp = ImageLoader.Load(Sources.Background, screenWidth, screenHeight, cover: true);
            }
            else
            {
                artwork.Small = ImageLoader.Load(Sources.Cover, 480, 0, cover: false);
                artwork.FromCover = true;
            }

            return artwork;
        }

        /// <summary>
        /// Shows the sharp artwork (scaled by <paramref name="backgroundScale"/>), or the cover blurred by σ units and
        /// scaled when it stands in; fades in over <paramref name="fadeSeconds"/>.
        /// </summary>
        protected void ShowSharpBackdrop(Artwork artwork, double coverSigma = 28, double coverScale = 1.15,
            double backgroundScale = 1, double fadeSeconds = 0.8)
        {
            if (artwork?.Small == null)
            {
                return;
            }

            bool cover = artwork.FromCover;
            _sharpImage.Source = cover ? BlurCovering(artwork.Small, coverSigma) : artwork.Sharp ?? artwork.Small;
            double scale = cover ? coverScale : backgroundScale;
            _sharpImage.RenderTransform = new ScaleTransform(scale, scale);
            StyleKit.FadeIn(_sharp, fadeSeconds, 0, StyleKit.SineOut);
        }

        /// <summary>The blurred copy for glass (σ in units; <paramref name="coverSigma"/> when the cover stands in), or null.</summary>
        protected BitmapSource GlassCopy(Artwork artwork, double sigma, double coverSigma)
        {
            return artwork?.Small == null ? null : BlurCovering(artwork.Small, artwork.FromCover ? coverSigma : sigma);
        }

        /// <summary>A blur of σ units as seen when the image covers the whole stage.</summary>
        protected BitmapSource BlurCovering(BitmapSource image, double sigma)
        {
            double shownHeight = Math.Max(StageHeight, StageWidth * image.PixelHeight / Math.Max(1, image.PixelWidth));
            return StyleKit.Blur(image, StyleKit.SigmaToRadius(sigma) / shownHeight);
        }

        /// <summary>The game's artwork for the backdrop and the glass.</summary>
        protected sealed class Artwork
        {
            /// <summary>The background at screen size (null when the cover stands in).</summary>
            public BitmapSource Sharp { get; set; }

            /// <summary>A small copy for blurring: the background, or the cover.</summary>
            public BitmapSource Small { get; set; }

            public bool FromCover { get; set; }
        }

        /// <summary>Loads images off the UI thread, then applies them on it.</summary>
        protected void LoadInBackground<T>(Func<T> load, Action<T> apply) where T : class
        {
            Task.Run(load).ContinueWith(task =>
            {
                T result = task.Status == TaskStatus.RanToCompletion ? task.Result : null;
                _root.Dispatcher.BeginInvoke((Action)(() => Guard(() => apply(result))));
            }, TaskScheduler.Default);
        }

        /// <summary>
        /// Text layouts: decides between the logo and the name. The logo is used when it arrives within 300 ms of
        /// showing; otherwise the name is shown for good, so the layout never jumps. Call from <see cref="Start"/>.
        /// </summary>
        protected void WaitForLogo()
        {
            if (!UseLogo || _logoFailed)
            {
                DecideLogo(null);
            }
            else if (_arrivedLogo != null)
            {
                DecideLogo(_arrivedLogo);
            }
            else
            {
                _logoTimer = new DispatcherTimer { Interval = LogoWait };
                _logoTimer.Tick += (s, e) => DecideLogo(null);
                _logoTimer.Start();
            }
        }

        /// <summary>Reports the loaded logo (null when it could not be loaded).</summary>
        protected void LogoLoaded(BitmapSource logo)
        {
            if (_logoDecided)
            {
                return;
            }

            if (logo == null)
            {
                _logoFailed = true;
                if (_started)
                {
                    DecideLogo(null);
                }
            }
            else if (_started)
            {
                DecideLogo(logo);
            }
            else
            {
                _arrivedLogo = logo;
            }
        }

        /// <summary>The logo won: show it (with its arrival animation).</summary>
        protected virtual void ShowLogo(BitmapSource logo)
        {
        }

        /// <summary>The name is shown instead of the logo.</summary>
        protected virtual void ShowName()
        {
        }

        private void DecideLogo(BitmapSource logo)
        {
            if (_logoDecided)
            {
                return;
            }

            _logoDecided = true;
            _logoTimer?.Stop();
            _logoTimer = null;
            Guard(() =>
            {
                if (logo != null)
                {
                    ShowLogo(logo);
                }
                else
                {
                    ShowName();
                }
            });
        }

        /// <summary>Screen size in pixels for decoding images (1920 × 1080 when unknown).</summary>
        protected static void ScreenPixels(MonitorArea screen, out int width, out int height)
        {
            width = screen?.Width > 0 ? screen.Width : 1920;
            height = screen?.Height > 0 ? screen.Height : 1080;
        }

        /// <summary>Pixels on the screen for a size in units.</summary>
        protected static int UnitsToPixels(double units, int screenHeight) => Math.Max(1, (int)Math.Ceiling(units * screenHeight / StageHeight));

        private void TryStart()
        {
            if (!_started && _root.IsLoaded && StageWidth > 0)
            {
                _started = true;
                _shownAt = Now();
                Guard(Start);
            }
        }

        // A failing animation must never take the splash (or Playnite) down.
        private static void Guard(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Splash screen style error.");
            }
        }
    }

    /// <summary>Building blocks shared by the splash styles.</summary>
    internal static class StyleKit
    {
        public static readonly FontFamily SegoeUi = new FontFamily("Segoe UI");

        public static readonly FontFamily Consolas = new FontFamily("Consolas");

        /// <summary>
        /// Sizes a logo exactly: <paramref name="width"/> wide, or less when it would be taller than
        /// <paramref name="maxHeight"/> (so a logo limited by its height still lines up with the text).
        /// </summary>
        public static void FitLogo(Image image, BitmapSource logo, double width, double maxHeight)
        {
            double aspect = logo.PixelHeight > 0 ? (double)logo.PixelWidth / logo.PixelHeight : 1;
            double height = width / aspect;
            if (height > maxHeight)
            {
                height = maxHeight;
                width = height * aspect;
            }

            image.Width = width;
            image.Height = height;
        }

        /// <summary>
        /// A stepped animation: each value is set at its time (seconds), with no tweening in between. Before the
        /// first step the property keeps its own value.
        /// </summary>
        public static DoubleAnimationUsingKeyFrames Steps(params (double At, double Value)[] steps)
        {
            var animation = new DoubleAnimationUsingKeyFrames();
            foreach ((double at, double value) in steps)
            {
                animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(value, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(at))));
            }

            return animation;
        }

        /// <summary>Sets an element's opacity to <paramref name="value"/> at the given second (an instant cut).</summary>
        public static void CutTo(UIElement element, double value, double atSecond)
        {
            element.BeginAnimation(UIElement.OpacityProperty, Steps((atSecond, value)));
        }

        /// <summary>
        /// A tiled pattern drawn in a <paramref name="size"/> × <paramref name="size"/> square; set its Viewport to
        /// whole screen pixels (see SnapToPixels) and use EdgeMode.Aliased on the element for crisp edges.
        /// </summary>
        public static DrawingBrush Tiles(double size, params (Rect Area, Color Color)[] cells)
        {
            var drawing = new DrawingGroup();
            drawing.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, size, size))));
            foreach ((Rect area, Color color) in cells)
            {
                drawing.Children.Add(new GeometryDrawing(new SolidColorBrush(color), null, new RectangleGeometry(area)));
            }

            return new DrawingBrush(drawing)
            {
                TileMode = TileMode.Tile,
                Stretch = Stretch.Fill,
                Viewbox = new Rect(0, 0, size, size),
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(0, 0, size, size),
                ViewportUnits = BrushMappingMode.Absolute
            };
        }

        /// <summary>Exponential ease-out with a custom exponent (the later style specs use 7).</summary>
        public static IEasingFunction ExpoOutBy(double exponent) => Ease(new ExponentialEase { Exponent = exponent }, EasingMode.EaseOut);

        /// <summary>WPF blur radius for a Gaussian standard deviation (both in the same units).</summary>
        public static double SigmaToRadius(double sigma) => 3 * sigma;

        public static IEasingFunction QuintOut => Ease(new QuinticEase(), EasingMode.EaseOut);
        public static IEasingFunction CubicOut => Ease(new CubicEase(), EasingMode.EaseOut);
        public static IEasingFunction CubicIn => Ease(new CubicEase(), EasingMode.EaseIn);
        public static IEasingFunction QuadIn => Ease(new QuadraticEase(), EasingMode.EaseIn);
        public static IEasingFunction QuadOut => Ease(new QuadraticEase(), EasingMode.EaseOut);
        public static IEasingFunction SineOut => Ease(new SineEase(), EasingMode.EaseOut);
        public static IEasingFunction SineInOut => Ease(new SineEase(), EasingMode.EaseInOut);
        public static IEasingFunction CubicInOut => Ease(new CubicEase(), EasingMode.EaseInOut);
        public static IEasingFunction ExpoOut => Ease(new ExponentialEase { Exponent = 6 }, EasingMode.EaseOut);
        public static IEasingFunction BackOut => Ease(new BackEase { Amplitude = 0.4 }, EasingMode.EaseOut);

        /// <summary>Animates a property once (from the current value when <paramref name="from"/> is null).</summary>
        public static void Animate(IAnimatable target, DependencyProperty property, double? from, double to,
            double seconds, double delay = 0, IEasingFunction ease = null, Action completed = null)
        {
            var animation = new DoubleAnimation
            {
                To = to,
                Duration = TimeSpan.FromSeconds(seconds),
                BeginTime = TimeSpan.FromSeconds(delay),
                EasingFunction = ease
            };
            if (from.HasValue)
            {
                animation.From = from.Value;
            }

            if (completed != null)
            {
                animation.Completed += (s, e) => completed();
            }

            target.BeginAnimation(property, animation);
        }

        /// <summary>
        /// Spring move A → B → C over <paramref name="seconds"/>: to the overshoot B in the first 60 % (CubicOut),
        /// back to C in the rest (SineInOut). The property holds A until the delay is over.
        /// </summary>
        public static void Spring(DependencyObject target, DependencyProperty property, double from, double overshoot, double to,
            double seconds, double delay)
        {
            target.SetValue(property, from);
            ((IAnimatable)target).BeginAnimation(property, Keys(delay,
                (0, from, null), (0.6 * seconds, overshoot, CubicOut), (seconds, to, SineInOut)));
        }

        /// <summary>Spring scale on both axes.</summary>
        public static void Spring(ScaleTransform scale, double from, double overshoot, double to, double seconds, double delay)
        {
            Spring(scale, ScaleTransform.ScaleXProperty, from, overshoot, to, seconds, delay);
            Spring(scale, ScaleTransform.ScaleYProperty, from, overshoot, to, seconds, delay);
        }

        /// <summary>The fade that goes with a spring move ("with appearing"): 0 → 1 in its first 60 %, CubicOut.</summary>
        public static void SpringFade(UIElement element, double seconds, double delay)
        {
            Animate(element, UIElement.OpacityProperty, 0, 1, 0.6 * seconds, delay, CubicOut);
        }

        /// <summary>
        /// Key frames (second, value, ease) starting from the property's current value; a null ease is linear.
        /// </summary>
        public static DoubleAnimationUsingKeyFrames Keys(double delay, params (double At, double Value, IEasingFunction Ease)[] keys)
        {
            var animation = new DoubleAnimationUsingKeyFrames { BeginTime = TimeSpan.FromSeconds(delay) };
            foreach ((double at, double value, IEasingFunction ease) in keys)
            {
                animation.KeyFrames.Add(new EasingDoubleKeyFrame(value, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(at)), ease));
            }

            return animation;
        }

        /// <summary>Animates a property back and forth forever.</summary>
        public static void Loop(IAnimatable target, DependencyProperty property, double from, double to,
            double seconds, IEasingFunction ease, int frameRate)
        {
            var animation = new DoubleAnimation(from, to, TimeSpan.FromSeconds(seconds))
            {
                EasingFunction = ease,
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            Timeline.SetDesiredFrameRate(animation, frameRate);
            target.BeginAnimation(property, animation);
        }

        /// <summary>Fades an element in.</summary>
        public static void FadeIn(UIElement element, double seconds, double delay = 0, IEasingFunction ease = null)
        {
            Animate(element, UIElement.OpacityProperty, 0, 1, seconds, delay, ease);
        }

        /// <summary>
        /// A blurred copy of a (small) image, rendered once so nothing is blurred while the splash is shown.
        /// <paramref name="radiusShare"/> is the blur radius as a share of the image height.
        /// </summary>
        public static BitmapSource Blur(BitmapSource image, double radiusShare)
        {
            if (image == null)
            {
                return null;
            }

            int width = image.PixelWidth;
            int height = image.PixelHeight;
            var element = new Image
            {
                Source = image,
                Width = width,
                Height = height,
                Stretch = Stretch.Fill,
                Effect = new BlurEffect { Radius = radiusShare * height, KernelType = KernelType.Gaussian }
            };
            element.Measure(new Size(width, height));
            element.Arrange(new Rect(0, 0, width, height));

            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(element);
            bitmap.Freeze();
            return bitmap;
        }

        /// <summary>
        /// A sharp cover in a frame: rounded corners (only the top ones with <paramref name="squareBottom"/>),
        /// an optional placeholder fill until the image arrives, and a shadow outside the clip.
        /// </summary>
        public static Grid CoverFrame(double width, double height, double radius, bool squareBottom, Brush placeholder,
            Effect shadow, out Image image)
        {
            image = CoverImage();
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            image.Opacity = 0;

            // A clip taller than the frame by the radius leaves the bottom corners square.
            var clip = new Grid { Clip = new RectangleGeometry(new Rect(0, 0, width, height + (squareBottom ? radius : 0)), radius, radius) };
            if (placeholder != null)
            {
                clip.Children.Add(new Rectangle { Fill = placeholder });
            }

            clip.Children.Add(image);
            return new Grid { Width = width, Height = height, Effect = shadow, Children = { clip } };
        }

        /// <summary>An image filling its box with cropping, scaled around its center.</summary>
        public static Image CoverImage(double scale = 1)
        {
            return new Image
            {
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(scale, scale)
            };
        }

        public static Rectangle Dim(double opacity)
        {
            return new Rectangle { Fill = Brushes.Black, Opacity = opacity, IsHitTestVisible = false };
        }

        /// <summary>
        /// A soft shadow of a rounded <paramref name="width"/> × <paramref name="height"/> box drawn only outside it,
        /// so it never darkens what lies inside (glass, a placeholder). Place it exactly like the box, behind it.
        /// </summary>
        public static Grid SoftShadow(double width, double height, double radius, double offset, double blur, double opacity)
        {
            double reach = offset + 2 * blur;
            var outside = new CombinedGeometry(GeometryCombineMode.Exclude,
                new RectangleGeometry(new Rect(-reach, -reach, width + 2 * reach, height + 2 * reach)),
                new RectangleGeometry(new Rect(0, 0, width, height), radius, radius));
            outside.Freeze();
            return new Grid
            {
                Width = width,
                Height = height,
                IsHitTestVisible = false,
                Clip = outside,
                Children =
                {
                    new Rectangle
                    {
                        Fill = Brushes.Black,
                        RadiusX = radius,
                        RadiusY = radius,
                        Opacity = opacity,
                        Margin = new Thickness(0, offset, 0, -offset),
                        Effect = new BlurEffect { Radius = blur, KernelType = KernelType.Gaussian }
                    }
                }
            };
        }

        /// <summary>Drop shadow straight down.</summary>
        public static DropShadowEffect Shadow(double offset, double blur, double opacity)
        {
            return new DropShadowEffect
            {
                Color = Colors.Black,
                Direction = 270,
                ShadowDepth = offset,
                BlurRadius = blur,
                Opacity = opacity
            };
        }

        public static SolidColorBrush White(double alpha)
        {
            var brush = new SolidColorBrush(Color.FromArgb((byte)Math.Round(alpha * 255), 255, 255, 255));
            brush.Freeze();
            return brush;
        }

        public static SolidColorBrush Gray(byte level)
        {
            var brush = new SolidColorBrush(Color.FromRgb(level, level, level));
            brush.Freeze();
            return brush;
        }

        /// <summary>Text (Segoe UI unless given): wraps to at most <paramref name="maxLines"/> lines, then ends with "…".</summary>
        public static TextBlock Text(string text, double size, int weight, Brush foreground, double lineHeight = 1.2, int maxLines = 1,
            FontFamily family = null)
        {
            var block = new TextBlock
            {
                Text = text ?? string.Empty,
                FontFamily = family ?? SegoeUi,
                FontSize = size,
                FontWeight = FontWeight.FromOpenTypeWeight(weight),
                Foreground = foreground,
                LineHeight = size * lineHeight,
                LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = maxLines > 1 ? TextWrapping.Wrap : TextWrapping.NoWrap
            };
            if (maxLines > 1)
            {
                block.MaxHeight = size * lineHeight * maxLines;
            }

            return block;
        }

        /// <summary>
        /// Long names: keeps the large size when the text fits in <paramref name="lines"/> lines of
        /// <paramref name="width"/>; otherwise switches to the small size (and its line limit), then "…".
        /// </summary>
        public static void FitText(TextBlock block, double width, double size, int lines, double smallSize, int smallLines, double lineHeight)
        {
            bool large = LineCount(block, width, size, lineHeight) <= lines;
            double chosen = large ? size : smallSize;
            block.FontSize = chosen;
            block.LineHeight = chosen * lineHeight;
            block.MaxHeight = chosen * lineHeight * (large ? lines : smallLines);
            block.TextWrapping = TextWrapping.Wrap;
            block.MaxWidth = width;
        }

        private static int LineCount(TextBlock block, double width, double size, double lineHeight)
        {
            var typeface = new Typeface(block.FontFamily, block.FontStyle, block.FontWeight, block.FontStretch);
            var text = new FormattedText(block.Text ?? string.Empty, System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, typeface, size, Brushes.White, 1.0)
            {
                MaxTextWidth = Math.Max(1, width),
                LineHeight = size * lineHeight
            };
            return (int)Math.Round(text.Height / (size * lineHeight));
        }

        /// <summary>A gradient along an angle given like CSS (0° = to top, 90° = to right).</summary>
        public static LinearGradientBrush AngledGradient(double degrees, params GradientStop[] stops)
        {
            double radians = degrees * Math.PI / 180;
            double dx = Math.Sin(radians) / 2;
            double dy = -Math.Cos(radians) / 2;
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0.5 - dx, 0.5 - dy),
                EndPoint = new Point(0.5 + dx, 0.5 + dy)
            };
            foreach (GradientStop stop in stops)
            {
                brush.GradientStops.Add(stop);
            }

            return brush;
        }

        public static GradientStop Stop(byte alpha, byte level, double offset) => new GradientStop(Color.FromArgb(alpha, level, level, level), offset);

        private static IEasingFunction Ease(EasingFunctionBase ease, EasingMode mode)
        {
            ease.EasingMode = mode;
            ease.Freeze();
            return ease;
        }
    }
}
