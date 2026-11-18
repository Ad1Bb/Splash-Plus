using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static SplashAddonPlus.StyleKit;

namespace SplashAddonPlus
{
    /// <summary>
    /// Cinema: letterbox bars slide in, the blurred backdrop drifts slowly sideways, the title is revealed by a soft
    /// left-to-right wipe, then a thin rule and one row of details. On closing the bars meet like a shutter.
    /// </summary>
    internal sealed class CinemaScene : StyledScene
    {
        // The backdrop is always blurred; the extra scale hides the soft edges of the blur during the drift.
        private const double BackdropScale = 1.14;
        private const double BackgroundBlurRadius = 24;
        private const double CoverBlurRadius = 40;
        private const double CoverDim = 0.35;
        private const double LogoWidthShare = 0.30;
        private const double LogoMaxHeight = 360;
        private const double DriftShare = 0.016;
        private const double BottomGap = 70;

        private readonly Grid _backdropGroup = new Grid { Opacity = 0 };
        private readonly Image _backdrop = CoverImage();
        private readonly TranslateTransform _drift = new TranslateTransform();
        private readonly Rectangle _dim = Dim(0);
        private readonly StackPanel _title = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom };
        private readonly Image _logo;
        private readonly TranslateTransform _logoShift = new TranslateTransform(0, 14);
        private readonly TextBlock _name;
        private readonly GradientStop _wipeSolid = new GradientStop(Colors.Black, -1.0);
        private readonly GradientStop _wipeClear = new GradientStop(Color.FromArgb(0, 0, 0, 0), -0.5);
        private readonly Rectangle _rule;
        private readonly TextBlock _meta;
        private readonly TranslateTransform _metaShift = new TranslateTransform(0, 10);
        private readonly Rectangle _topBar = new Rectangle { Fill = Brushes.Black, VerticalAlignment = VerticalAlignment.Top };
        private readonly Rectangle _bottomBar = new Rectangle { Fill = Brushes.Black, VerticalAlignment = VerticalAlignment.Bottom };
        private readonly TranslateTransform _topBarShift = new TranslateTransform();
        private readonly TranslateTransform _bottomBarShift = new TranslateTransform();

