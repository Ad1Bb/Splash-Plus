using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static SplashAddonPlus.StyleKit;

namespace SplashAddonPlus
{
    /// <summary>
    /// Band: a translucent dark strip spans the sharp artwork, blurring what lies under it; the sharp cover cuts
    /// across it, and the logo or name, a line of details and the play time with the library stand centered beside it.
    /// </summary>
    internal sealed class BandScene : StyledScene
    {
        private const double BandCenter = 0.52 * StageHeight;
        private const double BandHeight = 432;
        private const double CoverHeight = 640;
        private const double CoverLeft = 250;
        private const double TextGap = 100;
        private const double TextRight = 160;
        private const double LogoWidthShare = 0.15;
        private const double LogoMaxHeight = 200;

        private readonly double _coverWidth;

        private readonly GlassPane _glass = new GlassPane(0);
        private readonly Border _band;
        private readonly ScaleTransform _bandScale = new ScaleTransform(1, 0, 0, BandHeight / 2);

        private readonly Grid _coverFrame;
        private readonly Image _cover;
        private readonly TranslateTransform _coverShift = new TranslateTransform(0, 30);
        private readonly TranslateTransform _frameShift = new TranslateTransform();

        private readonly Grid _textArea;
        private readonly StackPanel _text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        private readonly TextBlock _name;
        private readonly Image _logo;
        private readonly TranslateTransform _logoShift = new TranslateTransform(20, 0);
        private readonly List<FrameworkElement> _lines = new List<FrameworkElement>();

        public BandScene(SplashGameInfo info, SplashImageSources sources)
            : base(info, sources)
        {
            _coverWidth = CoverHeight * CoverAspect;
            const HorizontalAlignment align = HorizontalAlignment.Center;

            // 1–2. Base and the sharp artwork (no dimming)
            Stage.Children.Add(new Rectangle { Fill = Gray(0x10) });
            AddSharpBackdrop();

            // 3. The strip across the screen: the artwork blurred inside it, under its dark tint
            _glass.Margin = new Thickness(0, BandCenter - BandHeight / 2, 0, 0);
            _glass.Reveal = _bandScale;
            if (HasCover || !string.IsNullOrEmpty(Sources.Background))
            {
                Stage.Children.Add(_glass);
            }

            _band = new Border
            {
                Height = BandHeight,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, BandCenter - BandHeight / 2, 0, 0),
                Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                BorderBrush = White(0.14),
                RenderTransform = _bandScale
            };
            Stage.Children.Add(_band);

            // 6. Cover cutting across the strip
            if (HasCover)
            {
                _coverFrame = CoverFrame(_coverWidth, CoverHeight, 6, false, White(0.05), Shadow(30, 70, 0.6), out _cover);
                _coverFrame.HorizontalAlignment = HorizontalAlignment.Left;
                _coverFrame.VerticalAlignment = VerticalAlignment.Top;
                _coverFrame.Margin = new Thickness(CoverLeft, BandCenter - CoverHeight / 2, 0, 0);
                _coverFrame.Opacity = 0;
                _coverFrame.RenderTransform = _frameShift;
                _cover.RenderTransform = _coverShift;
                SafeArea().Children.Add(_coverFrame);
            }

            // 7. Text on the strip
            var titleSlot = new Grid();
            _name = Text(Info.Name, 64, 300, Brushes.White, 1.1, 2);
            _name.HorizontalAlignment = align;
            _name.TextAlignment = TextAlignment.Center;
            _name.Opacity = 0;
            _name.RenderTransform = new TranslateTransform(0, 10);
            titleSlot.Children.Add(_name);
            if (UseLogo)
            {
                _logo = new Image { Stretch = Stretch.Uniform, HorizontalAlignment = align, Opacity = 0, RenderTransform = _logoShift };
                RenderOptions.SetBitmapScalingMode(_logo, BitmapScalingMode.HighQuality);
                titleSlot.Children.Add(_logo);
            }

            _text.Children.Add(titleSlot);
            AddLine(SplashText.Join(Separator, Info.ReleaseYear?.ToString(), Info.Developer, Info.Platform), 21, White(0.65), 22, align);
            AddLine(SplashText.Join(Separator, SplashText.Playtime(Info.Playtime), Info.Library), 17, White(0.42), 8, align);
            _text.HorizontalAlignment = HorizontalAlignment.Center;

