using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static SplashAddonPlus.StyleKit;

namespace SplashAddonPlus
{
    /// <summary>
    /// Iris Gloss: the art pulls into focus; on the left a round disc of dark glossy glass opens with a round window
    /// for the cover. Its reflections trail behind the disc's movement, and a point of light travels a thin ring
    /// around it. On the right, with no panel, stand the logo or name, a rule and a line of details.
    /// </summary>
    internal sealed class IrisGlossScene : LensScene
    {
        private const double DiscSize = 460;
        private const double WindowSize = 340;
        private const double WindowInset = (DiscSize - WindowSize) / 2;
        private const double DiscCenterX = -440;
        private const double RingRadius = 252;
        private const double RingBox = 2 * (RingRadius + 6);
        private const double TextLeft = -120;
        private const double TextWidth = 900;

        private static readonly double RingLength = 2 * Math.PI * RingRadius;

        private readonly ScreenGlass _disc = new ScreenGlass(new SolidColorBrush(Color.FromArgb(89, 0, 0, 0)), 24, 60, 0.25);
        private readonly ScaleTransform _discScale = new ScaleTransform(0.8, 0.8, DiscSize / 2, DiscSize / 2);

        // In a narrow window the disc, ring and text shrink together (1 on wide screens); the frost stays screen-aligned.
        private readonly ScaleTransform _discFit = new ScaleTransform(1, 1, DiscSize / 2, DiscSize / 2);
        private Canvas _ringHolder;
        private Canvas _textHolder;
        private readonly Reflection _cap;
        private readonly Reflection _streaks;
        private readonly TranslateTransform _sheenShift;
        private readonly Reflection _windowCap;
        private readonly Reflection _windowStreak;
        private readonly Image _cover;
        private readonly ScaleTransform _coverScale = new ScaleTransform(1.12, 1.12);

        private readonly Grid _ring;
        private readonly RotateTransform _ringTurn = new RotateTransform(0, RingBox / 2, RingBox / 2);
        private readonly Path _ringLine;
        private readonly Grid _light;
        private readonly RotateTransform _lightTurn = new RotateTransform(0, RingBox / 2, RingBox / 2);

        private readonly Grid _textBlock;
        private readonly TextBlock _name;
        private readonly Image _logo;
        private readonly Rectangle _rule;
        private readonly ScaleTransform _ruleScale = new ScaleTransform(0, 1);
        private readonly TextBlock _details;

