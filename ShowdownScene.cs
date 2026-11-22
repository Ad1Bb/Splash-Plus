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
    /// Showdown: a boss-intro title card. A tilted black band with white edges cuts across the screen with speed
    /// lines racing through it; the cover slides in from the right like the opponent's portrait, and the title
    /// slams down on the left in tall narrow capitals with a white flash and a short shake.
    /// </summary>
    internal sealed class ShowdownScene : StyledScene
    {
        private const double Angle = -7;
        private const double BandHeight = 380;
        private const double GroupWidth = 1920;
        private const double CenterX = GroupWidth / 2;
        private const double CenterY = StageHeight / 2;
        private const double CoverLeft = CenterX + 260;
        private const double CoverWidth = 360;
        private const double CoverHeight = 540;
        private const double TextLeft = CenterX - 820;
        private const double TileWidth = 1024;

        /// <summary>Speed of the speed lines: one tile every 0.6 s.</summary>
        private const double LineSpeed = TileWidth / 0.6;

        /// <summary>Bahnschrift (Windows 10/11), with Impact where it is missing.</summary>
        private static readonly FontFamily Bahnschrift = new FontFamily("Bahnschrift, Impact");

        private readonly Canvas _bandLayer = new Canvas();
        private readonly Grid _band;
        private readonly ScaleTransform _bandScale = new ScaleTransform(1, 0);
        private readonly Rectangle _lines;
        private readonly TranslateTransform _linesShift = new TranslateTransform();
        private DateTime _linesStart;
        private bool _linesRunning;

        private readonly Canvas _group = new Canvas { Width = GroupWidth, Height = StageHeight };

        // Below 1:1 of the design (a narrow window) the title, cover and band shrink together; 1 on wide screens.
        private readonly ScaleTransform _groupScale = new ScaleTransform(1, 1);

        /// <summary>Width the title and the cover take up around the center (text from 140 u, cover to 1580 u).</summary>
        private const double ContentWidth = 2 * (CenterX - TextLeft);
        private readonly TranslateTransform _shake = new TranslateTransform();

        private readonly Grid _coverFrame;
        private readonly TranslateTransform _coverShift = new TranslateTransform(900, 0);
        private readonly Image _cover;

        private readonly Grid _textBlock;
        private readonly TranslateTransform _textShift = new TranslateTransform();
        private readonly Grid _title;
        private readonly ScaleTransform _titleScale = new ScaleTransform(2.4, 2.4);
        private readonly Grid _nameGroup;
        private readonly Image _logo;
        private readonly Rectangle _rule;
        private readonly ScaleTransform _ruleScale = new ScaleTransform(0, 1);
        private readonly TextBlock _details;

        private readonly Rectangle _flash = new Rectangle { Fill = Brushes.White, Opacity = 0, IsHitTestVisible = false };

        public ShowdownScene(SplashGameInfo info, SplashImageSources sources)
            : base(info, sources)
        {
            bool cover = HasCover;

            // 1. Base: a dark radial gradient, seen only without artwork
            var baseFill = new RadialGradientBrush { Center = new Point(0.5, 0.4), GradientOrigin = new Point(0.5, 0.4) };
            baseFill.GradientStops.Add(new GradientStop(Color.FromRgb(0x22, 0x22, 0x22), 0));
            baseFill.GradientStops.Add(new GradientStop(Color.FromRgb(0x05, 0x05, 0x05), 0.75));
            Stage.Children.Add(new Rectangle { Fill = baseFill });

            // 2. The sharp artwork (no dimming in this style)
            AddSharpBackdrop();

            // 4. Shaking layer: the band and the content tilted with it
            var shaking = new Grid { RenderTransform = _shake };

            // 4a. Band with white edges and the speed lines inside
            _lines = new Rectangle
            {
                Fill = new ImageBrush(SpeedLineTile())
                {
                    TileMode = TileMode.Tile,
                    Stretch = Stretch.Fill,
                    Viewport = new Rect(0, 0, TileWidth, BandHeight),
                    ViewportUnits = BrushMappingMode.Absolute,
                    Transform = _linesShift
                }
            };
            var bandTransforms = new TransformGroup();
            bandTransforms.Children.Add(_bandScale);
            bandTransforms.Children.Add(new RotateTransform(Angle));
            _band = new Grid
            {
                Height = BandHeight,
                ClipToBounds = true,
                Background = new SolidColorBrush(Color.FromArgb(214, 0, 0, 0)),
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = bandTransforms,
                Children =
                {
                    _lines,
                    new Border { BorderBrush = White(0.92), BorderThickness = new Thickness(0, 3, 0, 3) }
                }
            };
            Canvas.SetTop(_band, CenterY - BandHeight / 2);
            _bandLayer.Children.Add(_band);
            shaking.Children.Add(_bandLayer);

            // 4b. Content on a 1920 × 1080 frame around the screen's center, tilted like the band
            _group.RenderTransformOrigin = new Point(0.5, 0.5);
            _group.RenderTransform = new TransformGroup { Children = { _groupScale, new RotateTransform(Angle) } };
            shaking.Children.Add(new Canvas { Children = { _group } });

            if (cover)
            {
                _cover = CoverImage();
                _cover.Opacity = 0;
                RenderOptions.SetBitmapScalingMode(_cover, BitmapScalingMode.HighQuality);
                _coverFrame = new Grid
                {
                    Width = CoverWidth,
                    Height = CoverHeight,
                    Opacity = 0,
                    RenderTransform = _coverShift,
                    Children =
                    {
                        // Hard shadow: a plain offset rectangle
                        new Rectangle
                        {
                            Fill = new SolidColorBrush(Color.FromArgb(153, 0, 0, 0)),
                            RenderTransform = new TranslateTransform(18, 18)
                        },
                        new Border
                        {
                            Background = Gray(0x15),
                            BorderBrush = Brushes.White,
                            BorderThickness = new Thickness(4),
                            Child = new Grid { ClipToBounds = true, Children = { _cover } }
                        }
                    }
                };
                Canvas.SetLeft(_coverFrame, CoverLeft);
                Canvas.SetTop(_coverFrame, CenterY - CoverHeight / 2);
                _group.Children.Add(_coverFrame);
            }

            // Text block: logo or name, a rule and a line of details
            double textWidth = cover ? 1000 : 1640;
            HorizontalAlignment align = cover ? HorizontalAlignment.Left : HorizontalAlignment.Center;
            var origin = new Point(cover ? 0 : 0.5, 0.5);
            var column = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            _title = new Grid
            {
                HorizontalAlignment = align,
                Opacity = 0,
                RenderTransformOrigin = origin,
                RenderTransform = _titleScale
            };
            string name = (Info.Name ?? string.Empty).ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(name))
            {
                TextBlock face = NameText(name, Brushes.White, cover);
                FitText(face, textWidth, 124, 2, 70, 3, 0.95);
                TextBlock shadow = NameText(name, new SolidColorBrush(Color.FromArgb(153, 0, 0, 0)), cover);
                FitText(shadow, textWidth, 124, 2, 70, 3, 0.95);
                shadow.RenderTransform = new TranslateTransform(8, 8);
                _nameGroup = new Grid { HorizontalAlignment = align, Children = { shadow, face } };
                _title.Children.Add(_nameGroup);
            }

            if (UseLogo)
            {
                _logo = new Image
                {
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = align,
                    Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 0, Direction = 315, ShadowDepth = 8 * Math.Sqrt(2), Opacity = 0.6 }
                };
                RenderOptions.SetBitmapScalingMode(_logo, BitmapScalingMode.HighQuality);
                _title.Children.Add(_logo);
            }

            column.Children.Add(_title);

            _rule = new Rectangle
            {
                Width = 160,
                Height = 6,
                Fill = Brushes.White,
                HorizontalAlignment = align,
                Margin = new Thickness(0, 22, 0, 16),
                RenderTransformOrigin = new Point(cover ? 0 : 0.5, 0.5),
                RenderTransform = _ruleScale
            };
            column.Children.Add(_rule);

            string details = SplashText.Join("  ·  ", Info.Genre, Info.ReleaseYear?.ToString());
            if (details != null)
            {
                _details = Text(details.ToUpperInvariant(), 26, 600, White(0.75), 1.2, 1, Bahnschrift);
                _details.HorizontalAlignment = align;
                _details.MaxWidth = textWidth;
                _details.Opacity = 0;
                _details.RenderTransform = new TranslateTransform(0, 10);
                column.Children.Add(_details);
            }

            _textBlock = new Grid { Width = textWidth, Height = StageHeight, RenderTransform = _textShift, Children = { column } };
            Canvas.SetLeft(_textBlock, TextLeft);
            _group.Children.Add(_textBlock);

            Stage.Children.Add(shaking);

            // 5. White flash over everything
            Stage.Children.Add(_flash);

            ((FrameworkElement)Root).Unloaded += (s, e) => StopLines();
        }

        private static TextBlock NameText(string name, Brush brush, bool cover)
        {
            TextBlock block = Text(name, 124, 700, brush, 0.95, 2, Bahnschrift);
            block.FontStretch = FontStretches.Condensed;
            block.TextAlignment = cover ? TextAlignment.Left : TextAlignment.Center;
            return block;
        }

        protected override void OnStageWidth(double width)
        {
            // The band is 120 % of the screen wide, so its tilted ends still reach past the edges.
            _band.Width = 1.2 * width;
            Canvas.SetLeft(_band, -0.1 * width);
            Canvas.SetLeft(_group, width / 2 - CenterX);

            double fit = FitWidth(width, ContentWidth, 40);
            if (fit < 1)
            {
                _groupScale.ScaleX = fit;
                _groupScale.ScaleY = fit;
                _band.Height = BandHeight * fit;
                Canvas.SetTop(_band, CenterY - _band.Height / 2);
            }
        }

        protected override void Start()
        {
            Animate(_bandScale, ScaleTransform.ScaleYProperty, 0, 1, 0.45, 0.10, ExpoOut);
            StartLines();
            Animate(_lines, UIElement.OpacityProperty, 1, 0.4, 1.0, 2.2);

            if (_coverFrame != null)
            {
                Animate(_coverShift, TranslateTransform.XProperty, 900, 0, 0.65, 0.30, QuintOut);
                Animate(_coverFrame, UIElement.OpacityProperty, 0, 1, 0.65, 0.30, QuintOut);
            }

            Animate(_ruleScale, ScaleTransform.ScaleXProperty, 0, 1, 0.5, 1.0, ExpoOut);
            if (_details != null)
            {
                Animate(_details, UIElement.OpacityProperty, 0, 1, 0.7, 1.15, CubicOut);
                Animate(_details.RenderTransform, TranslateTransform.YProperty, 10, 0, 0.7, 1.15, CubicOut);
            }

            WaitForLogo();
        }

        protected override void ShowLogo(BitmapSource logo)
        {
            if (_nameGroup != null)
            {
                _nameGroup.Visibility = Visibility.Collapsed;
            }

            _logo.Source = logo;
            FitLogo(_logo, logo, 500, 260);
            Slam(FromShow(0.65));
        }

        protected override void ShowName()
        {
            if (_logo != null)
            {
                _logo.Visibility = Visibility.Collapsed;
            }

            // Without a name the hit still lands, on an empty title.
            Slam(FromShow(0.65));
        }

        /// <summary>
        /// The title drops from 2.4× onto the band, speeding up into the hit; at the hit (0.25 s in) the screen
        /// flashes white and the band shakes.
        /// </summary>
        private void Slam(double delay)
        {
            Animate(_title, UIElement.OpacityProperty, 0, 1, 0.26, delay, CubicIn);
            Animate(_titleScale, ScaleTransform.ScaleXProperty, 2.4, 1, 0.26, delay, CubicIn);
            Animate(_titleScale, ScaleTransform.ScaleYProperty, 2.4, 1, 0.26, delay, CubicIn);

            double hit = delay + 0.25;
            _flash.BeginAnimation(UIElement.OpacityProperty, Keys(hit, (0, 0, null), (0.054, 0.5, null), (0.45, 0, null)));
            _shake.BeginAnimation(TranslateTransform.XProperty, Shake(hit, -14, 12, -8, 6, -3));
            _shake.BeginAnimation(TranslateTransform.YProperty, Shake(hit, 6, -8, 4, -3, 2));
        }

        private static DoubleAnimationUsingKeyFrames Shake(double delay, params double[] offsets)
        {
            var animation = Keys(delay, (0, 0, null));
            for (int i = 0; i < offsets.Length; i++)
            {
                animation.KeyFrames.Add(new LinearDoubleKeyFrame(offsets[i], KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.06 * (i + 1)))));
            }

            animation.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.06 * (offsets.Length + 1)))));
            return animation;
        }

        protected override void Close()
        {
            Animate(_textShift, TranslateTransform.XProperty, null, -220, 0.40, 0, CubicIn);
            Animate(_textBlock, UIElement.OpacityProperty, null, 0, 0.40, 0, CubicIn);
            if (_coverFrame != null)
            {
                Animate(_coverShift, TranslateTransform.XProperty, null, 600, 0.45, 0, CubicIn);
                Animate(_coverFrame, UIElement.OpacityProperty, null, 0, 0.45, 0, CubicIn);
            }

            Animate(_bandScale, ScaleTransform.ScaleYProperty, null, 0, 0.45, 0.25, CubicIn);
        }

        public override void LoadImagesAsync(SplashImageSources sources, MonitorArea screen)
        {
            ScreenPixels(screen, out int width, out int height);
            bool useLogo = UseLogo;
            bool hasCover = HasCover;

            LoadInBackground(() => new Loaded
            {
                Artwork = LoadArtwork(width, height),
                Cover = hasCover ? ImageLoader.Load(Sources.Cover, UnitsToPixels(CoverWidth, height), UnitsToPixels(CoverHeight, height), cover: true) : null,
                Logo = useLogo ? ImageLoader.Load(Sources.Logo, UnitsToPixels(500, height), UnitsToPixels(260, height), cover: false) : null
            }, loaded =>
            {
                loaded = loaded ?? new Loaded();
                ShowSharpBackdrop(loaded.Artwork, 40, 1.15, 1.04, 0.6);
                if (_cover != null && loaded.Cover != null)
                {
                    _cover.Source = loaded.Cover;
                    FadeIn(_cover, 0.4, 0, SineOut);
                }

                if (useLogo)
                {
                    LogoLoaded(loaded.Logo);
                }
            });
        }

        // ----- Speed lines -----

        /// <summary>
        /// Moves the speed lines every frame: full speed, then from 2.2 s easing down to a quarter of it over 1 s.
        /// The offset is worked out from the time, so it never drifts with the frame rate.
        /// </summary>
        private void StartLines()
        {
            if (_linesRunning)
            {
                return;
            }

            _linesRunning = true;
            _linesStart = Now();
            CompositionTarget.Rendering += MoveLines;
        }

        private void StopLines()
        {
            if (_linesRunning)
            {
                _linesRunning = false;
                CompositionTarget.Rendering -= MoveLines;
            }
        }

        private void MoveLines(object sender, EventArgs e)
        {
            double distance = LineDistance((Now() - _linesStart).TotalSeconds);
            _linesShift.X = -(distance % TileWidth);
        }

        private static double LineDistance(double t)
        {
            const double slowFrom = 2.2;
            if (t <= slowFrom)
            {
                return LineSpeed * t;
            }

            double ramp = Math.Min(1, t - slowFrom);
            double distance = LineSpeed * (slowFrom + ramp - 0.375 * ramp * ramp);
            return distance + 0.25 * LineSpeed * Math.Max(0, t - slowFrom - 1);
        }

        /// <summary>
        /// One tile of speed lines (1024 × 380 units), drawn once into a bitmap from a fixed seed: 48 horizontal
        /// streaks of 80–500 units, each a white glint fading out to both ends.
        /// </summary>
        private static BitmapSource SpeedLineTile()
        {
            const double scale = 2;
            var random = new Random(7);
            var visual = new DrawingVisual();
            using (DrawingContext context = visual.RenderOpen())
            {
                context.PushTransform(new ScaleTransform(scale, scale * BandHeight / 400));
                for (int i = 0; i < 48; i++)
                {
                    double length = 80 + random.NextDouble() * 420;
                    double x = random.NextDouble() * (TileWidth - length);
                    double y = random.NextDouble() * 400;
                    double thickness = random.NextDouble() < 0.2 ? 3 : 1 + random.NextDouble() * 1.2;
                    byte alpha = (byte)Math.Round((0.06 + random.NextDouble() * 0.22) * 255);
                    var glint = new LinearGradientBrush { StartPoint = new Point(0, 0.5), EndPoint = new Point(1, 0.5) };
                    glint.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 0));
                    glint.GradientStops.Add(new GradientStop(Color.FromArgb(alpha, 255, 255, 255), 0.5));
                    glint.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 1));
                    context.DrawRectangle(glint, null, new Rect(x, y - thickness / 2, length, thickness));
                }

                context.Pop();
            }

            var bitmap = new RenderTargetBitmap((int)(TileWidth * scale), (int)(BandHeight * scale), 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();
            return bitmap;
        }

        private sealed class Loaded
        {
            public Artwork Artwork { get; set; }
            public BitmapSource Cover { get; set; }
            public BitmapSource Logo { get; set; }
        }
    }
}
