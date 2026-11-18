using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static SplashAddonPlus.StyleKit;

namespace SplashAddonPlus
{
    /// <summary>
    /// Focus: the splash opens like a lens finding focus. The game's art appears heavily blurred and zoomed in, then
    /// sharpens and pulls back to the full frame; a frosted glass pill slides in at the bottom with a cover thumbnail,
    /// the logo or name and a line of details.
    /// </summary>
    internal sealed class FocusScene : LensScene
    {
        private const double PillWidth = 1040;
        private const double PillHeight = 156;
        private const double PillTop = StageHeight - 72 - PillHeight;
        private const double ThumbInset = (PillHeight - ThumbHeight) / 2;
        private const double ThumbWidth = 85;
        private const double ThumbHeight = 128;
        private const double ThumbRadius = ThumbWidth / 2;

        /// <summary>
        /// The pill's corners follow the thumbnail's: its radius plus the 14 u gap, so both curves share their centers
        /// and the gap stays even all the way round the corner.
        /// </summary>
        private const double PillRadius = ThumbRadius + ThumbInset;

        private readonly Grid _pill;
        private readonly ScaleTransform _pillScale = new ScaleTransform(0.96, 0.96);
        private readonly TranslateTransform _pillShift = new TranslateTransform(0, 60);
        // The frost is darkened (30 % black) so white text keeps its contrast over bright art; a faint milky
        // white on top keeps it reading as glass.
        private readonly GlassPane _glass = new GlassPane(PillRadius, 0.30, White(0.05)).WithShadow(24, 60, 0.25);
        private readonly TranslateTransform _sheenShift = new TranslateTransform(-1.3 * PillWidth, 0);

        private readonly Image _thumb;
        private readonly Grid _title;
        private readonly TextBlock _name;
        private readonly Image _logo;
        private readonly TextBlock _details;

        public FocusScene(SplashGameInfo info, SplashImageSources sources)
            : base(info, sources)
        {
            bool cover = HasCover;

            // 1–2. Base and the art pulling into focus
            AddLens();

            // 4. The pill: frosted glass (copy of the art under it + milky white), gloss, edge, sheen and content
            _glass.SetSize(PillWidth, PillHeight);
            var shape = new RectangleGeometry(new Rect(0, 0, PillWidth, PillHeight), PillRadius, PillRadius);
            var overlay = new Grid
            {
                Clip = shape,
                Children =
                {
                    new Rectangle
                    {
                        Fill = AngledGradient(135,
                            new GradientStop(Color.FromArgb(31, 255, 255, 255), 0),
                            new GradientStop(Color.FromArgb(8, 255, 255, 255), 0.35),
                            new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.55))
                    },
                    new Border { CornerRadius = new CornerRadius(PillRadius), BorderThickness = new Thickness(1), BorderBrush = White(0.20) },
                    new Border { CornerRadius = new CornerRadius(PillRadius), BorderThickness = new Thickness(0, 1.5, 0, 0), BorderBrush = White(0.28) },
                    new Rectangle
                    {
                        Fill = AngledGradient(105, Stop(0, 255, 0.38), Stop(56, 255, 0.5), Stop(0, 255, 0.62)),
                        RenderTransform = _sheenShift
                    }
                }
            };

            // Content: one row, centered vertically
            var row = new Grid();
            double textWidth;
            HorizontalAlignment align = cover ? HorizontalAlignment.Left : HorizontalAlignment.Center;
            var column = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = align };
            if (cover)
            {
                _thumb = CoverImage();
                _thumb.Opacity = 0;
                RenderOptions.SetBitmapScalingMode(_thumb, BitmapScalingMode.HighQuality);
                var thumbFrame = new Grid
                {
                    Width = ThumbWidth,
                    Height = ThumbHeight,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(ThumbInset, 0, 0, 0),
                    Children =
                    {
                        SoftShadow(ThumbWidth, ThumbHeight, ThumbRadius, 8, 20, 0.3),
                        new Grid
                        {
                            Clip = new RectangleGeometry(new Rect(0, 0, ThumbWidth, ThumbHeight), ThumbRadius, ThumbRadius),
                            Children = { new Rectangle { Fill = White(0.10) }, _thumb }
                        }
                    }
                };
                row.Children.Add(thumbFrame);
                double left = ThumbInset + ThumbWidth + 28;
                textWidth = PillWidth - left - 70;
                column.Margin = new Thickness(left, 0, 70, 0);
            }
            else
            {
                textWidth = PillWidth - 140;
                column.Margin = new Thickness(70, 0, 70, 0);
            }

            column.MaxWidth = textWidth;
            var title = new Grid { HorizontalAlignment = align };
            _title = title;
            if (!string.IsNullOrWhiteSpace(Info.Name))
            {
                _name = Text(Info.Name, 34, 600, Brushes.White, 1.2);
                FitText(_name, textWidth, 34, 1, 28, 1, 1.2);
                _name.TextWrapping = TextWrapping.NoWrap;
                _name.HorizontalAlignment = align;
                _name.Effect = Shadow(2, 16, 0.45);
                _name.Opacity = 0;
                _name.RenderTransform = new TranslateTransform(0, 12);
                title.Children.Add(_name);
            }

