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
    /// Drawer: the sharp cover sits in a glass mount in the middle of the sharp artwork; a glass panel slides out
    /// from behind it to the right, with the logo or name and the game's details, one value per row.
    /// </summary>
    internal sealed class DrawerScene : StyledScene
    {
        private const double CoverHeight = 660;
        private const double Mount = 18;
        private const double PanelWidth = 540;
        private const double PanelHeight = 580;
        private const double Tuck = 24;
        private const double Inset = 48;
        private const double GlassDim = 0.15;
        private const double LogoWidthShare = 0.15;
        private const double LogoMaxHeight = 150;

        private readonly double _coverWidth;
        private readonly double _innerWidth;

        private readonly Grid _coverHolder;
        private readonly GlassPane _mount;
        private readonly Image _cover;
        private readonly ScaleTransform _coverScale = new ScaleTransform(0.96, 0.96);

        private readonly Grid _panel;
        private readonly GlassPane _panelGlass;
        private readonly TranslateTransform _panelShift = new TranslateTransform();
        private readonly List<Border> _hairlines = new List<Border>();

        private readonly TextBlock _name;
        private readonly Image _logo;
        private readonly Rectangle _line;
        private readonly List<FrameworkElement> _rows = new List<FrameworkElement>();

        public DrawerScene(SplashGameInfo info, SplashImageSources sources)
            : base(info, sources)
        {
            _coverWidth = CoverHeight * CoverAspect;
            _innerWidth = HasCover ? PanelWidth - Inset - (Inset + Tuck) : PanelWidth - 2 * Inset;
            bool hasArtwork = HasCover || !string.IsNullOrEmpty(Sources.Background);

            // 1–2. Base and the sharp artwork (no dimming)
            Stage.Children.Add(new Rectangle { Fill = Gray(0x08) });
            AddSharpBackdrop();

            // 3. Panel content, centered: logo or name, a line, one value per row
            var content = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var titleSlot = new Grid { HorizontalAlignment = HorizontalAlignment.Center };
            _name = Text(Info.Name, 44, 600, Brushes.White, 1.12, 3);
            FitText(_name, _innerWidth, 44, 3, 32, 3, 1.12);
            _name.TextAlignment = TextAlignment.Center;
            _name.HorizontalAlignment = HorizontalAlignment.Center;
            _name.Opacity = 0;
            _name.RenderTransform = new TranslateTransform(0, 10);
            titleSlot.Children.Add(_name);
            if (UseLogo)
            {
                _logo = new Image { Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0 };
                RenderOptions.SetBitmapScalingMode(_logo, BitmapScalingMode.HighQuality);
                titleSlot.Children.Add(_logo);
            }

            content.Children.Add(titleSlot);
            foreach (string value in new[] { Info.Developer, Info.ReleaseYear?.ToString(), Info.Genre, Info.Platform, SplashText.Playtime(Info.Playtime), Info.Library })
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _rows.Add(Row(value));
                }
            }

            if (_rows.Count > 0)
            {
                _line = new Rectangle { Fill = White(0.2), Margin = new Thickness(0, 28, 0, 8), Opacity = 0, RenderTransform = new TranslateTransform(0, 10) };
                content.Children.Add(_line);
                foreach (FrameworkElement row in _rows)
                {
                    content.Children.Add(row);
                }
            }

            // The panel: frosted glass, or (with nothing to blur) a dark translucent fill.
            _panel = new Grid
            {
                Width = PanelWidth,
                Height = PanelHeight,
                Opacity = 0,
                RenderTransform = _panelShift
            };
            if (hasArtwork)
            {
                _panelGlass = new GlassPane(16, GlassDim).WithShadow(20, 50, 0.25);
                _panelGlass.SetSize(PanelWidth, PanelHeight);
                _panel.Children.Add(_panelGlass);
            }
            else
            {
                _panel.Children.Add(new Border { CornerRadius = new CornerRadius(16), Background = new SolidColorBrush(Color.FromArgb(153, 0x14, 0x14, 0x14)) });
            }

            _panel.Children.Add(new Border
            {
                Padding = HasCover ? new Thickness(Inset + Tuck, Inset, Inset, Inset) : new Thickness(Inset),
                Child = content
            });

            Grid area = SafeArea();
            if (HasCover)
            {
                // Cover and panel as one centered group; the panel slides inside a clip that starts at the middle
                // of the cover, so it never shows on the cover's left side.
                _panel.HorizontalAlignment = HorizontalAlignment.Left;
                _panel.VerticalAlignment = VerticalAlignment.Center;
                _panel.Margin = new Thickness(_coverWidth / 2 - Tuck, 0, 0, 0);
                var drawer = new Grid
                {
                    Margin = new Thickness(Mount + _coverWidth / 2, 0, 0, 0),
                    Clip = new RectangleGeometry(new Rect(0, -StageHeight, 2 * SafeAreaWidth, 3 * StageHeight)),
                    Children = { _panel }
                };

                // The cover in its glass mount (18 u larger on every side); they come and go together.
                _mount = new GlassPane(22, GlassDim).WithShadow(20, 50, 0.25);
                _mount.SetSize(_coverWidth + 2 * Mount, CoverHeight + 2 * Mount);
                Grid frame = CoverFrame(_coverWidth, CoverHeight, 10, false, White(0.05), Shadow(10, 30, 0.35), out _cover);
                frame.Margin = new Thickness(Mount);
                _coverHolder = new Grid
                {
                    Width = _coverWidth + 2 * Mount,
                    Height = CoverHeight + 2 * Mount,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Opacity = 0,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = _coverScale,
                    Children = { _mount, frame }
                };

                area.Children.Add(new Grid
                {
                    Width = GroupWidth + 2 * Mount,
                    Height = CoverHeight + 2 * Mount,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children = { drawer, _coverHolder }
                });
            }
            else
            {
                _panel.HorizontalAlignment = HorizontalAlignment.Center;
                _panel.VerticalAlignment = VerticalAlignment.Center;
                _panel.RenderTransform = new TranslateTransform(0, 20);
                area.Children.Add(_panel);
            }
        }

        private double GroupWidth => _coverWidth + PanelWidth - Tuck;

        protected override void OnStageWidth(double width)
        {
            foreach (Border hairline in _hairlines)
            {
                hairline.BorderThickness = new Thickness(0, 0, 0, OnePixel);
            }

            if (_line != null)
            {
                _line.Height = OnePixel;
            }

            // The glass copies lie under the artwork when the panel and the cover are in place.
            _panelGlass?.WithEdge(OnePixel);
            _mount?.WithEdge(OnePixel);
            if (HasCover)
            {
                double groupLeft = SafeLeft + (SafeWidth - GroupWidth) / 2;
                double groupTop = (StageHeight - CoverHeight) / 2;
                _mount.AlignCopy(new Point(groupLeft - Mount, groupTop - Mount), width, StageHeight);
                _panelGlass?.AlignCopy(new Point(groupLeft + _coverWidth - Tuck, groupTop + (CoverHeight - PanelHeight) / 2), width, StageHeight);
            }
            else
            {
                _panelGlass?.AlignCopy(new Point((width - PanelWidth) / 2, (StageHeight - PanelHeight) / 2), width, StageHeight);
            }
        }

        protected override void Start()
        {
            if (_coverHolder != null)
            {
                Animate(_coverHolder, UIElement.OpacityProperty, 0, 1, 1.0, 0.1, QuintOut);
                Animate(_coverScale, ScaleTransform.ScaleXProperty, 0.96, 1, 1.0, 0.1, QuintOut);
                Animate(_coverScale, ScaleTransform.ScaleYProperty, 0.96, 1, 1.0, 0.1, QuintOut);

                double away = -(PanelWidth - Tuck);
                _panelShift.X = away;
                Animate(_panelShift, TranslateTransform.XProperty, away, 0, 1.2, 0.7, QuintOut);
                Animate(_panel, UIElement.OpacityProperty, 0, 1, 0.24, 0.7);
            }
            else
            {
                Animate(_panel, UIElement.OpacityProperty, 0, 1, 1.0, 0.2, CubicOut);
                Animate(_panel.RenderTransform, TranslateTransform.YProperty, 20, 0, 1.0, 0.2, CubicOut);
            }

            WaitForLogo();
        }

        protected override void ShowLogo(BitmapSource logo)
        {
            _name.Visibility = Visibility.Collapsed;
            _logo.Source = logo;
            FitLogo(_logo, logo, Math.Min(LogoWidthShare * StageWidth, _innerWidth), LogoMaxHeight);
            FadeIn(_logo, 0.8, FromShow(_coverHolder != null ? 0.9 : 0.3), SineOut);
            RevealContent(includeName: false);
        }

        protected override void ShowName()
        {
            if (_logo != null)
            {
                _logo.Visibility = Visibility.Collapsed;
            }

            RevealContent(includeName: true);
        }

        /// <summary>Name, line and rows one after another (the logo has its own fade).</summary>
        private void RevealContent(bool includeName)
        {
            var items = new List<FrameworkElement>();
            if (includeName)
            {
                items.Add(_name);
            }

            if (_line != null)
            {
                items.Add(_line);
            }

            items.AddRange(_rows);
            double start = _coverHolder != null ? 1.25 : 0.5;
            for (int k = 0; k < items.Count; k++)
            {
                double delay = FromShow(start + k * 0.09);
                Animate(items[k], UIElement.OpacityProperty, 0, 1, 0.8, delay, CubicOut);
                Animate(items[k].RenderTransform, TranslateTransform.YProperty, 10, 0, 0.8, delay, CubicOut);
            }
        }

        protected override void Close()
        {
            if (_coverHolder != null)
            {
                Animate(_panelShift, TranslateTransform.XProperty, null, -(PanelWidth - Tuck), 0.5, 0, CubicIn);
                Animate(_panel, UIElement.OpacityProperty, null, 0, 0.1, 0.4);
                Animate(_coverHolder, UIElement.OpacityProperty, null, 0, 0.5, 0.4, CubicIn);
                Animate(_coverScale, ScaleTransform.ScaleXProperty, null, 0.96, 0.5, 0.4, CubicIn);
                Animate(_coverScale, ScaleTransform.ScaleYProperty, null, 0.96, 0.5, 0.4, CubicIn);
            }
            else
            {
                Animate(_panel, UIElement.OpacityProperty, null, 0, 0.5);
            }
        }

        public override void LoadImagesAsync(SplashImageSources sources, MonitorArea screen)
        {
            ScreenPixels(screen, out int width, out int height);
            bool useLogo = UseLogo;
            bool hasCover = HasCover;
            double coverWidth = _coverWidth;
            int logoWidth = Math.Min((int)(width * LogoWidthShare), UnitsToPixels(_innerWidth, height));

            LoadInBackground(() => new Loaded
            {
                Artwork = LoadArtwork(width, height),
                Cover = hasCover ? ImageLoader.Load(Sources.Cover, UnitsToPixels(coverWidth, height), UnitsToPixels(CoverHeight, height), cover: true) : null,
                Logo = useLogo ? ImageLoader.Load(Sources.Logo, logoWidth, UnitsToPixels(LogoMaxHeight, height), cover: false) : null
            }, loaded =>
            {
                loaded = loaded ?? new Loaded();
                ShowSharpBackdrop(loaded.Artwork);
                BitmapSource copy = GlassCopy(loaded.Artwork, 24, 44);
                double scale = loaded.Artwork?.FromCover == true ? 1.15 : 1.06;
                _panelGlass?.SetImage(copy, scale);
                _mount?.SetImage(copy, scale);
                if (_cover != null && loaded.Cover != null)
                {
                    _cover.Source = loaded.Cover;
                    FadeIn(_cover, 0.6, 0, SineOut);
                }

                if (useLogo)
                {
                    LogoLoaded(loaded.Logo);
                }
            });
        }

        /// <summary>One value, centered, with a faint line under it.</summary>
        private FrameworkElement Row(string value)
        {
            TextBlock text = Text(value, 21, 400, White(0.92));
            text.HorizontalAlignment = HorizontalAlignment.Center;
            var row = new Border
            {
                Padding = new Thickness(0, 10, 0, 10),
                BorderBrush = White(0.10),
                Child = text,
                Opacity = 0,
                RenderTransform = new TranslateTransform(0, 10)
            };
            _hairlines.Add(row);
            return row;
        }

        private sealed class Loaded
        {
            public Artwork Artwork { get; set; }
            public BitmapSource Cover { get; set; }
            public BitmapSource Logo { get; set; }
        }
    }
}
