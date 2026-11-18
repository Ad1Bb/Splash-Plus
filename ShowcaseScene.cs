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
    /// Showcase: the cover stands in the middle of the sharp artwork with a floor reflection and a sheen sweeping
    /// across it, set off from the artwork by a deep shadow. Without a cover, the logo or name stands there instead.
    /// </summary>
    internal sealed class ShowcaseScene : StyledScene
    {
        private const double CoverWidth = 427;
        private const double CoverHeight = 640;
        private const double CoverTop = StageHeight / 2 - 360;
        private const double ReflectionHeight = 170;
        private const double Radius = 14;
        private const double LogoWidthShare = 0.30;
        private const double LogoMaxHeight = 360;

        private bool _coverLoadFinished;

        private readonly Grid _coverGroup;
        private readonly TranslateTransform _coverGroupShift = new TranslateTransform(0, 50);
        private readonly ScaleTransform _coverGroupScale = new ScaleTransform(0.96, 0.96);
        private readonly ScaleTransform _frameScale = new ScaleTransform(1, 1);
        private readonly Image _coverImage;
        private readonly Image _reflectionImage;
        private readonly Rectangle _shimmer;
        private readonly TranslateTransform _shimmerShift = new TranslateTransform(-CoverWidth, 0);
        private readonly Rectangle _sheen;
        private readonly TranslateTransform _sheenShift = new TranslateTransform(-1.3 * CoverWidth, 0);

        private readonly Grid _title;
        private readonly TranslateTransform _titleShift = new TranslateTransform(0, 30);
        private readonly TextBlock _name;
        private readonly Image _logo;

        public ShowcaseScene(SplashGameInfo info, SplashImageSources sources)
            : base(info, sources)
        {
            // 1–2. Base and the sharp artwork (no dimming)
            Stage.Children.Add(new Rectangle { Fill = Gray(0x10) });
            AddSharpBackdrop();

            if (HasCover)
            {
                // 3. Cover, its frame and reflection
                _coverImage = CoverImage();
                _coverImage.Opacity = 0;
                _shimmer = new Rectangle
                {
                    Fill = AngledGradient(100, Stop(0, 255, 0.3), Stop(23, 255, 0.5), Stop(0, 255, 0.7)),
                    RenderTransform = _shimmerShift
                };
                _sheen = new Rectangle
                {
                    Fill = AngledGradient(105, Stop(0, 255, 0.38), Stop(66, 255, 0.5), Stop(0, 255, 0.62)),
                    RenderTransform = _sheenShift
                };
                var inside = new Grid { Clip = new RectangleGeometry(new Rect(0, 0, CoverWidth - 2, CoverHeight - 2), Radius - 1, Radius - 1) };
                inside.Children.Add(_shimmer);
                inside.Children.Add(_coverImage);
                inside.Children.Add(_sheen);

                // Two soft shadows (40 u down 100 u blur 75 %, and an even 160 u at 50 %) and a 12 % white outline
                // set the cover off from the sharp artwork.
                var frame = new Grid
                {
                    Width = CoverWidth,
                    Height = CoverHeight,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = _frameScale,
                    Children =
                    {
                        SoftShadow(CoverWidth, CoverHeight, Radius, 0, 160, 0.5),
                        SoftShadow(CoverWidth, CoverHeight, Radius, 40, 100, 0.75),
                        new Border
                        {
                            CornerRadius = new CornerRadius(Radius),
                            Background = White(0.06),
                            BorderBrush = White(0.12),
                            BorderThickness = new Thickness(1),
                            Child = inside
                        }
                    }
                };

                // The cover flipped upside down, so its bottom edge is mirrored at the top of the reflection.
                _reflectionImage = CoverImage();
                _reflectionImage.Width = CoverWidth;
                _reflectionImage.Height = CoverHeight;
                _reflectionImage.VerticalAlignment = VerticalAlignment.Top;
                _reflectionImage.Opacity = 0;
                _reflectionImage.RenderTransform = new ScaleTransform(1, -1);

                // 42 % at the top of the reflection, gone at 90 % of its height. The mask is in the image's own
                // (unflipped) coordinates, where the top of the reflection is the bottom edge of the cover.
                var fade = new LinearGradientBrush
                {
                    MappingMode = BrushMappingMode.Absolute,
                    StartPoint = new Point(0, CoverHeight),
                    EndPoint = new Point(0, CoverHeight - 0.90 * ReflectionHeight)
                };
                fade.GradientStops.Add(new GradientStop(Color.FromArgb(107, 0, 0, 0), 0));
                fade.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 1));
                _reflectionImage.OpacityMask = fade;
                var reflection = new Grid
                {
                    Width = CoverWidth,
                    Height = ReflectionHeight,
                    Margin = new Thickness(0, CoverHeight + 10, 0, 0),
                    VerticalAlignment = VerticalAlignment.Top,
                    Clip = new RectangleGeometry(new Rect(0, 0, CoverWidth, ReflectionHeight), Radius, Radius)
                };
                // In a Canvas the full-size image is not clipped to the 170 u slot before it is flipped.
                reflection.Children.Add(new Canvas { Children = { _reflectionImage } });

                var transforms = new TransformGroup();
                transforms.Children.Add(_coverGroupScale);
                transforms.Children.Add(_coverGroupShift);
                double groupHeight = CoverHeight + 10 + ReflectionHeight;
                _coverGroup = new Grid
                {
                    Width = CoverWidth,
                    Height = groupHeight,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, CoverTop, 0, 0),
                    Opacity = 0,
                    RenderTransformOrigin = new Point(0.5, CoverHeight / 2 / groupHeight),
                    RenderTransform = transforms
                };
                _coverGroup.Children.Add(reflection);
                _coverGroup.Children.Add(new Grid { Height = CoverHeight, VerticalAlignment = VerticalAlignment.Top, Children = { frame } });
                Stage.Children.Add(_coverGroup);
            }
            else
            {
                // 3. Without a cover: the logo, or the name, in the middle
                _title = new Grid
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Opacity = 0,
                    RenderTransform = _titleShift
                };
                _name = Text(Info.Name, 72, 600, Brushes.White, 1.12, 2);
                _name.TextAlignment = TextAlignment.Center;
                _name.HorizontalAlignment = HorizontalAlignment.Center;
                _name.Effect = Shadow(2, 20, 0.6);
                _title.Children.Add(_name);
                if (UseLogo)
                {
                    _logo = new Image { Stretch = Stretch.Uniform, Effect = Shadow(4, 18, 0.5) };
                    RenderOptions.SetBitmapScalingMode(_logo, BitmapScalingMode.HighQuality);
                    _title.Children.Add(_logo);
                }

                Stage.Children.Add(_title);
            }
        }

        protected override void OnStageWidth(double width)
        {
            if (_name != null)
            {
                FitText(_name, Math.Min(1500, 0.8 * width), 72, 2, 52, 2, 1.12);
            }
        }

        protected override void Start()
        {
            if (_coverGroup != null)
            {
                Animate(_coverGroup, UIElement.OpacityProperty, 0, 1, 1.1, 0.15, QuintOut);
                Animate(_coverGroupShift, TranslateTransform.YProperty, 50, 0, 1.1, 0.15, QuintOut);
                Animate(_coverGroupScale, ScaleTransform.ScaleXProperty, 0.96, 1, 1.1, 0.15, QuintOut);
                Animate(_coverGroupScale, ScaleTransform.ScaleYProperty, 0.96, 1, 1.1, 0.15, QuintOut);

                // Placeholder shimmer until the cover arrives.
                var shimmer = new DoubleAnimation(-CoverWidth, CoverWidth, TimeSpan.FromSeconds(1.3))
                {
                    EasingFunction = SineInOut,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                if (!_coverLoadFinished)
                {
                    _shimmerShift.BeginAnimation(TranslateTransform.XProperty, shimmer);
                }
            }
            else
            {
                WaitForLogo();
            }
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
            Animate(_title, UIElement.OpacityProperty, 0, 1, 1.1, FromShow(0.3), QuintOut);
            Animate(_titleShift, TranslateTransform.YProperty, 30, 0, 1.1, FromShow(0.3), QuintOut);
        }

        protected override void Close()
        {
            if (_coverGroup != null)
            {
                Animate(_coverGroupScale, ScaleTransform.ScaleXProperty, null, 1.06, 0.7, 0, CubicIn);
                Animate(_coverGroupScale, ScaleTransform.ScaleYProperty, null, 1.06, 0.7, 0, CubicIn);
                Animate(_coverGroup, UIElement.OpacityProperty, null, 0, 0.7, 0, CubicIn);
            }
            else
            {
                Animate(_title, UIElement.OpacityProperty, null, 0, 0.45, 0, QuadIn);
            }
        }

        public override void LoadImagesAsync(SplashImageSources sources, MonitorArea screen)
        {
            ScreenPixels(screen, out int width, out int height);
            bool hasCover = HasCover;
            bool useLogo = UseLogo && !hasCover;

            LoadInBackground(() => new Loaded
            {
                Artwork = LoadArtwork(width, height),
                Cover = hasCover ? ImageLoader.Load(Sources.Cover, UnitsToPixels(CoverWidth, height), UnitsToPixels(CoverHeight, height), cover: true) : null,
                Logo = useLogo ? ImageLoader.Load(Sources.Logo, (int)(width * LogoWidthShare), UnitsToPixels(LogoMaxHeight, height), cover: false) : null
            }, loaded =>
            {
                loaded = loaded ?? new Loaded();
                ShowSharpBackdrop(loaded.Artwork, 32, 1.2);
                if (_coverGroup != null)
                {
                    _coverLoadFinished = true;
                    StopShimmer();
                    if (loaded.Cover != null)
                    {
                        _coverImage.Source = loaded.Cover;
                        _reflectionImage.Source = loaded.Cover;
                        FadeIn(_coverImage, 0.6, 0, SineOut);
                        FadeIn(_reflectionImage, 0.6, 0, SineOut);
                        Animate(_frameScale, ScaleTransform.ScaleXProperty, 0.97, 1, 0.9, 0, BackOut);
                        Animate(_frameScale, ScaleTransform.ScaleYProperty, 0.97, 1, 0.9, 0, BackOut);
                        StartSheen();
                    }
                }

                if (useLogo)
                {
                    LogoLoaded(loaded.Logo);
                }
            });
        }

        private void StopShimmer()
        {
            Animate(_shimmer, UIElement.OpacityProperty, null, 0, 0.4, 0, null,
                () => _shimmerShift.BeginAnimation(TranslateTransform.XProperty, null));
        }

        /// <summary>Every 8 s the sheen sweeps across the cover in 1.6 s, then rests off the frame.</summary>
        private void StartSheen()
        {
            double offFrame = 1.3 * CoverWidth;
            var sweep = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromSeconds(8),
                BeginTime = TimeSpan.FromSeconds(0.7),
                RepeatBehavior = RepeatBehavior.Forever
            };
            sweep.KeyFrames.Add(new DiscreteDoubleKeyFrame(-offFrame, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            sweep.KeyFrames.Add(new EasingDoubleKeyFrame(offFrame, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.6)), SineInOut));
            _sheenShift.BeginAnimation(TranslateTransform.XProperty, sweep);
        }

        private sealed class Loaded
        {
            public Artwork Artwork { get; set; }
            public BitmapSource Cover { get; set; }
            public BitmapSource Logo { get; set; }
        }
    }
}