        public CinemaScene(SplashGameInfo info, SplashImageSources sources)
            : base(info, sources)
        {
            // 1. Base
            var baseBrush = new RadialGradientBrush { Center = new Point(0.5, 0.35), GradientOrigin = new Point(0.5, 0.35), RadiusX = 0.71, RadiusY = 0.92 };
            baseBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x1C, 0x1C, 0x1C), 0));
            baseBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x07, 0x07, 0x07), 0.7));
            Stage.Children.Add(new Rectangle { Fill = baseBrush });

            // 2–3. Backdrop with its dimming, drifting sideways
            var transforms = new TransformGroup();
            transforms.Children.Add(new ScaleTransform(BackdropScale, BackdropScale));
            transforms.Children.Add(_drift);
            _backdrop.RenderTransform = transforms;
            _backdropGroup.Children.Add(_backdrop);
            _backdropGroup.Children.Add(_dim);
            Stage.Children.Add(_backdropGroup);

            // 5. Bottom shade
            var shade = new LinearGradientBrush { StartPoint = new Point(0, 1), EndPoint = new Point(0, 0) };
            shade.GradientStops.Add(new GradientStop(Color.FromArgb(209, 0, 0, 0), 0));
            shade.GradientStops.Add(new GradientStop(Color.FromArgb(89, 0, 0, 0), 0.32));
            shade.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.58));
            Stage.Children.Add(new Rectangle { Fill = shade });

            // 7. Title block: logo or name, rule, details
            if (UseLogo)
            {
                _logo = new Image
                {
                    Stretch = Stretch.Uniform,
                    MaxHeight = LogoMaxHeight,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Opacity = 0,
                    RenderTransform = _logoShift,
                    Effect = Shadow(4, 18, 0.6)
                };
                _title.Children.Add(_logo);
            }
            else
            {
                _name = Text(Info.Name, 80, 300, Brushes.White, 1.14, 2);
                _name.TextAlignment = TextAlignment.Center;
                _name.HorizontalAlignment = HorizontalAlignment.Center;
                _name.Effect = Shadow(2, 24, 0.7);
                var wipe = new LinearGradientBrush { StartPoint = new Point(0, 0.5), EndPoint = new Point(1, 0.5) };
                wipe.GradientStops.Add(_wipeSolid);
                wipe.GradientStops.Add(_wipeClear);
                _name.OpacityMask = wipe;
                _title.Children.Add(_name);
            }

            var ruleBrush = new LinearGradientBrush { StartPoint = new Point(0, 0.5), EndPoint = new Point(1, 0.5) };
            ruleBrush.GradientStops.Add(Stop(0, 255, 0));
            ruleBrush.GradientStops.Add(Stop(191, 255, 0.5));
            ruleBrush.GradientStops.Add(Stop(0, 255, 1));
            _rule = new Rectangle
            {
                Width = 300,
                Height = 2,
                Margin = new Thickness(0, 26, 0, 0),
                Fill = ruleBrush,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(0, 1)
            };
            _title.Children.Add(_rule);

            string meta = SplashText.Join(Separator, Info.ReleaseYear?.ToString(), Info.Developer, Info.Genre);
            if (meta != null)
            {
                _meta = Text(meta, 26, 350, White(0.75));
                _meta.HorizontalAlignment = HorizontalAlignment.Center;
                _meta.Margin = new Thickness(0, 22, 0, 0);
                _meta.Opacity = 0;
                _meta.RenderTransform = _metaShift;
                _title.Children.Add(_meta);
            }

            Stage.Children.Add(_title);

            // 6. Letterbox bars (in front, so the shutter covers everything when closing)
            _topBar.RenderTransform = _topBarShift;
            _bottomBar.RenderTransform = _bottomBarShift;
            Stage.Children.Add(_topBar);
            Stage.Children.Add(_bottomBar);
        }

        protected override void OnStageWidth(double width)
        {
            double barHeight = SplashText.LetterboxShare(width, StageHeight) * StageHeight;
            _topBar.Height = barHeight;
            _bottomBar.Height = barHeight;
            _title.Margin = new Thickness(0.06 * width, 0, 0.06 * width, barHeight + BottomGap);
            _title.MaxWidth = width * 0.88;
            if (_logo != null)
            {
                _logo.Width = LogoWidthShare * width;
            }

            if (_name != null)
            {
                FitText(_name, Math.Min(1500, 0.88 * width), 80, 2, 60, 2, 1.14);
            }
        }

        protected override void Start()
        {
            double barHeight = _topBar.Height;
            Animate(_topBarShift, TranslateTransform.YProperty, -barHeight, 0, 1.0, 0.1, QuintOut);
            Animate(_bottomBarShift, TranslateTransform.YProperty, barHeight, 0, 1.0, 0.1, QuintOut);

            if (_name != null)
            {
                Animate(_wipeSolid, GradientStop.OffsetProperty, -1.0, 1.0, 1.6, 0.55, CubicOut);
                Animate(_wipeClear, GradientStop.OffsetProperty, -0.5, 1.5, 1.6, 0.55, CubicOut);
            }

            Animate(_rule.RenderTransform, ScaleTransform.ScaleXProperty, 0, 1, 1.2, 1.0, ExpoOut);
            if (_meta != null)
            {
                Animate(_meta, UIElement.OpacityProperty, 0, 1, 0.9, 1.3, CubicOut);
                Animate(_metaShift, TranslateTransform.YProperty, 10, 0, 0.9, 1.3, CubicOut);
            }

            double drift = DriftShare * StageWidth;
            Loop(_drift, TranslateTransform.XProperty, -drift, drift, 26, SineInOut, 30);
        }

        protected override void Close()
        {
            double shutter = 0.505 * StageHeight;
            Animate(_topBar, FrameworkElement.HeightProperty, null, shutter, 0.8, 0, CubicIn);
            Animate(_bottomBar, FrameworkElement.HeightProperty, null, shutter, 0.8, 0, CubicIn);
            Animate(_title, UIElement.OpacityProperty, null, 0, 0.35, 0, QuadIn);
        }

        public override void LoadImagesAsync(SplashImageSources sources, MonitorArea screen)
        {
            ScreenPixels(screen, out int width, out int height);
            bool useLogo = UseLogo;

            LoadInBackground(() =>
            {
                // A small decode is enough for a blurred backdrop: the background, or else the cover.
                var loaded = new Loaded { Backdrop = ImageLoader.Load(Sources.Background, 480, 0, cover: false) };
                if (loaded.Backdrop == null)
                {
                    loaded.Backdrop = ImageLoader.Load(Sources.Cover, 480, 0, cover: false);
                    loaded.BackdropIsCover = loaded.Backdrop != null;
                }

                loaded.Logo = useLogo ? ImageLoader.Load(Sources.Logo, (int)(width * LogoWidthShare), UnitsToPixels(LogoMaxHeight, height), cover: false) : null;
                return loaded;
            }, loaded =>
            {
                loaded = loaded ?? new Loaded();
                if (loaded.Backdrop != null)
                {
                    _backdrop.Source = loaded.BackdropIsCover
                        ? Blur(loaded.Backdrop, CoverBlurRadius / StageHeight)
                        : Blur(loaded.Backdrop, BackgroundBlurRadius / StageHeight);
                    _dim.Opacity = loaded.BackdropIsCover ? CoverDim : 0;
                    FadeIn(_backdropGroup, 1.0, 0, SineOut);
                }

                if (_logo != null && loaded.Logo != null)
                {
                    _logo.Source = loaded.Logo;
                    Animate(_logo, UIElement.OpacityProperty, 0, 1, 1.1, 0, CubicOut);
                    Animate(_logoShift, TranslateTransform.YProperty, 14, 0, 1.1, 0, CubicOut);
                }
            });
        }

        private sealed class Loaded
        {
            public BitmapSource Backdrop { get; set; }
            public bool BackdropIsCover { get; set; }
            public BitmapSource Logo { get; set; }
        }
    }
}