            _textArea = new Grid
            {
                Height = BandHeight,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, BandCenter - BandHeight / 2, 0, 0),
                Children = { _text }
            };
            if (HasCover)
            {
                _text.Margin = new Thickness(CoverLeft + _coverWidth + TextGap, 0, TextRight, 0);
                SafeArea().Children.Add(_textArea);
            }
            else
            {
                Stage.Children.Add(_textArea);
            }
        }

        private double TextWidth => HasCover ? SafeWidth - (CoverLeft + _coverWidth + TextGap) - TextRight : 0.76 * StageWidth;

        protected override void OnStageWidth(double width)
        {
            _band.BorderThickness = new Thickness(0, OnePixel, 0, OnePixel);
            _glass.SetSize(width, BandHeight);
            _glass.AlignCopy(new Point(0, BandCenter - BandHeight / 2), width, StageHeight);
            if (!HasCover)
            {
                _textArea.Margin = new Thickness(0.12 * width, BandCenter - BandHeight / 2, 0.12 * width, 0);
            }

            double textWidth = Math.Max(1, TextWidth);
            FitText(_name, textWidth, 64, 2, 46, 2, 1.1);
        }

        protected override void Start()
        {
            Animate(_bandScale, ScaleTransform.ScaleYProperty, 0, 1, 1.0, 0.1, ExpoOutBy(7));
            if (_coverFrame != null)
            {
                Animate(_coverFrame, UIElement.OpacityProperty, 0, 1, 0.6, 0.3);
            }

            WaitForLogo();
        }

        protected override void ShowLogo(BitmapSource logo)
        {
            _name.Visibility = Visibility.Collapsed;
            _logo.Source = logo;
            FitLogo(_logo, logo, Math.Min(LogoWidthShare * StageWidth, Math.Max(1, TextWidth)), LogoMaxHeight);
            Animate(_logo, UIElement.OpacityProperty, 0, 1, 1.0, 0.3, QuintOut);
            Animate(_logoShift, TranslateTransform.XProperty, 20, 0, 1.0, 0.3, QuintOut);
            RevealText(includeName: false);
        }

        protected override void ShowName()
        {
            if (_logo != null)
            {
                _logo.Visibility = Visibility.Collapsed;
            }

            RevealText(includeName: true);
        }

        /// <summary>Name, details and play time one after another (the logo has its own entrance).</summary>
        private void RevealText(bool includeName)
        {
            var items = new List<FrameworkElement>();
            if (includeName)
            {
                items.Add(_name);
            }

            items.AddRange(_lines);
            for (int k = 0; k < items.Count; k++)
            {
                double delay = FromShow(0.6 + k * 0.12);
                Animate(items[k], UIElement.OpacityProperty, 0, 1, 0.9, delay, CubicOut);
                Animate(items[k].RenderTransform, TranslateTransform.YProperty, 10, 0, 0.9, delay, CubicOut);
            }
        }

        protected override void Close()
        {
            Animate(_text, UIElement.OpacityProperty, null, 0, 0.3);
            if (_coverFrame != null)
            {
                Animate(_coverFrame, UIElement.OpacityProperty, null, 0, 0.5, 0, CubicIn);
                Animate(_frameShift, TranslateTransform.YProperty, null, -30, 0.5, 0, CubicIn);
            }

            Animate(_bandScale, ScaleTransform.ScaleYProperty, null, 0, 0.65, 0.15, CubicIn);
        }

        public override void LoadImagesAsync(SplashImageSources sources, MonitorArea screen)
        {
            ScreenPixels(screen, out int width, out int height);
            bool useLogo = UseLogo;
            bool hasCover = HasCover;
            double coverWidth = _coverWidth;
            int logoWidth = (int)(width * LogoWidthShare);

            LoadInBackground(() => new Loaded
            {
                Artwork = LoadArtwork(width, height),
                Cover = hasCover ? ImageLoader.Load(Sources.Cover, UnitsToPixels(coverWidth, height), UnitsToPixels(CoverHeight, height), cover: true) : null,
                Logo = useLogo ? ImageLoader.Load(Sources.Logo, logoWidth, UnitsToPixels(LogoMaxHeight, height), cover: false) : null
            }, loaded =>
            {
                loaded = loaded ?? new Loaded();
                ShowSharpBackdrop(loaded.Artwork);
                _glass.SetImage(GlassCopy(loaded.Artwork, 24, 44), loaded.Artwork?.FromCover == true ? 1.15 : 1.06);
                if (_cover != null && loaded.Cover != null)
                {
                    _cover.Source = loaded.Cover;
                    Animate(_cover, UIElement.OpacityProperty, 0, 1, 1.0, 0, QuintOut);
                    Animate(_coverShift, TranslateTransform.YProperty, 30, 0, 1.0, 0, QuintOut);
                }

                if (useLogo)
                {
                    LogoLoaded(loaded.Logo);
                }
            });
        }

        private void AddLine(string text, double size, Brush brush, double gap, HorizontalAlignment align)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            TextBlock line = Text(text, size, 350, brush);
            line.HorizontalAlignment = align;
            line.Margin = new Thickness(0, gap, 0, 0);
            line.Opacity = 0;
            line.RenderTransform = new TranslateTransform(0, 10);
            _text.Children.Add(line);
            _lines.Add(line);
        }

        private sealed class Loaded
        {
            public Artwork Artwork { get; set; }
            public BitmapSource Cover { get; set; }
            public BitmapSource Logo { get; set; }
        }
    }
}
