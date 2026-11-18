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
    /// Plinth: the sharp cover rises from behind a thin horizon line like an exhibit on a plinth, over the sharp
    /// artwork; below the line lies a floor of frosted glass with the logo or name and one line of details.
    /// Without a cover, the logo or name itself rises on the line.
    /// </summary>
    internal sealed class PlinthScene : StyledScene
    {
        private const double Horizon = 920;
        private const double CoverHeight = 760;
        private const double FloorHeight = StageHeight - Horizon;
        private const double LineGap = 36;

        private readonly double _coverWidth;
        private readonly GlassPane _floor = new GlassPane(0);
        private readonly Rectangle _glow;
        private readonly Rectangle _line;
        private readonly ScaleTransform _lineScale = new ScaleTransform(0, 1);

        // On the line: the cover, or (without a cover) the logo or name
        private readonly Grid _coverFrame;
        private readonly Image _cover;
        private readonly Grid _lineTitle;
        private readonly TranslateTransform _riseShift = new TranslateTransform(0, 2 * StageHeight);

        // On the floor: the logo or name (with a cover), then the details
        private readonly StackPanel _block = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        private readonly Grid _floorTitle;
        private readonly TranslateTransform _floorTitleShift = new TranslateTransform(0, 10);
        private readonly TextBlock _meta;
        private readonly TranslateTransform _metaShift = new TranslateTransform(0, 8);

        private readonly TextBlock _name;
        private readonly Image _logo;

        public PlinthScene(SplashGameInfo info, SplashImageSources sources)
            : base(info, sources)
        {
            _coverWidth = CoverHeight * CoverAspect;

            // 1–2. Base and the sharp artwork (no dimming)
            Stage.Children.Add(new Rectangle { Fill = Gray(0x07) });
            AddSharpBackdrop();

            // 3. Floor: the artwork under it blurred, darker towards the bottom
            if (HasCover || !string.IsNullOrEmpty(Sources.Background))
            {
                _floor.Margin = new Thickness(0, Horizon, 0, 0);
                Stage.Children.Add(_floor);
            }

            var shade = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
            shade.GradientStops.Add(new GradientStop(Color.FromArgb(89, 0, 0, 0), 0));
            shade.GradientStops.Add(new GradientStop(Color.FromArgb(153, 0, 0, 0), 1));
            Stage.Children.Add(new Rectangle { Fill = shade, Margin = new Thickness(0, Horizon, 0, 0) });

            // 4. Glow on the floor
            var glow = new RadialGradientBrush();
            glow.GradientStops.Add(Stop(20, 255, 0));
            glow.GradientStops.Add(Stop(0, 255, 1));
            _glow = new Rectangle
            {
                Width = 1100,
                Height = 160,
                Fill = glow,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, Horizon - 80, 0, 0),
                Opacity = 0
            };
            Stage.Children.Add(_glow);

            // 5. Horizon line
            var lineBrush = new LinearGradientBrush { StartPoint = new Point(0, 0.5), EndPoint = new Point(1, 0.5) };
            lineBrush.GradientStops.Add(Stop(0, 255, 0.05));
            lineBrush.GradientStops.Add(Stop(115, 255, 0.5));
            lineBrush.GradientStops.Add(Stop(0, 255, 0.95));
            _line = new Rectangle
            {
                Fill = lineBrush,
                VerticalAlignment = VerticalAlignment.Top,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = _lineScale
            };
            Stage.Children.Add(_line);

            // The logo or the name: on the floor with a cover, on the line without one.
            var title = new Grid { HorizontalAlignment = HorizontalAlignment.Center };
            _name = Text(Info.Name, HasCover ? 34 : 80, 300, Brushes.White, HasCover ? 1.15 : 1.1, 2);
            _name.TextAlignment = TextAlignment.Center;
            _name.HorizontalAlignment = HorizontalAlignment.Center;
            title.Children.Add(_name);
            if (UseLogo)
            {
                _logo = new Image { Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center };
                RenderOptions.SetBitmapScalingMode(_logo, BitmapScalingMode.HighQuality);
                title.Children.Add(_logo);
            }

            // 6. Above the line, clipped so things rise from behind it
            var above = new Grid { Height = Horizon, VerticalAlignment = VerticalAlignment.Top, ClipToBounds = true };
            if (HasCover)
            {
                _coverFrame = CoverFrame(_coverWidth, CoverHeight, 8, true, null, null, out _cover);

                // A wide and a close shadow all round set the cover off from the sharp artwork.
                _coverFrame.Children.Insert(0, SoftShadow(_coverWidth, CoverHeight, 8, 0, 50, 0.6));
                _coverFrame.Children.Insert(0, SoftShadow(_coverWidth, CoverHeight, 8, 0, 160, 0.75));
                _coverFrame.Children.Add(new Border
                {
                    CornerRadius = new CornerRadius(8, 8, 0, 0),
                    BorderThickness = new Thickness(1, 1, 1, 0),
                    BorderBrush = White(0.10)
                });
                _coverFrame.HorizontalAlignment = HorizontalAlignment.Center;
                _coverFrame.VerticalAlignment = VerticalAlignment.Bottom;
                _coverFrame.RenderTransform = _riseShift;
                above.Children.Add(_coverFrame);

                _floorTitle = title;
                _floorTitle.Opacity = 0;
                _floorTitle.RenderTransform = _floorTitleShift;
                _block.Children.Add(_floorTitle);
            }
            else
            {
                _lineTitle = title;
                _lineTitle.VerticalAlignment = VerticalAlignment.Bottom;
                _lineTitle.Margin = new Thickness(0, 0, 0, LineGap);
                _lineTitle.RenderTransform = _riseShift;
                above.Children.Add(_lineTitle);
            }

            Stage.Children.Add(above);

            // 7. The details under the title, on the floor
            string meta = SplashText.Join(Separator, Info.ReleaseYear?.ToString(), Info.Developer, Info.Genre);
            if (meta != null)
            {
                _meta = Text(meta, 17, 350, White(0.65));
                _meta.HorizontalAlignment = HorizontalAlignment.Center;
                _meta.Margin = new Thickness(0, HasCover ? 10 : 0, 0, 0);
                _meta.Opacity = 0;
                _meta.RenderTransform = _metaShift;
                _block.Children.Add(_meta);
            }

            Stage.Children.Add(new Grid { Height = FloorHeight, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, Horizon, 0, 0), Children = { _block } });
        }

        private double LogoWidthShare => HasCover ? 0.14 : 0.21;

        private double LogoMaxHeight => HasCover ? 70 : 320;

        protected override void OnStageWidth(double width)
        {
            double thickness = Math.Max(1.5, OnePixel);
            _line.Height = thickness;
            _line.Margin = new Thickness(0, Horizon - thickness / 2, 0, 0);

            _floor.SetSize(width, FloorHeight);
            _floor.AlignCopy(new Point(0, Horizon), width, StageHeight);

            double block = 0.8 * width;
            _block.MaxWidth = block;
            if (HasCover)
            {
                FitText(_name, Math.Min(1200, block), 34, 2, 26, 2, 1.15);
            }
            else
            {
                FitText(_name, block, 80, 2, 56, 2, 1.1);
            }
        }

        protected override void Start()
        {
            Animate(_lineScale, ScaleTransform.ScaleXProperty, 0, 1, 1.3, 0.2, ExpoOutBy(7));
            Animate(_glow, UIElement.OpacityProperty, 0, 1, 1.2, 0.5);
            if (_meta != null)
            {
                double delay = _floorTitle != null ? 1.05 : 0.9;
                Animate(_meta, UIElement.OpacityProperty, 0, 1, 1.0, delay, CubicOut);
                Animate(_metaShift, TranslateTransform.YProperty, 8, 0, 1.0, delay, CubicOut);
            }

            WaitForLogo();
        }

        protected override void ShowLogo(BitmapSource logo)
        {
            _name.Visibility = Visibility.Collapsed;
            _logo.Source = logo;
            FitLogo(_logo, logo, LogoWidthShare * StageWidth, LogoMaxHeight);
            RevealTitle();
        }

        protected override void ShowName()
        {
            if (_logo != null)
            {
                _logo.Visibility = Visibility.Collapsed;
            }

            RevealTitle();
        }

        private void RevealTitle()
        {
            if (_lineTitle != null)
            {
                // Rises from behind the line: from 140 % of its own height below.
                _lineTitle.Measure(new Size(StageWidth, double.PositiveInfinity));
                double from = 1.4 * Math.Max(1, _lineTitle.DesiredSize.Height - LineGap);
                _riseShift.Y = from;
                Animate(_riseShift, TranslateTransform.YProperty, from, 0, 1.3, FromShow(0.5), ExpoOutBy(7));
            }
            else
            {
                Animate(_floorTitle, UIElement.OpacityProperty, 0, 1, 1.0, FromShow(0.9), CubicOut);
                Animate(_floorTitleShift, TranslateTransform.YProperty, 10, 0, 1.0, FromShow(0.9), CubicOut);
            }
        }

        protected override void Close()
        {
            FrameworkElement onLine = (FrameworkElement)_coverFrame ?? _lineTitle;
            Animate(_riseShift, TranslateTransform.YProperty, null, onLine.ActualHeight + 60, 0.6, 0, CubicIn);
            Animate(_lineScale, ScaleTransform.ScaleXProperty, null, 0, 0.7, 0.2, CubicIn);
            Animate(_glow, UIElement.OpacityProperty, null, 0, 0.5, 0.2);
            Animate(_block, UIElement.OpacityProperty, null, 0, 0.3);
        }

        public override void LoadImagesAsync(SplashImageSources sources, MonitorArea screen)
        {
            ScreenPixels(screen, out int width, out int height);
            bool hasCover = HasCover;
            bool useLogo = UseLogo;
            double coverWidth = _coverWidth;
            int logoWidth = (int)(width * LogoWidthShare);
            int logoHeight = UnitsToPixels(LogoMaxHeight, height);

            LoadInBackground(() => new Loaded
            {
                Artwork = LoadArtwork(width, height),
                Cover = hasCover ? ImageLoader.Load(Sources.Cover, UnitsToPixels(coverWidth, height), UnitsToPixels(CoverHeight, height), cover: true) : null,
                Logo = useLogo ? ImageLoader.Load(Sources.Logo, logoWidth, logoHeight, cover: false) : null
            }, loaded =>
            {
                loaded = loaded ?? new Loaded();
                ShowSharpBackdrop(loaded.Artwork);
                BitmapSource floor = GlassCopy(loaded.Artwork, 28, 44);
                if (floor != null)
                {
                    _floor.SetImage(floor, loaded.Artwork.FromCover ? 1.2 : 1.06);
                }

                if (_cover != null && loaded.Cover != null)
                {
                    _cover.Source = loaded.Cover;
                    _cover.Opacity = 1;
                    Animate(_riseShift, TranslateTransform.YProperty, 1.01 * CoverHeight, 0, 1.3, 0, ExpoOutBy(7));
                }

                if (useLogo)
                {
                    LogoLoaded(loaded.Logo);
                }
            });
        }

        private sealed class Loaded
        {
            public Artwork Artwork { get; set; }
            public BitmapSource Cover { get; set; }
            public BitmapSource Logo { get; set; }
        }
    }
}