        public IrisGlossScene(SplashGameInfo info, SplashImageSources sources)
            : base(info, sources)
        {
            // 1–2. Base and the art pulling into focus
            AddLens();

            // 3. The ring with its point of light, under the disc
            Geometry circle = RingCircle();
            _ringLine = new Path
            {
                Data = circle,
                Stroke = White(0.5),
                StrokeThickness = 1.5,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeDashCap = PenLineCap.Round,
                // Drawn on: an 86 % dash followed by a gap longer than the rest, pushed off the start until it draws.
                StrokeDashArray = new DoubleCollection { 0.86 * RingLength / 1.5, RingLength / 1.5 },
                StrokeDashOffset = (0.86 * RingLength + 4) / 1.5,
                CacheMode = new BitmapCache()
            };
            var lightLine = new Path
            {
                Data = circle,
                Stroke = Brushes.White,
                StrokeThickness = 2.5,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeDashCap = PenLineCap.Round,
                StrokeDashArray = new DoubleCollection { 0.035 * RingLength / 2.5, 2 * RingLength / 2.5 },
                Effect = new DropShadowEffect { Color = Colors.White, ShadowDepth = 0, BlurRadius = 6, Opacity = 0.9 },
                CacheMode = new BitmapCache()
            };
            _light = new Grid { Opacity = 0, RenderTransform = _lightTurn, Children = { lightLine } };
            _ring = new Grid { Width = RingBox, Height = RingBox, RenderTransform = _ringTurn, Children = { _ringLine, _light } };
            _ringHolder = new Canvas { Children = { _ring } };
            Stage.Children.Add(_ringHolder);

            // 4. The disc of smoked glossy glass
            _disc.Motion.Children.Add(_discScale);
            _disc.Motion.Children.Add(_discFit);
            _disc.Opacity = 0;
            var discShape = new EllipseGeometry(new Rect(0, 0, DiscSize, DiscSize));
            discShape.Freeze();
            _cap = Gloss.Cap(DiscSize, DiscSize, -0.10, -0.38, 1.20, 0.85, Gloss.White(0.20, 0), Gloss.White(0.06, 0.6), Gloss.White(0, 1));
            _streaks = Gloss.Streaks(DiscSize, DiscSize, Gloss.White(0, 0.30), Gloss.White(0.14, 0.335), Gloss.White(0, 0.37),
                Gloss.White(0, 0.41), Gloss.White(0.08, 0.425), Gloss.White(0, 0.44));
            Rectangle sheen = Gloss.Sheen(DiscSize, DiscSize, 0.30, out _sheenShift);
            var discLayer = new Canvas();
            discLayer.Children.Add(_cap.Element);
            discLayer.Children.Add(_streaks.Element);
            discLayer.Children.Add(Gloss.LitEdges(discShape, 0.38, 0.12, 0.10));
            discLayer.Children.Add(sheen);

            // 5. The window: the cover, or a monogram, under its own reflections
            var windowShape = new EllipseGeometry(new Rect(0, 0, WindowSize, WindowSize));
            windowShape.Freeze();
            var inside = new Grid { Clip = windowShape, Children = { new Rectangle { Fill = new SolidColorBrush(Color.FromArgb(77, 0, 0, 0)) } } };
            if (HasCover)
            {
                _cover = CoverImage();
                _cover.Opacity = 0;
                _cover.RenderTransform = _coverScale;
                RenderOptions.SetBitmapScalingMode(_cover, BitmapScalingMode.HighQuality);
                inside.Children.Add(_cover);
            }
            else
            {
                string name = (Info.Name ?? string.Empty).Trim();
                string letter = name.Length > 0 ? name.Substring(0, 1).ToUpper() : "?";
                TextBlock monogram = Text(letter, 190, 300, White(0.92), 1.0);
                monogram.HorizontalAlignment = HorizontalAlignment.Center;
                monogram.VerticalAlignment = VerticalAlignment.Center;
                monogram.TextAlignment = TextAlignment.Center;
                monogram.Effect = Shadow(2, 16, 0.45);
                inside.Children.Add(monogram);
            }

            _windowCap = Gloss.Cap(WindowSize, WindowSize, -0.20, -0.45, 1.40, 0.80, Gloss.White(0.14, 0), Gloss.White(0, 1));
            _windowStreak = Gloss.Streaks(WindowSize, WindowSize, Gloss.White(0, 0.36), Gloss.White(0.16, 0.39), Gloss.White(0, 0.42));
            inside.Children.Add(new Canvas { Children = { _windowCap.Element, _windowStreak.Element } });
            inside.Children.Add(Gloss.LitEdges(windowShape, 0.35, 0.10, 0));
            var window = new Grid
            {
                Width = WindowSize,
                Height = WindowSize,
                Children = { SoftShadow(WindowSize, WindowSize, WindowSize / 2, 10, 30, 0.35), inside }
            };
            Canvas.SetLeft(window, WindowInset);
            Canvas.SetTop(window, WindowInset);
            discLayer.Children.Add(window);
            _disc.Decor.Children.Add(discLayer);
            Stage.Children.Add(_disc);

            // 6. Text, with no panel
            var column = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left, MaxWidth = TextWidth };
            var title = new Grid { HorizontalAlignment = HorizontalAlignment.Left };
            if (!string.IsNullOrWhiteSpace(Info.Name))
            {
                _name = Text(Info.Name, 64, 600, Brushes.White, 1.1, 2);
                FitText(_name, TextWidth, 64, 2, 44, 2, 1.1);
                _name.Effect = Shadow(2, 16, 0.45);
                _name.Opacity = 0;
                _name.RenderTransform = new TranslateTransform(40, 0);
                title.Children.Add(_name);
            }