            if (UseLogo)
            {
                _logo = new Image
                {
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = align,
                    Opacity = 0,
                    Effect = Shadow(2, 10, 0.4),
                    RenderTransform = new TranslateTransform(0, 12)
                };
                RenderOptions.SetBitmapScalingMode(_logo, BitmapScalingMode.HighQuality);
                title.Children.Add(_logo);
            }

            column.Children.Add(title);

            string details = SplashText.Join(Separator, Info.Genre, Info.ReleaseYear?.ToString(), Info.Developer);
            if (details != null)
            {
                _details = Text(details, 18, 350, White(0.82), 1.3);
                _details.HorizontalAlignment = align;
                _details.MaxWidth = textWidth;
                _details.Margin = new Thickness(0, 6, 0, 0);
                _details.Effect = Shadow(2, 16, 0.45);
                _details.Opacity = 0;
                _details.RenderTransform = new TranslateTransform(0, 12);
                column.Children.Add(_details);
            }

            row.Children.Add(column);
            overlay.Children.Add(row);

            var transforms = new TransformGroup();
            transforms.Children.Add(_pillScale);
            transforms.Children.Add(_pillShift);
            _pill = new Grid
            {
                Width = PillWidth,
                Height = PillHeight,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, PillTop, 0, 0),
                Opacity = 0,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = transforms,
                Children = { _glass, overlay }
            };
            Stage.Children.Add(_pill);
        }

        protected override void OnStageWidth(double width)
        {
            _glass.AlignCopy(new Point((width - PillWidth) / 2, PillTop), width, StageHeight);
        }

        protected override void Start()
        {
            StartLens();
            Animate(_pill, UIElement.OpacityProperty, 0, 1, 1.4, 1.0, ExpoOut);
            Animate(_pillShift, TranslateTransform.YProperty, 60, 0, 1.4, 1.0, ExpoOut);
            Animate(_pillScale, ScaleTransform.ScaleXProperty, 0.96, 1, 1.4, 1.0, ExpoOut);
            Animate(_pillScale, ScaleTransform.ScaleYProperty, 0.96, 1, 1.4, 1.0, ExpoOut);

            // Every 9 s from 3.2 s the sheen crosses the pill in 1.26 s.
            var sweep = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromSeconds(9),
                BeginTime = TimeSpan.FromSeconds(3.2),
                RepeatBehavior = RepeatBehavior.Forever
            };
            sweep.KeyFrames.Add(new DiscreteDoubleKeyFrame(-1.3 * PillWidth, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            sweep.KeyFrames.Add(new EasingDoubleKeyFrame(1.3 * PillWidth, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.26)), SineInOut));
            _sheenShift.BeginAnimation(TranslateTransform.XProperty, sweep);

            if (_details != null)
            {
                Rise(_details, 1.40);
            }

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
                FitLogo(_logo, logo, 360, 70);

                // The logo and the details line share one center line (the column still starts beside the
                // thumbnail), so a short or long logo never looks pushed to the left of the text under it.
                _title.HorizontalAlignment = HorizontalAlignment.Center;
                _logo.HorizontalAlignment = HorizontalAlignment.Center;
                if (_details != null)
                {
                    _details.HorizontalAlignment = HorizontalAlignment.Center;
                }
            }
            else
            {
                FitLogo(_logo, logo, 480, 90);
            }

            Rise(_logo, FromShow(1.25));
        }

        protected override void ShowName()
        {
            if (_logo != null)
            {
                _logo.Visibility = Visibility.Collapsed;
            }

            if (_name != null)
            {
                Rise(_name, FromShow(1.25));
            }
        }

        private static void Rise(FrameworkElement element, double delay)
        {
            Animate(element, UIElement.OpacityProperty, 0, 1, 1.2, delay, CubicOut);
            Animate(element.RenderTransform, TranslateTransform.YProperty, 12, 0, 1.2, delay, CubicOut);
        }

        protected override void Close()
        {
            CloseLens();
            Animate(_pillShift, TranslateTransform.YProperty, null, 40, 0.5, 0, CubicIn);
            Animate(_pill, UIElement.OpacityProperty, null, 0, 0.5, 0, CubicIn);
        }

        public override void LoadImagesAsync(SplashImageSources sources, MonitorArea screen)
        {
            ScreenPixels(screen, out int width, out int height);
            bool useLogo = UseLogo;
            bool hasCover = HasCover;

            LoadInBackground(() => new Loaded
            {
                Artwork = LoadArtwork(width, height),
                Cover = hasCover ? ImageLoader.Load(Sources.Cover, UnitsToPixels(ThumbWidth, height), UnitsToPixels(ThumbHeight, height), cover: true) : null,
                Logo = useLogo ? ImageLoader.Load(Sources.Logo, UnitsToPixels(480, height), UnitsToPixels(90, height), cover: false) : null
            }, loaded =>
            {
                loaded = loaded ?? new Loaded();
                LensArrived(loaded.Artwork);
                _glass.SetImage(FrostCopy(loaded.Artwork), FrostScale(loaded.Artwork));

                if (_thumb != null && loaded.Cover != null)
                {
                    _thumb.Source = loaded.Cover;
                    FadeIn(_thumb, 0.7, 0, SineOut);
                }

                if (useLogo)
                {
                    LogoLoaded(loaded.Logo);
                }
            });
        }

    }
}
