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
    /// Slate Gloss: the art pulls into focus; a tall slate of dark glossy glass with well-rounded corners slides in
    /// from the left, holding the cover, the logo or name, a rule and a short list of values one under another. The
    /// reflections on the glass and on the cover trail behind the slate's movement, like on a real pane.
    /// </summary>
    internal sealed class SlateGlossScene : LensScene
    {
        private const double SlateLeft = 120;
        private const double SlateTop = 110;
        private const double SlateWidth = 520;
        private const double SlateHeight = 860;
        private const double SlateRadius = 36;
        private const double Padding = 48;
        private const double ContentWidth = SlateWidth - 2 * Padding;
        private const double CoverWidth = 300;
        private const double CoverHeight = 450;
        private const double CoverRadius = 14;

        private readonly ScreenGlass _slate = new ScreenGlass(new SolidColorBrush(Color.FromArgb(89, 0, 0, 0)), 30, 70, 0.25);
        private readonly TranslateTransform _slateShift = new TranslateTransform(-140, 0);
        private readonly Reflection _cap;
        private readonly Reflection _streaks;
        private readonly TranslateTransform _sheenShift;
        private readonly Reflection _coverCap;
        private readonly Reflection _coverStreak;

        private readonly Grid _coverFrame;
        private readonly Image _cover;
        private readonly TextBlock _name;
        private readonly Image _logo;
        private readonly Rectangle _rule;
        private readonly List<FrameworkElement> _rows = new List<FrameworkElement>();

        public SlateGlossScene(SplashGameInfo info, SplashImageSources sources)
            : base(info, sources)
        {
            bool cover = HasCover;

            // 1–2. Base and the art pulling into focus
            AddLens();

            // 3. The slate of smoked glossy glass
            _slate.Motion.Children.Add(_slateShift);
            _slate.Opacity = 0;
            var slateShape = new RectangleGeometry(new Rect(0, 0, SlateWidth, SlateHeight), SlateRadius, SlateRadius);
            slateShape.Freeze();
            _cap = Gloss.Cap(SlateWidth, SlateHeight, -0.10, -0.38, 1.20, 0.85, Gloss.White(0.20, 0), Gloss.White(0.06, 0.6), Gloss.White(0, 1));
            _streaks = Gloss.Streaks(SlateWidth, SlateHeight, Gloss.White(0, 0.30), Gloss.White(0.14, 0.335), Gloss.White(0, 0.37),
                Gloss.White(0, 0.41), Gloss.White(0.08, 0.425), Gloss.White(0, 0.44));
            Rectangle sheen = Gloss.Sheen(SlateWidth, SlateHeight, 0.30, out _sheenShift);
            var layer = new Canvas();
            layer.Children.Add(_cap.Element);
            layer.Children.Add(_streaks.Element);
            layer.Children.Add(Gloss.LitEdges(slateShape, 0.38, 0.12, 0.10));
            layer.Children.Add(sheen);

            // 4. Content: one column, from the top with a cover, centered without one
            var column = new StackPanel
            {
                Width = ContentWidth,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = cover ? VerticalAlignment.Top : VerticalAlignment.Center
            };
            if (cover)
            {
                var coverShape = new RectangleGeometry(new Rect(0, 0, CoverWidth, CoverHeight), CoverRadius, CoverRadius);
                coverShape.Freeze();
                _cover = CoverImage();
                _cover.Opacity = 0;
                RenderOptions.SetBitmapScalingMode(_cover, BitmapScalingMode.HighQuality);
                _coverCap = Gloss.Cap(CoverWidth, CoverHeight, -0.20, -0.45, 1.40, 0.80, Gloss.White(0.14, 0), Gloss.White(0, 1));
                _coverStreak = Gloss.Streaks(CoverWidth, CoverHeight, Gloss.White(0, 0.36), Gloss.White(0.16, 0.39), Gloss.White(0, 0.42));
                _coverFrame = new Grid
                {
                    Width = CoverWidth,
                    Height = CoverHeight,
                    Margin = new Thickness(0, 0, 0, 28),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children =
                    {
                        SoftShadow(CoverWidth, CoverHeight, CoverRadius, 16, 40, 0.35),
                        new Grid
                        {
                            Clip = coverShape,
                            Children =
                            {
                                new Rectangle { Fill = new SolidColorBrush(Color.FromArgb(77, 0, 0, 0)) },
                                _cover,
                                new Canvas { Children = { _coverCap.Element, _coverStreak.Element } },
                                Gloss.LitEdges(coverShape, 0.35, 0.10, 0)
                            }
                        }
                    }
                };
                Rising(_coverFrame);
                column.Children.Add(_coverFrame);
            }

            var title = new Grid { HorizontalAlignment = HorizontalAlignment.Center };
            if (!string.IsNullOrWhiteSpace(Info.Name))
            {
                double size = cover ? 34 : 48;
                _name = Text(Info.Name, size, 600, Brushes.White, 1.15, 2);
                FitText(_name, ContentWidth, size, 2, cover ? 26 : 36, 2, 1.15);
                _name.TextAlignment = TextAlignment.Center;
                _name.HorizontalAlignment = HorizontalAlignment.Center;
                _name.Effect = Shadow(2, 16, 0.45);
                Rising(_name);
                title.Children.Add(_name);
            }

            if (UseLogo)
            {
                _logo = new Image { Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Center, Effect = Shadow(3, 14, 0.45) };
                RenderOptions.SetBitmapScalingMode(_logo, BitmapScalingMode.HighQuality);
                Rising(_logo);
                title.Children.Add(_logo);
            }

            column.Children.Add(title);
            _rule = new Rectangle { Height = 1, Fill = White(0.30), Margin = new Thickness(0, 24, 0, 14) };
            Rising(_rule);
            column.Children.Add(_rule);

            foreach (string value in new[] { Info.Developer, Info.ReleaseYear?.ToString(), Info.Genre, SplashText.Hours(Info.Playtime) })
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                TextBlock row = Text(value, 19, 350, White(0.85), 1.6);
                row.HorizontalAlignment = HorizontalAlignment.Center;
                row.MaxWidth = ContentWidth;
                row.Effect = Shadow(2, 16, 0.45);
                Rising(row);
                column.Children.Add(row);
                _rows.Add(row);
            }

            layer.Children.Add(new Grid
            {
                Width = SlateWidth,
                Height = SlateHeight,
                Children = { new Grid { Margin = new Thickness(Padding), Children = { column } } }
            });
            _slate.Decor.Children.Add(layer);
            Stage.Children.Add(_slate);
        }

        /// <summary>Hidden, ready to rise 14 u into place.</summary>
        private static void Rising(FrameworkElement element)
        {
            element.Opacity = 0;
            element.RenderTransform = new TranslateTransform(0, 14);
        }

        private static void Rise(FrameworkElement element, double delay)
        {
            Animate(element, UIElement.OpacityProperty, 0, 1, 1.2, delay, CubicOut);
            Animate(element.RenderTransform, TranslateTransform.YProperty, 14, 0, 1.2, delay, CubicOut);
        }

        protected override void OnStageWidth(double width)
        {
            _slate.SetShape(new RectangleGeometry(new Rect(0, 0, SlateWidth, SlateHeight), SlateRadius, SlateRadius),
                new Point(SafeLeft + SlateLeft, SlateTop));
            _slate.SetStageSize(width, StageHeight);
        }

        protected override void Start()
        {
            StartLens();
            Animate(_slate, UIElement.OpacityProperty, 0, 1, 1.7, 0.9, ExpoOut);
            Animate(_slateShift, TranslateTransform.XProperty, -140, 0, 1.7, 0.9, ExpoOut);

            // Reflections start pushed to the right and are left behind as the slate moves right.
            _cap.EnterFrom(-0.21, -0.12, 1.7, 0.9);
            _streaks.EnterFrom(0.35, 0, 2.0, 0.9);
            _streaks.Drift(-0.06, 0, 8, 2.6);
            _cap.Drift(0.04, 0.03, 9, 2.6);
            if (_coverCap != null)
            {
                _coverCap.EnterFrom(-0.225, -0.10, 1.9, 0.9);
                _coverStreak.EnterFrom(0.45, 0, 2.2, 0.9);
                _coverStreak.Drift(-0.05, 0, 7, 2.8);
            }

            Gloss.Sweep(_sheenShift, SlateWidth, 6, 0.84, 3.0);
            WaitForLogo();
        }

        protected override void ShowLogo(BitmapSource logo)
        {
            if (_name != null)
            {
                _name.Visibility = Visibility.Collapsed;
            }

            _logo.Source = logo;
            if (HasCover)
            {
                FitLogo(_logo, logo, 400, 110);
            }
            else
            {
                FitLogo(_logo, logo, 420, 200);
            }

            Rise(_logo, FromShow(1.4));
            RevealContent(includeName: false);
        }

        protected override void ShowName()
        {
            if (_logo != null)
            {
                _logo.Visibility = Visibility.Collapsed;
            }

            RevealContent(includeName: _name != null);
        }

        /// <summary>Cover, name, rule and each value rise in turn, 100 ms apart (the logo has its own start).</summary>
        private void RevealContent(bool includeName)
        {
            var items = new List<FrameworkElement>();
            if (_coverFrame != null)
            {
                items.Add(_coverFrame);
            }

            if (includeName)
            {
                items.Add(_name);
            }

            items.Add(_rule);
            items.AddRange(_rows);
            for (int i = 0; i < items.Count; i++)
            {
                Rise(items[i], FromShow(1.25 + i * 0.1));
            }
        }

        protected override void Close()
        {
            CloseLens();
            Animate(_slateShift, TranslateTransform.XProperty, null, -90, 0.6, 0, CubicIn);
            Animate(_slate, UIElement.OpacityProperty, null, 0, 0.6, 0, CubicIn);
            _streaks.Leave(-0.35, 0);
            _cap.Leave(0.14, -0.10);
            _coverStreak?.Leave(-0.45, 0);
            _coverCap?.Leave(0.18, -0.10);
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
                Logo = useLogo ? ImageLoader.Load(Sources.Logo, UnitsToPixels(420, height), UnitsToPixels(200, height), cover: false) : null
            }, loaded =>
            {
                loaded = loaded ?? new Loaded();
                LensArrived(loaded.Artwork);
                _slate.SetCopy(FrostCopy(loaded.Artwork), FrostScale(loaded.Artwork));
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
