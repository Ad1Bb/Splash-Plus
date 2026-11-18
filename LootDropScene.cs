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
    /// Loot Drop: the game appears like a rare item. A beam of light falls, the cover drops in after it as an item
    /// card, lands with a bounce and flips from its back to its face, while rays in the game's "rarity" color (taken
    /// from the cover) turn slowly behind it. Without a cover, the logo or name stands where the card would be.
    /// </summary>
    internal sealed class LootDropScene : StyledScene
    {
        private const double CenterY = 0.42 * StageHeight;
        private const double CardWidth = 380;
        private const double CardHeight = 570;
        private const double CardRadius = 16;
        private const double RaysSize = 1500;
        private const double BeamWidth = 150;
        private const double RingSize = 320;
        private const double MarkSize = 14;
        private const double DimOpacity = 0.50;

        private static readonly Color NeutralTint = Color.FromRgb(0xDC, 0xDC, 0xDC);

        // Everything in the tint color, with its own alpha, so one color change reaches all of them.
        private readonly List<(Animatable Target, DependencyProperty Property, byte Alpha)> _tinted =
            new List<(Animatable, DependencyProperty, byte)>();

        private bool _started;
        private BitmapSource _arrivedCover;

        private readonly Image _backdrop = CoverImage(1.15);
        private readonly Canvas _raysCanvas = new Canvas();
        private readonly Grid _rays;
        private readonly RotateTransform _raysTurn = new RotateTransform(0, RaysSize / 2, RaysSize / 2);
        private readonly Ellipse _floorGlow;
        private readonly Rectangle _beam;
        private readonly ScaleTransform _beamScale = new ScaleTransform(1, 0);
        private readonly Ellipse _ring;
        private readonly ScaleTransform _ringScale = new ScaleTransform(0.5, 0.5);

        private readonly Grid _card;
        private readonly TranslateTransform _cardDrop = new TranslateTransform(0, -760);
        private readonly ScaleTransform _cardScale = new ScaleTransform(1, 1);
        private readonly Border _cardGlow;
        private readonly ScaleTransform _flip = new ScaleTransform(1, 1);
        private readonly Grid _back;
        private readonly Image _face;
        private readonly TranslateTransform _sheenShift = new TranslateTransform(-1.3 * CardWidth, 0);

        private readonly Grid _textArea;
        private readonly StackPanel _text = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        private readonly TextBlock _name;
        private readonly Image _logo;
        private readonly ScaleTransform _logoScale = new ScaleTransform(0.8, 0.8);
        private readonly StackPanel _marks;
        private readonly TextBlock _details;

        public LootDropScene(SplashGameInfo info, SplashImageSources sources)
            : base(info, sources)
        {
            bool card = HasCover;

            // 1–3. Base, the blurred artwork, dimming
            Stage.Children.Add(new Rectangle { Fill = Gray(0x0B) });
            _backdrop.Opacity = 0;
            RenderOptions.SetBitmapScalingMode(_backdrop, BitmapScalingMode.HighQuality);
            Stage.Children.Add(_backdrop);
            Stage.Children.Add(Dim(DimOpacity));

            // 4. Rays: 12 narrow wedges fading out from the middle, cached once and turned as a bitmap
            var wedges = new StreamGeometry();
            using (StreamGeometryContext context = wedges.Open())
            {
                double c = RaysSize / 2;
                for (int k = 0; k < 12; k++)
                {
                    double angle = k * Math.PI / 6;
                    context.BeginFigure(new Point(c, c), true, true);
                    context.LineTo(new Point(c + c * Math.Cos(angle - 0.06), c + c * Math.Sin(angle - 0.06)), false, false);
                    context.LineTo(new Point(c + c * Math.Cos(angle + 0.06), c + c * Math.Sin(angle + 0.06)), false, false);
                }
            }

            wedges.Freeze();
            var fade = new RadialGradientBrush();
            fade.GradientStops.Add(new GradientStop(Colors.Black, 0));
            fade.GradientStops.Add(new GradientStop(Colors.Black, 0.2));
            fade.GradientStops.Add(new GradientStop(Colors.Transparent, 1));
            _rays = new Grid
            {
                Width = RaysSize,
                Height = RaysSize,
                Opacity = 0,
                RenderTransform = _raysTurn,
                Children =
                {
                    new Path
                    {
                        Data = wedges,
                        Fill = TintBrush(1),
                        OpacityMask = fade,
                        CacheMode = new BitmapCache { RenderAtScale = 0.5 }
                    }
                }
            };
            Canvas.SetTop(_rays, CenterY - RaysSize / 2);
            _raysCanvas.Children.Add(_rays);
            Stage.Children.Add(_raysCanvas);

            // 5. Glow on the floor under the card
            var floor = new RadialGradientBrush();
            floor.GradientStops.Add(TintStop(0.45, 0));
            floor.GradientStops.Add(TintStop(0, 1));
            _floorGlow = new Ellipse
            {
                Width = 900,
                Height = 160,
                Fill = floor,
                Opacity = 0,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, CenterY + CardHeight / 2 - 80, 0, 0)
            };
            Stage.Children.Add(_floorGlow);

            // 6. Beam of light from the top edge down to the card's center
            var beam = new LinearGradientBrush { StartPoint = new Point(0, 0.5), EndPoint = new Point(1, 0.5) };
            beam.GradientStops.Add(TintStop(0, 0));
            beam.GradientStops.Add(TintStop(0.35, 0.32));
            beam.GradientStops.Add(new GradientStop(Color.FromArgb(235, 255, 255, 255), 0.5));
            beam.GradientStops.Add(TintStop(0.35, 0.68));
            beam.GradientStops.Add(TintStop(0, 1));
            _beam = new Rectangle
            {
                Width = BeamWidth,
                Height = CenterY,
                Fill = beam,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                RenderTransformOrigin = new Point(0.5, 0),
                RenderTransform = _beamScale
            };
            Stage.Children.Add(_beam);

            // 7. Ring of the impact
            _ring = new Ellipse
            {
                Width = RingSize,
                Height = RingSize,
                Stroke = TintBrush(1),
                StrokeThickness = 3,
                Opacity = 0,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, CenterY - RingSize / 2, 0, 0),
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = _ringScale
            };
            Stage.Children.Add(_ring);

            // 8. The item card
            if (card)
            {
                _cardGlow = new Border
                {
                    Margin = new Thickness(-6),
                    Opacity = 0.45,
                    CacheMode = new BitmapCache(),
                    Child = new Rectangle
                    {
                        Fill = TintBrush(0.7),
                        RadiusX = CardRadius + 6,
                        RadiusY = CardRadius + 6,
                        Effect = new BlurEffect { Radius = 60, KernelType = KernelType.Gaussian }
                    }
                };

                var stripes = new LinearGradientBrush
                {
                    MappingMode = BrushMappingMode.Absolute,
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(16 / Math.Sqrt(2), 16 / Math.Sqrt(2)),
                    SpreadMethod = GradientSpreadMethod.Repeat
                };
                stripes.GradientStops.Add(new GradientStop(Color.FromArgb(13, 255, 255, 255), 0));
                stripes.GradientStops.Add(new GradientStop(Color.FromArgb(13, 255, 255, 255), 0.125));
                stripes.GradientStops.Add(new GradientStop(Colors.Transparent, 0.125));
                stripes.GradientStops.Add(new GradientStop(Colors.Transparent, 1));
                _back = new Grid
                {
                    Children =
                    {
                        new Rectangle { Fill = stripes },
                        Diamond(120, TintBrush(0.18), TintBrush(1), 4),
                        Diamond(46, TintBrush(1), null, 0)
                    }
                };

                _face = CoverImage();
                _face.Opacity = 0;
                RenderOptions.SetBitmapScalingMode(_face, BitmapScalingMode.HighQuality);

                var content = new Grid
                {
                    Clip = new RectangleGeometry(new Rect(0, 0, CardWidth, CardHeight), CardRadius, CardRadius),
                    Children =
                    {
                        _back,
                        _face,
                        new Rectangle
                        {
                            Fill = AngledGradient(105, Stop(0, 255, 0.38), Stop(71, 255, 0.5), Stop(0, 255, 0.62)),
                            RenderTransform = _sheenShift
                        }
                    }
                };

                var flipping = new Grid
                {
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = _flip,
                    Children =
                    {
                        new Border
                        {
                            CornerRadius = new CornerRadius(CardRadius),
                            Background = Gray(0x10),
                            Effect = Shadow(30, 60, 0.6)
                        },
                        content,
                        new Border
                        {
                            CornerRadius = new CornerRadius(CardRadius),
                            BorderThickness = new Thickness(3),
                            BorderBrush = TintBrush(0.9)
                        }
                    }
                };

                var transforms = new TransformGroup();
                transforms.Children.Add(_cardScale);
                transforms.Children.Add(_cardDrop);
                _card = new Grid
                {
                    Width = CardWidth,
                    Height = CardHeight,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, CenterY - CardHeight / 2, 0, 0),
                    Opacity = 0,
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = transforms,
                    Children = { _cardGlow, flipping }
                };
                Stage.Children.Add(_card);
            }

            // 9. Logo or name, the rating marks and a line of details
            double nameSize = card ? 40 : 68;
            _name = Text(Info.Name, nameSize, 600, Brushes.White, 1.15, 2);
            _name.TextAlignment = TextAlignment.Center;
            _name.HorizontalAlignment = HorizontalAlignment.Center;
            _name.Effect = Shadow(2, 16, 0.45);
            _name.Opacity = 0;
            _name.RenderTransform = new TranslateTransform(0, 10);
            var title = new Grid { HorizontalAlignment = HorizontalAlignment.Center, Children = { _name } };
            if (UseLogo)
            {
                _logo = new Image
                {
                    Stretch = Stretch.Uniform,
                    Opacity = 0,
                    Effect = Shadow(3, 14, 0.5),
                    RenderTransformOrigin = new Point(0.5, 0.5),
                    RenderTransform = _logoScale
                };
                RenderOptions.SetBitmapScalingMode(_logo, BitmapScalingMode.HighQuality);
                title.Children.Add(_logo);
            }

            _text.Children.Add(title);

            int? marks = SplashText.RatingMarks(Info.Score);
            if (marks != null)
            {
                _marks = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 18, 0, 0)
                };
                for (int i = 0; i < 5; i++)
                {
                    _marks.Children.Add(RatingMark(i < marks.Value, i));
                }

                _text.Children.Add(_marks);
            }

            string details = SplashText.Join(Separator, Info.Genre, Info.ReleaseYear?.ToString());
            if (details != null)
            {
                _details = Text(details, 19, 350, White(0.70));
                _details.HorizontalAlignment = HorizontalAlignment.Center;
                _details.Margin = new Thickness(0, 14, 0, 0);
                _details.Effect = Shadow(2, 16, 0.45);
                _details.Opacity = 0;
                _details.RenderTransform = new TranslateTransform(0, 10);
                _text.Children.Add(_details);
            }

            _textArea = new Grid { VerticalAlignment = VerticalAlignment.Top, Children = { _text } };
            if (card)
            {
                _textArea.Margin = new Thickness(0, CenterY + CardHeight / 2 + 46, 0, 0);
                _text.VerticalAlignment = VerticalAlignment.Top;
            }
            else
            {
                // A box twice as high as the center line keeps the block's middle on it.
                _textArea.Height = 2 * CenterY;
                _text.VerticalAlignment = VerticalAlignment.Center;
            }

            Stage.Children.Add(_textArea);
            ApplyTint(NeutralTint, 0);
        }

        protected override void OnStageWidth(double width)
        {
            Canvas.SetLeft(_rays, width / 2 - RaysSize / 2);
            double textWidth = Math.Max(1, Math.Min(1300, 0.8 * width));
            _text.MaxWidth = 0.8 * width;
            if (HasCover)
            {
                FitText(_name, textWidth, 40, 2, 30, 2, 1.15);
            }
            else
            {
                FitText(_name, textWidth, 68, 2, 48, 2, 1.15);
            }
        }

        private bool Card => _card != null;

        protected override void Start()
        {
            _started = true;

            // Beam: drops down in its first quarter, then widens and fades to a faint shaft
            _beamScale.BeginAnimation(ScaleTransform.ScaleYProperty, Keys(0.15, (0, 0, null), (0.2875, 1, QuadIn)));
            _beamScale.BeginAnimation(ScaleTransform.ScaleXProperty, Keys(0.15, (0.2875, 1, null), (1.15, 2.2, CubicOut)));
            _beam.BeginAnimation(UIElement.OpacityProperty, Keys(0.15, (0.2875, 1, null), (1.15, 0.22, CubicOut)));

            if (Card)
            {
                Animate(_cardDrop, TranslateTransform.YProperty, -760, 0, 0.65, 0.35, BounceOut);
                Animate(_card, UIElement.OpacityProperty, 0, 1, 0.13, 0.35);
                var pulse = new DoubleAnimation(0.45, 1, TimeSpan.FromSeconds(2.6))
                {
                    BeginTime = TimeSpan.FromSeconds(1.4),
                    EasingFunction = SineInOut,
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                };
                _cardGlow.BeginAnimation(UIElement.OpacityProperty, pulse);
                if (_arrivedCover != null)
                {
                    FlipCard(_arrivedCover);
                }
            }

            Animate(_ring, UIElement.OpacityProperty, 0.9, 0, 0.7, 1.0, CubicOut);
            Animate(_ringScale, ScaleTransform.ScaleXProperty, 0.5, 2.1, 0.7, 1.0, CubicOut);
            Animate(_ringScale, ScaleTransform.ScaleYProperty, 0.5, 2.1, 0.7, 1.0, CubicOut);
            _floorGlow.BeginAnimation(UIElement.OpacityProperty, Keys(0.95, (0, 0, null), (0.36, 1, null), (1.2, 0.6, null)));

            Animate(_rays, UIElement.OpacityProperty, 0, 0.17, 0.9, 0.9);
            _raysTurn.BeginAnimation(RotateTransform.AngleProperty,
                new DoubleAnimation(0, 360, TimeSpan.FromSeconds(80)) { RepeatBehavior = RepeatBehavior.Forever });

            double marksAt = Card ? 1.55 : 1.20;
            if (_marks != null)
            {
                for (int i = 0; i < _marks.Children.Count; i++)
                {
                    var mark = (FrameworkElement)_marks.Children[i];
                    var scale = (ScaleTransform)mark.RenderTransform;
                    double delay = marksAt + i * 0.07;
                    Animate(mark, UIElement.OpacityProperty, 0, 1, 0.4, delay, BackOut);
                    Animate(scale, ScaleTransform.ScaleXProperty, 0.2, 1, 0.4, delay, BackOut);
                    Animate(scale, ScaleTransform.ScaleYProperty, 0.2, 1, 0.4, delay, BackOut);
                }
            }

            if (_details != null)
            {
                Rise(_details, marksAt);
            }

            WaitForLogo();
        }

        protected override void ShowLogo(BitmapSource logo)
        {
            _name.Visibility = Visibility.Collapsed;
            _logo.Source = logo;
            FitLogo(_logo, logo, (Card ? 0.20 : 0.30) * StageWidth, Card ? 110 : 300);
            double delay = FromShow(Card ? 1.4 : 1.05);
            Animate(_logo, UIElement.OpacityProperty, 0, 1, 0.7, delay, BackOut);
            Animate(_logoScale, ScaleTransform.ScaleXProperty, 0.8, 1, 0.7, delay, BackOut);
            Animate(_logoScale, ScaleTransform.ScaleYProperty, 0.8, 1, 0.7, delay, BackOut);
        }

        protected override void ShowName()
        {
            if (_logo != null)
            {
                _logo.Visibility = Visibility.Collapsed;
            }

            if (string.IsNullOrWhiteSpace(Info.Name))
            {
                _name.Visibility = Visibility.Collapsed;
                return;
            }

            Rise(_name, FromShow(Card ? 1.40 : 1.05));
        }

        private static void Rise(FrameworkElement element, double delay)
        {
            Animate(element, UIElement.OpacityProperty, 0, 1, 0.8, delay, CubicOut);
            Animate(element.RenderTransform, TranslateTransform.YProperty, 10, 0, 0.8, delay, CubicOut);
        }

        /// <summary>
        /// The card turns from its back to the cover: squeezed to an edge, the faces swap, then it opens again.
        /// Not before 1.15 s (it has to land first); the sheen starts sweeping 0.4 s after.
        /// </summary>
        private void FlipCard(BitmapSource cover)
        {
            _face.Source = cover;
            double at = FromShow(1.15);
            _flip.BeginAnimation(ScaleTransform.ScaleXProperty, Keys(at, (0, 1, null), (0.171, 0, QuadIn), (0.38, 1, QuadOut)));
            CutTo(_back, 0, at + 0.171);
            CutTo(_face, 1, at + 0.171);

            var sweep = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromSeconds(6),
                BeginTime = TimeSpan.FromSeconds(at + 0.4),
                RepeatBehavior = RepeatBehavior.Forever
            };
            sweep.KeyFrames.Add(new DiscreteDoubleKeyFrame(-1.3 * CardWidth, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            sweep.KeyFrames.Add(new EasingDoubleKeyFrame(1.3 * CardWidth, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.9)), SineInOut));
            _sheenShift.BeginAnimation(TranslateTransform.XProperty, sweep);
        }

        protected override void Close()
        {
            Animate(_text, UIElement.OpacityProperty, null, 0, 0.3);
            if (Card)
            {
                Animate(_cardDrop, TranslateTransform.YProperty, null, -280, 0.6, 0, CubicIn);
                Animate(_cardScale, ScaleTransform.ScaleXProperty, null, 0.55, 0.6, 0, CubicIn);
                Animate(_cardScale, ScaleTransform.ScaleYProperty, null, 0.55, 0.6, 0, CubicIn);
                Animate(_card, UIElement.OpacityProperty, null, 0, 0.6, 0, CubicIn);
            }

            Animate(_beamScale, ScaleTransform.ScaleXProperty, null, 1, 0.6, 0.25, CubicIn);
            Animate(_beamScale, ScaleTransform.ScaleYProperty, null, 0, 0.6, 0.25, CubicIn);
            Animate(_beam, UIElement.OpacityProperty, null, 0.6, 0.6, 0.25, CubicIn);
            Animate(_rays, UIElement.OpacityProperty, null, 0, 0.5);
            Animate(_floorGlow, UIElement.OpacityProperty, null, 0, 0.4);
        }

        public override void LoadImagesAsync(SplashImageSources sources, MonitorArea screen)
        {
            ScreenPixels(screen, out int width, out int height);
            bool useLogo = UseLogo;
            bool hasCover = HasCover;
            int logoWidth = (int)(width * 0.30);

            LoadInBackground(() =>
            {
                var loaded = new Loaded { Backdrop = ImageLoader.Load(Sources.Background, 480, 0, cover: false) };
                if (loaded.Backdrop == null)
                {
                    loaded.Backdrop = ImageLoader.Load(Sources.Cover, 480, 0, cover: false);
                    loaded.BackdropFromCover = true;
                }

                if (hasCover)
                {
                    loaded.Cover = ImageLoader.Load(Sources.Cover, UnitsToPixels(CardWidth, height), UnitsToPixels(CardHeight, height), cover: true);
                }

                loaded.Tint = TintOf(ImageLoader.Load(Sources.Cover, 32, 32, cover: false))
                    ?? TintOf(ImageLoader.Load(Sources.Background, 32, 32, cover: false));
                if (useLogo)
                {
                    loaded.Logo = ImageLoader.Load(Sources.Logo, logoWidth, UnitsToPixels(300, height), cover: false);
                }

                return loaded;
            }, loaded =>
            {
                loaded = loaded ?? new Loaded();
                if (loaded.Backdrop != null)
                {
                    _backdrop.Source = BlurCovering(loaded.Backdrop, loaded.BackdropFromCover ? 44 : 30);
                    FadeIn(_backdrop, 0.8, 0, SineOut);
                }

                if (loaded.Tint != null)
                {
                    int rgb = loaded.Tint.Value;
                    ApplyTint(Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb), 0.8);
                }

                // A cover that fails to load leaves the card on its back.
                if (Card && loaded.Cover != null)
                {
                    if (_started)
                    {
                        FlipCard(loaded.Cover);
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

        /// <summary>The rarity color of a small image (off the UI thread), or null without an image.</summary>
        private static int? TintOf(BitmapSource image)
        {
            if (image == null)
            {
                return null;
            }

            try
            {
                var pixels = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
                int count = pixels.PixelWidth * pixels.PixelHeight;
                var bytes = new byte[count * 4];
                pixels.CopyPixels(bytes, pixels.PixelWidth * 4, 0);
                return RarityTint.FromPixels(bytes, count);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Moves every tinted brush, gradient stop and glow to the color (instantly with 0 seconds).</summary>
        private void ApplyTint(Color color, double seconds)
        {
            foreach ((Animatable target, DependencyProperty property, byte alpha) in _tinted)
            {
                Color to = Color.FromArgb(alpha, color.R, color.G, color.B);
                if (seconds <= 0)
                {
                    target.SetValue(property, to);
                }
                else
                {
                    target.BeginAnimation(property, new ColorAnimation(to, TimeSpan.FromSeconds(seconds)) { EasingFunction = SineInOut });
                }
            }
        }

        private SolidColorBrush TintBrush(double alpha)
        {
            var brush = new SolidColorBrush();
            _tinted.Add((brush, SolidColorBrush.ColorProperty, (byte)Math.Round(alpha * 255)));
            return brush;
        }

        private GradientStop TintStop(double alpha, double offset)
        {
            var stop = new GradientStop { Offset = offset };
            _tinted.Add((stop, GradientStop.ColorProperty, (byte)Math.Round(alpha * 255)));
            return stop;
        }

        /// <summary>A square turned by 45°, centered in its parent.</summary>
        private static Rectangle Diamond(double size, Brush fill, Brush stroke, double strokeThickness)
        {
            return new Rectangle
            {
                Width = size,
                Height = size,
                Fill = fill,
                Stroke = stroke,
                StrokeThickness = strokeThickness,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform(45)
            };
        }

        /// <summary>One rating mark: a filled, glowing diamond in the tint, or an empty outline.</summary>
        private FrameworkElement RatingMark(bool filled, int index)
        {
            Rectangle diamond = filled
                ? Diamond(MarkSize, TintBrush(1), TintBrush(1), 2)
                : Diamond(MarkSize, null, White(0.35), 2);
            if (filled)
            {
                var glow = new DropShadowEffect { ShadowDepth = 0, BlurRadius = 10 };
                _tinted.Add((glow, DropShadowEffect.ColorProperty, 204));
                diamond.Effect = glow;
            }

            // The turned square reaches √2 × its size; the slot leaves 14 u between the diamonds.
            double slot = MarkSize * Math.Sqrt(2);
            return new Grid
            {
                Width = slot,
                Height = slot,
                Margin = new Thickness(index == 0 ? 0 : 14, 0, 0, 0),
                Opacity = 0,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(0.2, 0.2),
                Children = { diamond }
            };
        }

        private static IEasingFunction BounceOut
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
            public BitmapSource Backdrop { get; set; }
            public bool BackdropFromCover { get; set; }
            public BitmapSource Cover { get; set; }
            public BitmapSource Logo { get; set; }
            public int? Tint { get; set; }
        }
    }
}