            if (UseLogo)
            {
                _logo = new Image
                {
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Opacity = 0,
                    Effect = Shadow(3, 14, 0.45),
                    RenderTransform = new TranslateTransform(40, 0)
                };
                RenderOptions.SetBitmapScalingMode(_logo, BitmapScalingMode.HighQuality);
                title.Children.Add(_logo);
            }

            column.Children.Add(title);
            _rule = new Rectangle
            {
                Width = 120,
                Height = 2,
                Fill = White(0.6),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 22, 0, 18),
                RenderTransform = _ruleScale
            };
            column.Children.Add(_rule);
            string details = SplashText.Join(Separator, Info.Genre, Info.ReleaseYear?.ToString(), Info.Developer);
            if (details != null)
            {
                _details = Text(details, 20, 350, White(0.85), 1.3);
                _details.MaxWidth = TextWidth;
                _details.Effect = Shadow(2, 16, 0.45);
                _details.Opacity = 0;
                _details.RenderTransform = new TranslateTransform(40, 0);
                column.Children.Add(_details);
            }

            _textBlock = new Grid { Width = TextWidth, Height = StageHeight, Children = { column } };
            _textHolder = new Canvas { Children = { _textBlock } };
            Stage.Children.Add(_textHolder);
        }

        /// <summary>A full circle of the ring's radius, starting at 12 o'clock and running clockwise.</summary>
        private static Geometry RingCircle()
        {
            double c = RingBox / 2;
            var top = new Point(c, c - RingRadius);
            var figure = new PathFigure { StartPoint = top, IsClosed = false, IsFilled = false };
            figure.Segments.Add(new ArcSegment(new Point(c, c + RingRadius), new Size(RingRadius, RingRadius), 0, false, SweepDirection.Clockwise, true));
            figure.Segments.Add(new ArcSegment(top, new Size(RingRadius, RingRadius), 0, false, SweepDirection.Clockwise, true));
            var geometry = new PathGeometry(new[] { figure });
            geometry.Freeze();
            return geometry;
        }

        protected override void OnStageWidth(double width)
        {
            // The ring reaches 698 u left of the center, the text up to 780 u right of it.
            double fit = FitWidth(width, 2 * 780, 40);
            double centerX = width / 2 + DiscCenterX * fit;
            _disc.SetShape(new EllipseGeometry(new Rect(0, 0, DiscSize, DiscSize)), new Point(centerX - DiscSize / 2, StageHeight / 2 - DiscSize / 2));
            _disc.SetStageSize(width, StageHeight);
            Canvas.SetLeft(_ring, centerX - RingBox / 2);
            Canvas.SetTop(_ring, StageHeight / 2 - RingBox / 2);
            Canvas.SetLeft(_textBlock, width / 2 + TextLeft * fit);
            if (fit < 1)
            {
                _discFit.ScaleX = fit;
                _discFit.ScaleY = fit;
                _ringHolder.RenderTransform = new ScaleTransform(fit, fit, centerX, StageHeight / 2);
                _textHolder.RenderTransform = new ScaleTransform(fit, fit, width / 2 + TextLeft * fit, StageHeight / 2);
            }
        }

        protected override void Start()
        {
            StartLens();

            Animate(_disc, UIElement.OpacityProperty, 0, 1, 1.6, 0.9, ExpoOut);
            Animate(_discScale, ScaleTransform.ScaleXProperty, 0.8, 1, 1.6, 0.9, ExpoOut);
            Animate(_discScale, ScaleTransform.ScaleYProperty, 0.8, 1, 1.6, 0.9, ExpoOut);
            if (_cover != null)
            {
                Animate(_coverScale, ScaleTransform.ScaleXProperty, 1.12, 1, 2.4, 0.9, QuintOut);
                Animate(_coverScale, ScaleTransform.ScaleYProperty, 1.12, 1, 2.4, 0.9, QuintOut);
            }

            // Reflections enter trailing the disc, then drift.
            _cap.EnterFrom(0.168, 0.08, 1.6, 0.9);
            _streaks.EnterFrom(-0.28, -0.20, 1.9, 0.9);
            _windowCap.EnterFrom(0.11, -0.10, 2.4, 0.9);
            _windowStreak.EnterFrom(-0.22, 0, 2.7, 0.9);
            _streaks.Drift(-0.06, 0, 8, 2.5);
            _cap.Drift(0.04, 0.03, 9, 2.5);
            _windowStreak.Drift(-0.05, 0, 7, 3.3);
            Gloss.Sweep(_sheenShift, DiscSize, 6, 0.84, 2.8);

            // The ring draws itself and turns; the point of light speeds up and slows down on each lap.
            Animate(_ringLine, Shape.StrokeDashOffsetProperty, _ringLine.StrokeDashOffset, 0, 1.6, 1.1, SineInOut);
            _ringTurn.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 360, TimeSpan.FromSeconds(40))
            {
                BeginTime = TimeSpan.FromSeconds(1.1),
                RepeatBehavior = RepeatBehavior.Forever
            });
            Animate(_light, UIElement.OpacityProperty, 0, 1, 0.8, 2.6);
            _lightTurn.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 360, TimeSpan.FromSeconds(7))
            {
                BeginTime = TimeSpan.FromSeconds(2.6),
                EasingFunction = SineInOut,
                RepeatBehavior = RepeatBehavior.Forever
            });

            Animate(_ruleScale, ScaleTransform.ScaleXProperty, 0, 1, 1.2, 1.45, ExpoOut);
            if (_details != null)
            {
                SlideIn(_details, 1.55);
            }

            WaitForLogo();
        }

        private static void SlideIn(FrameworkElement element, double delay)
        {
            Animate(element, UIElement.OpacityProperty, 0, 1, 1.4, delay, ExpoOut);
            Animate(element.RenderTransform, TranslateTransform.XProperty, 40, 0, 1.4, delay, ExpoOut);
        }

        protected override void ShowLogo(BitmapSource logo)
        {
            if (_name != null)
            {
                _name.Visibility = Visibility.Collapsed;
            }

            _logo.Source = logo;
            FitLogo(_logo, logo, 560, 170);
            SlideIn(_logo, FromShow(1.2));
        }

        protected override void ShowName()
        {
            if (_logo != null)
            {
                _logo.Visibility = Visibility.Collapsed;
            }

            if (_name != null)
            {
                SlideIn(_name, FromShow(1.2));
            }
        }

        protected override void Close()
        {
            CloseLens();
            Animate(_disc, UIElement.OpacityProperty, null, 0, 0.6, 0, CubicIn);
            Animate(_discScale, ScaleTransform.ScaleXProperty, null, 0.85, 0.6, 0, CubicIn);
            Animate(_discScale, ScaleTransform.ScaleYProperty, null, 0.85, 0.6, 0, CubicIn);
            _streaks.Leave(0.28, 0);
            _cap.Leave(-0.11, -0.10);
            _windowStreak.Leave(0.22, 0);
            _windowCap.Leave(-0.09, -0.10);
            Animate(_ring, UIElement.OpacityProperty, null, 0, 0.4);
            Animate(_textBlock, UIElement.OpacityProperty, null, 0, 0.4);
        }

        public override void LoadImagesAsync(SplashImageSources sources, MonitorArea screen)
        {
            ScreenPixels(screen, out int width, out int height);
            bool useLogo = UseLogo;
            bool hasCover = HasCover;

            LoadInBackground(() => new Loaded
            {
                Artwork = LoadArtwork(width, height),
                Cover = hasCover ? ImageLoader.Load(Sources.Cover, UnitsToPixels(WindowSize, height), UnitsToPixels(WindowSize, height), cover: true) : null,
                Logo = useLogo ? ImageLoader.Load(Sources.Logo, UnitsToPixels(560, height), UnitsToPixels(170, height), cover: false) : null
            }, loaded =>
            {
                loaded = loaded ?? new Loaded();
                LensArrived(loaded.Artwork);
                _disc.SetCopy(FrostCopy(loaded.Artwork), FrostScale(loaded.Artwork));
                if (_cover != null && loaded.Cover != null)
                {
                    _cover.Source = loaded.Cover;
                    FadeIn(_cover, 0.7, 0, SineOut);
                }

                if (useLogo)
                {
                    LogoLoaded(loaded.Logo);
                }
            });
        }
    }
}
