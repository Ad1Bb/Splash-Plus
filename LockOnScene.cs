using System;
using System.Collections.Generic;
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
    /// Lock-On: a tactical targeting display. Over the sharp, dimmed artwork lies a faint grid; four brackets fly in
    /// from the screen's corners and lock onto the cover, two thin rings turn around it, and a pointer line leads to
    /// a panel with the name and a few monospace readouts. Without a cover the brackets lock onto a centered panel.
    /// </summary>
    internal sealed class LockOnScene : StyledScene
    {
        // Layout on a centered 1920 × 1080 frame (on 16:10 its empty side margins fall off the screen).
        private const double FrameWidth = 1920;
        private static readonly Rect CoverRect = new Rect(390, 255, 380, 570);
        private static readonly Rect CenterTarget = new Rect(460, 330, 1000, 420);
        private const double PanelLeft = 930;
        private const double PanelTop = 315;
        private const double PanelWidth = 600;
        private const double CenteredPanelLeft = 520;
        private const double CenteredPanelWidth = 880;
        private const double PointerY = 351;
        private const double PointerFrom = 792;
        private const double PointerTo = 914;
        private const double BracketSize = 44;
        private const double BracketReach = 18;
        private const double BracketInset = 40;
        private const double GridStep = 90;
        private const double DimOpacity = 0.30;
        private const double LogoWidthShare = 0.16;
        private const double LogoMaxHeight = 150;

        private bool _started;
        private BitmapSource _arrivedCover;

        private readonly Rectangle _grid = new Rectangle { Opacity = 0, IsHitTestVisible = false };
        private readonly Canvas _frame = new Canvas { Width = FrameWidth, Height = StageHeight };
        private readonly Canvas _overlay = new Canvas();

        private readonly Grid _ringA;
        private readonly Grid _ringB;
        private readonly ScaleTransform _ringAScale = new ScaleTransform(1.15, 1.15);
        private readonly ScaleTransform _ringBScale = new ScaleTransform(1.15, 1.15);
        private readonly RotateTransform _ringATurn = new RotateTransform();
        private readonly RotateTransform _ringBTurn = new RotateTransform();

        private readonly Grid _coverFrame;
        private readonly ScaleTransform _coverFrameScale = new ScaleTransform(1, 1);
        private readonly Image _cover;
        private readonly Grid _scan;
        private readonly TranslateTransform _scanShift = new TranslateTransform();

        private readonly Grid _pointer;
        private readonly ScaleTransform _pointerScale = new ScaleTransform(0, 1);
        private readonly Ellipse _pointerDot;
        private readonly TranslateTransform _pointerDotShift = new TranslateTransform(-(PointerTo - PointerFrom), 0);

        private readonly StackPanel _panel = new StackPanel();
        private readonly TextBlock _platform;
        private readonly TextBlock _name;
        private readonly Image _logo;
        private readonly Rectangle _rule;
        private readonly List<FrameworkElement> _readouts = new List<FrameworkElement>();

        private readonly Bracket[] _brackets = new Bracket[4];

        public LockOnScene(SplashGameInfo info, SplashImageSources sources)
            : base(info, sources)
        {
            bool cover = HasCover;

            // 1–3. Base, the sharp artwork, dimming
            Stage.Children.Add(new Rectangle { Fill = Gray(0x0A) });
            AddSharpBackdrop();
            Stage.Children.Add(Dim(DimOpacity));

            // 4. Grid, fading out toward the edges
            var fade = new RadialGradientBrush { RadiusX = 0.7, RadiusY = 0.7 };
            fade.GradientStops.Add(new GradientStop(Colors.Black, 0));
            fade.GradientStops.Add(new GradientStop(Colors.Black, 0.3));
            fade.GradientStops.Add(new GradientStop(Colors.Transparent, 1));
            _grid.OpacityMask = fade;
            RenderOptions.SetEdgeMode(_grid, EdgeMode.Aliased);
            Stage.Children.Add(_grid);

            // Everything else is placed on the centered frame (and the brackets on the whole stage).
            Stage.Children.Add(new Canvas { Children = { _frame } });

            if (cover)
            {
                // 5. Rings around the cover
                var center = new Point(CoverRect.X + CoverRect.Width / 2, CoverRect.Y + CoverRect.Height / 2);
                _ringA = Ring(center, 330, 0.35, new DoubleCollection { 2 / 1.5, 9 / 1.5 }, 336, 350, 0.55, _ringAScale, _ringATurn);
                double arc = 2 * Math.PI * 314 / 1.5;
                _ringB = Ring(center, 314, 0.55, new DoubleCollection { 0.12 * arc, 0.13 * arc }, 320, 334, 0.75, _ringBScale, _ringBTurn);
                _frame.Children.Add(_ringA);
                _frame.Children.Add(_ringB);

                // 6. Cover frame with the scan line inside
                _cover = CoverImage();
                _cover.Opacity = 0;
                RenderOptions.SetBitmapScalingMode(_cover, BitmapScalingMode.HighQuality);
                _scan = new Grid
                {
                    Height = 2,
                    VerticalAlignment = VerticalAlignment.Top,
                    Opacity = 0,
                    RenderTransform = _scanShift,
                    Children =
                    {
                        new Rectangle
                        {
                            Fill = White(0.45),
                            Margin = new Thickness(-3),
                            Effect = new BlurEffect { Radius = 14, KernelType = KernelType.Gaussian }
                        },
                        new Rectangle { Fill = White(0.85) }
                    }
                };
                _coverFrame = new Grid
                {
                    Width = CoverRect.Width,
                    Height = CoverRect.Height,
                    Opacity = 0,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = _coverFrameScale,
                    Children =
                    {
                        SoftShadow(CoverRect.Width, CoverRect.Height, 0, 24, 60, 0.55),
                        new Grid
                        {
                            ClipToBounds = true,
                            Children = { new Rectangle { Fill = White(0.06) }, _cover, _scan }
                        }
                    }
                };
                Place(_coverFrame, CoverRect.X, CoverRect.Y);
                _frame.Children.Add(_coverFrame);

                // 7. Pointer line from the cover to the panel
                _pointerDot = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = Brushes.White,
                    Opacity = 0,
                    RenderTransform = _pointerDotShift
                };
                Place(_pointerDot, PointerTo - 5, PointerY - 5);
                _pointer = new Grid
                {
                    Width = PointerTo - PointerFrom,
                    Height = 1.5,
                    RenderTransformOrigin = new Point(0, 0.5),
                    RenderTransform = _pointerScale,
                    Children = { new Rectangle { Fill = White(0.7) } }
                };
                Place(_pointer, PointerFrom, PointerY - 0.75);
                _frame.Children.Add(_pointer);
                _frame.Children.Add(_pointerDot);
            }

            // 8. Panel: platform, logo or name, a rule and the readouts
            HorizontalAlignment align = cover ? HorizontalAlignment.Left : HorizontalAlignment.Center;
            TextAlignment textAlign = cover ? TextAlignment.Left : TextAlignment.Center;
            double panelWidth = cover ? PanelWidth : CenteredPanelWidth;
            _panel.Width = panelWidth;

            if (!string.IsNullOrWhiteSpace(Info.Platform))
            {
                _platform = Text(Info.Platform.ToUpperInvariant(), 18, 400, White(0.55), 1.2, 1, Consolas);
                _platform.HorizontalAlignment = align;
                _platform.Margin = new Thickness(0, 0, 0, 14);
                _platform.Opacity = 0;
                _panel.Children.Add(_platform);
            }

            _name = Text(Info.Name, 56, 600, Brushes.White, 1.1, 2);
            _name.HorizontalAlignment = align;
            _name.TextAlignment = textAlign;
            _name.Effect = Shadow(2, 16, 0.45);
            _name.Opacity = 0;
            FitText(_name, panelWidth, 56, 2, 38, 2, 1.1);
            var title = new Grid { Children = { _name } };
            if (UseLogo)
            {
                _logo = new Image { Stretch = Stretch.Uniform, HorizontalAlignment = align, Opacity = 0, Effect = Shadow(3, 14, 0.45) };
                RenderOptions.SetBitmapScalingMode(_logo, BitmapScalingMode.HighQuality);
                title.Children.Add(_logo);
            }

            _panel.Children.Add(title);

            _rule = new Rectangle { Height = 1, Fill = White(0.30), Margin = new Thickness(0, 22, 0, 16), Opacity = 0 };
            _panel.Children.Add(_rule);

            string[] values = { Info.Developer, Info.Genre, Info.ReleaseYear?.ToString(), SplashText.Hours(Info.Playtime) };
            if (cover)
            {
                foreach (string value in values)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        AddReadout("▸ " + value, 1.65, align, textAlign);
                    }
                }
            }
            else
            {
                string line = SplashText.Join("  ·  ", values);
                if (line != null)
                {
                    AddReadout(line, 1.5, align, textAlign);
                }
            }

            if (cover)
            {
                Place(_panel, PanelLeft, PanelTop);
                _frame.Children.Add(_panel);
            }
            else
            {
                _panel.VerticalAlignment = VerticalAlignment.Center;
                var column = new Grid { Width = CenteredPanelWidth, Height = StageHeight, Children = { _panel } };
                Place(column, CenteredPanelLeft, 0);
                _frame.Children.Add(column);
            }

            // 9. Brackets, on top of everything
            Stage.Children.Add(_overlay);
            for (int i = 0; i < 4; i++)
            {
                _brackets[i] = new Bracket(i % 2 == 1, i >= 2);
                _overlay.Children.Add(_brackets[i].Element);
            }
        }

        private Rect Target
        {
            get
            {
                Rect target = HasCover ? CoverRect : CenterTarget;
                target.Inflate(BracketReach, BracketReach);
                return target;
            }
        }

        protected override void OnStageWidth(double width)
        {
            double left = width / 2 - FrameWidth / 2;
            Canvas.SetLeft(_frame, left);

            // Grid lines one screen pixel thin, crossing at the screen's center.
            double step = SnapToPixels(GridStep);
            double line = OnePixel;
            var tile = new DrawingGroup();
            tile.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, step, step))));
            tile.Children.Add(new GeometryDrawing(White(0.06), null, new RectangleGeometry(new Rect(0, 0, step, line))));
            tile.Children.Add(new GeometryDrawing(White(0.06), null, new RectangleGeometry(new Rect(0, line, line, step - line))));
            _grid.Fill = new DrawingBrush(tile)
            {
                TileMode = TileMode.Tile,
                Stretch = Stretch.Fill,
                Viewbox = new Rect(0, 0, step, step),
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(Modulo(width / 2, step), Modulo(StageHeight / 2, step), step, step),
                ViewportUnits = BrushMappingMode.Absolute
            };

            // Brackets: their place on the target, and the screen corner each one flies in from.
            Rect target = Target;
            for (int i = 0; i < 4; i++)
            {
                Bracket bracket = _brackets[i];
                double x = left + (bracket.Right ? target.Right - BracketSize : target.Left);
                double y = bracket.Bottom ? target.Bottom - BracketSize : target.Top;
                Canvas.SetLeft(bracket.Element, x);
                Canvas.SetTop(bracket.Element, y);
                double startX = bracket.Right ? width - BracketInset - BracketSize : BracketInset;
                double startY = bracket.Bottom ? StageHeight - BracketInset - BracketSize : BracketInset;
                bracket.Away = new Vector(startX - x, startY - y);
                if (!_started)
                {
                    bracket.Fly.X = bracket.Away.X;
                    bracket.Fly.Y = bracket.Away.Y;
                }
            }
        }

        private static double Modulo(double value, double step) => value - step * Math.Floor(value / step);

        protected override void Start()
        {
            _started = true;
            Animate(_grid, UIElement.OpacityProperty, 0, 1, 0.6, 0.1);
            if (_coverFrame != null)
            {
                Animate(_coverFrame, UIElement.OpacityProperty, 0, 1, 0.5, 0.2);
            }

            // Brackets fly in from the corners, then twitch outward twice as they lock on.
            for (int i = 0; i < 4; i++)
            {
                Bracket bracket = _brackets[i];
                double delay = 0.25 + i * 0.04;
                Animate(bracket.Fly, TranslateTransform.XProperty, bracket.Away.X, 0, 0.75, delay, Overshoot);
                Animate(bracket.Fly, TranslateTransform.YProperty, bracket.Away.Y, 0, 0.75, delay, Overshoot);
                Animate(bracket.Element, UIElement.OpacityProperty, 0, 1, 0.1125, delay);
                double dx = bracket.Right ? 8 : -8;
                double dy = bracket.Bottom ? 8 : -8;
                bracket.Lock.BeginAnimation(TranslateTransform.XProperty, Twitch(dx));
                bracket.Lock.BeginAnimation(TranslateTransform.YProperty, Twitch(dy));
            }

            if (_ringA != null)
            {
                ShowRing(_ringA, _ringAScale, _ringATurn, 1.1, 360, 40);
                ShowRing(_ringB, _ringBScale, _ringBTurn, 1.2, -360, 26);
                Animate(_pointerScale, ScaleTransform.ScaleXProperty, 0, 1, 0.45, 1.15, ExpoOut);
                Animate(_pointerDotShift, TranslateTransform.XProperty, -(PointerTo - PointerFrom), 0, 0.45, 1.15, ExpoOut);
                CutTo(_pointerDot, 1, 1.15);
                if (_arrivedCover != null)
                {
                    ShowCover(_arrivedCover);
                }
            }

            WaitForLogo();
        }

        private static DoubleAnimationUsingKeyFrames Twitch(double distance)
        {
            return Keys(1.08, (0, 0, null), (0.105, distance, null), (0.21, 0, null), (0.315, distance, null), (0.42, 0, null));
        }

        private static void ShowRing(Grid ring, ScaleTransform scale, RotateTransform turn, double delay, double angle, double seconds)
        {
            Animate(ring, UIElement.OpacityProperty, 0, 1, 0.7, delay, ExpoOut);
            Animate(scale, ScaleTransform.ScaleXProperty, 1.15, 1, 0.7, delay, ExpoOut);
            Animate(scale, ScaleTransform.ScaleYProperty, 1.15, 1, 0.7, delay, ExpoOut);
            turn.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, angle, TimeSpan.FromSeconds(seconds))
            {
                BeginTime = TimeSpan.FromSeconds(delay),
                RepeatBehavior = RepeatBehavior.Forever
            });
        }

        protected override void ShowLogo(BitmapSource logo)
        {
            _name.Visibility = Visibility.Collapsed;
            _logo.Source = logo;
            FitLogo(_logo, logo, Math.Min(LogoWidthShare * StageWidth, _panel.Width), LogoMaxHeight);
            _logo.BeginAnimation(UIElement.OpacityProperty,
                StepsFrom(FromShow(1.3), (0, 0), (0.1, 0.3), (0.2, 0.1), (0.3, 1)));
            RevealPanel(includeName: false);
        }

        protected override void ShowName()
        {
            if (_logo != null)
            {
                _logo.Visibility = Visibility.Collapsed;
            }

            bool hasName = !string.IsNullOrWhiteSpace(Info.Name);
            if (!hasName)
            {
                _name.Visibility = Visibility.Collapsed;
            }

            RevealPanel(includeName: hasName);
        }

        /// <summary>Each line of the panel flickers on in turn: 0 → 1 → 0.2 → 1 in four 60 ms frames.</summary>
        private void RevealPanel(bool includeName)
        {
            var items = new List<FrameworkElement>();
            if (_platform != null)
            {
                items.Add(_platform);
            }

            if (includeName)
            {
                items.Add(_name);
            }

            items.Add(_rule);
            items.AddRange(_readouts);
            for (int i = 0; i < items.Count; i++)
            {
                items[i].BeginAnimation(UIElement.OpacityProperty,
                    StepsFrom(FromShow(1.3 + i * 0.11), (0, 0), (0.06, 1), (0.12, 0.2), (0.18, 1)));
            }
        }

        private static DoubleAnimationUsingKeyFrames StepsFrom(double delay, params (double At, double Value)[] steps)
        {
            DoubleAnimationUsingKeyFrames animation = Steps(steps);
            animation.BeginTime = TimeSpan.FromSeconds(delay);
            return animation;
        }

        /// <summary>The cover fades in; the scan starts sweeping it (not before 1.1 s), once every 7 s.</summary>
        private void ShowCover(BitmapSource cover)
        {
            _cover.Source = cover;
            FadeIn(_cover, 0.5, 0, SineOut);

            double at = FromShow(1.1);
            double height = CoverRect.Height;
            var sweep = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromSeconds(7),
                BeginTime = TimeSpan.FromSeconds(at),
                RepeatBehavior = RepeatBehavior.Forever
            };
            sweep.KeyFrames.Add(new DiscreteDoubleKeyFrame(-0.01 * height, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            sweep.KeyFrames.Add(new EasingDoubleKeyFrame(height, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.91)), SineInOut));
            _scanShift.BeginAnimation(TranslateTransform.YProperty, sweep);

            var visible = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromSeconds(7),
                BeginTime = TimeSpan.FromSeconds(at),
                RepeatBehavior = RepeatBehavior.Forever
            };
            visible.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            visible.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.91))));
            _scan.BeginAnimation(UIElement.OpacityProperty, visible);
        }

        protected override void Close()
        {
            Animate(_panel, UIElement.OpacityProperty, null, 0, 0.3);
            if (_ringA != null)
            {
                Animate(_pointer, UIElement.OpacityProperty, null, 0, 0.2);
                Animate(_pointerDot, UIElement.OpacityProperty, null, 0, 0.2);
                foreach ((Grid ring, ScaleTransform scale) in new[] { (_ringA, _ringAScale), (_ringB, _ringBScale) })
                {
                    Animate(scale, ScaleTransform.ScaleXProperty, null, 1.3, 0.45, 0, CubicIn);
                    Animate(scale, ScaleTransform.ScaleYProperty, null, 1.3, 0.45, 0, CubicIn);
                    Animate(ring, UIElement.OpacityProperty, null, 0, 0.45, 0, CubicIn);
                }

                Animate(_coverFrameScale, ScaleTransform.ScaleXProperty, null, 0.97, 0.5, 0.15, CubicIn);
                Animate(_coverFrameScale, ScaleTransform.ScaleYProperty, null, 0.97, 0.5, 0.15, CubicIn);
                Animate(_coverFrame, UIElement.OpacityProperty, null, 0, 0.5, 0.15, CubicIn);
            }

            foreach (Bracket bracket in _brackets)
            {
                bracket.Lock.BeginAnimation(TranslateTransform.XProperty, null);
                bracket.Lock.BeginAnimation(TranslateTransform.YProperty, null);
                Animate(bracket.Fly, TranslateTransform.XProperty, null, bracket.Away.X, 0.5, 0.1, CubicIn);
                Animate(bracket.Fly, TranslateTransform.YProperty, null, bracket.Away.Y, 0.5, 0.1, CubicIn);
                Animate(bracket.Element, UIElement.OpacityProperty, null, 0, 0.5, 0.1, CubicIn);
            }
        }

        public override void LoadImagesAsync(SplashImageSources sources, MonitorArea screen)
        {
            ScreenPixels(screen, out int width, out int height);
            bool useLogo = UseLogo;
            bool hasCover = HasCover;
            int logoWidth = (int)(width * LogoWidthShare);

            LoadInBackground(() => new Loaded
            {
                Artwork = LoadArtwork(width, height),
                Cover = hasCover ? ImageLoader.Load(Sources.Cover, UnitsToPixels(CoverRect.Width, height), UnitsToPixels(CoverRect.Height, height), cover: true) : null,
                Logo = useLogo ? ImageLoader.Load(Sources.Logo, logoWidth, UnitsToPixels(LogoMaxHeight, height), cover: false) : null
            }, loaded =>
            {
                loaded = loaded ?? new Loaded();
                ShowSharpBackdrop(loaded.Artwork, 32, 1.2);
                if (_cover != null && loaded.Cover != null)
                {
                    if (_started)
                    {
                        ShowCover(loaded.Cover);
                    }
                    else
                    {
                        _arrivedCover = loaded.Cover;
                    }
                }

                if (useLogo)
                {
                    LogoLoaded(loaded.Logo);
                }
            });
        }

        private void AddReadout(string text, double lineHeight, HorizontalAlignment align, TextAlignment textAlign)
        {
            TextBlock readout = Text(text, 22, 400, White(0.88), lineHeight, 1, Consolas);
            readout.HorizontalAlignment = align;
            readout.TextAlignment = textAlign;
            readout.MaxWidth = _panel.Width;
            readout.Effect = Shadow(2, 16, 0.45);
            readout.Opacity = 0;
            _panel.Children.Add(readout);
            _readouts.Add(readout);
        }

        /// <summary>
        /// A ring (dashed circle of <paramref name="radius"/>) with four radial ticks, cached as a bitmap and turned
        /// around <paramref name="center"/>.
        /// </summary>
        private static Grid Ring(Point center, double radius, double alpha, DoubleCollection dashes, double tickFrom, double tickTo,
            double tickAlpha, ScaleTransform scale, RotateTransform turn)
        {
            double half = tickTo + 2;
            var ticks = new GeometryGroup();
            for (int k = 0; k < 4; k++)
            {
                double angle = k * Math.PI / 2;
                var direction = new Vector(Math.Cos(angle), Math.Sin(angle));
                ticks.Children.Add(new LineGeometry(new Point(half, half) + direction * tickFrom, new Point(half, half) + direction * tickTo));
            }

            var transforms = new TransformGroup();
            transforms.Children.Add(scale);
            transforms.Children.Add(turn);
            var ring = new Grid
            {
                Width = 2 * half,
                Height = 2 * half,
                Opacity = 0,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = transforms,
                Children =
                {
                    new Grid
                    {
                        CacheMode = new BitmapCache(),
                        Children =
                        {
                            new Path
                            {
                                Data = new EllipseGeometry(new Point(half, half), radius, radius),
                                Stroke = White(alpha),
                                StrokeThickness = 1.5,
                                StrokeDashArray = dashes
                            },
                            new Path { Data = ticks, Stroke = White(tickAlpha), StrokeThickness = 2 }
                        }
                    }
                }
            };
            Place(ring, center.X - half, center.Y - half);
            return ring;
        }

        private static void Place(UIElement element, double x, double y)
        {
            Canvas.SetLeft(element, x);
            Canvas.SetTop(element, y);
        }

        private static IEasingFunction Overshoot
        {
            get
            {
                var ease = new BackEase { Amplitude = 0.5, EasingMode = EasingMode.EaseOut };
                ease.Freeze();
                return ease;
            }
        }

        private sealed class Loaded
        {
            public Artwork Artwork { get; set; }
            public BitmapSource Cover { get; set; }
            public BitmapSource Logo { get; set; }
        }

        /// <summary>One L-shaped targeting bracket: two 3 u strokes on the outer sides of a 44 u square.</summary>
        private sealed class Bracket
        {
            public Bracket(bool right, bool bottom)
            {
                Right = right;
                Bottom = bottom;
                var transforms = new TransformGroup();
                transforms.Children.Add(Lock);
                transforms.Children.Add(Fly);
                Element = new Grid
                {
                    Width = BracketSize,
                    Height = BracketSize,
                    Opacity = 0,
                    RenderTransform = transforms,
                    Effect = new DropShadowEffect { Color = Colors.Black, ShadowDepth = 0, BlurRadius = 6, Opacity = 0.6 },
                    Children =
                    {
                        new Rectangle
                        {
                            Height = 3,
                            Fill = Brushes.White,
                            VerticalAlignment = bottom ? VerticalAlignment.Bottom : VerticalAlignment.Top
                        },
                        new Rectangle
                        {
                            Width = 3,
                            Fill = Brushes.White,
                            HorizontalAlignment = right ? HorizontalAlignment.Right : HorizontalAlignment.Left
                        }
                    }
                };
            }

            public bool Right { get; }

            public bool Bottom { get; }

            public Grid Element { get; }

            /// <summary>The flight from the screen corner (offset from the final place).</summary>
            public TranslateTransform Fly { get; } = new TranslateTransform();

            /// <summary>The lock-on twitch, separate from the flight.</summary>
            public TranslateTransform Lock { get; } = new TranslateTransform();

            /// <summary>Offset of the start corner from the final place.</summary>
            public Vector Away { get; set; }
        }
    }
}
