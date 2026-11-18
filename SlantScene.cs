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
    /// Slant: the art pulls into focus; two identical frosted glass slabs slanted the same way (/ /) slide in from both
    /// edges near the bottom, knock their slanted edges together and bounce back to a fixed gap. The left one carries
    /// the logo or name and a line of details, the right one the cover at its tip by the gap.
    /// </summary>
    internal sealed class SlantScene : LensScene
    {
        private const double Gap = 60;
        private const double SlabHeight = 240;
        private const double SlabBottom = StageHeight - Gap;
        private const double SlabTop = SlabBottom - SlabHeight;
        private const double TipWidth = 160;

        /// <summary>Horizontal run of the 12° slant over the slab's height.</summary>
        private static readonly double Slant = Math.Tan(12 * Math.PI / 180) * SlabHeight;

        /// <summary>Horizontal gap that leaves 60 u between the slanted edges, measured across them.</summary>
        private static readonly double GapAcross = Gap / Math.Cos(12 * Math.PI / 180);

        private static readonly double TipBoxWidth = Slant + TipWidth;

        private bool _started;
        private double _travel;

        private readonly ScreenGlass _left = new ScreenGlass(White(0.08), 18, 32, 0.25);
        private readonly ScreenGlass _right = new ScreenGlass(White(0.08), 18, 32, 0.25);
        private readonly TranslateTransform _leftShift = new TranslateTransform();
        private readonly TranslateTransform _rightShift = new TranslateTransform();
        private readonly Slab _leftSlab = new Slab();
        private readonly Slab _rightSlab = new Slab();

        private readonly Grid _textBlock;
        private readonly TextBlock _name;
        private readonly Image _logo;
        private readonly TextBlock _details;

        private readonly Grid _tip;
        private readonly ScaleTransform _tipScale = new ScaleTransform(1.08, 1.08, TipBoxWidth / 2, SlabHeight / 2);
        private readonly Image _cover;

        private readonly Path _flash = new Path
        {
            Stroke = Brushes.White,
            StrokeThickness = 2,
            Opacity = 0,
            IsHitTestVisible = false,
            Effect = new DropShadowEffect { Color = Colors.White, ShadowDepth = 0, BlurRadius = 6, Opacity = 0.9 }
        };

        public SlantScene(SplashGameInfo info, SplashImageSources sources)
            : base(info, sources)
        {
            // 1–2. Base and the art pulling into focus
            AddLens();

            // 3. Left slab with the text
            _left.Motion.Children.Add(_leftShift);
            _left.Opacity = 0;
            _left.Decor.Children.Add(_leftSlab.Layer);

            var column = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left, MaxWidth = 600 };
            var title = new Grid { HorizontalAlignment = HorizontalAlignment.Left };
            if (!string.IsNullOrWhiteSpace(Info.Name))
            {
                _name = Text(Info.Name, 40, 600, Brushes.White, 1.1, 2);
                FitText(_name, 600, 40, 2, 28, 2, 1.1);
                _name.Effect = Shadow(2, 16, 0.45);
                _name.Opacity = 0;
                _name.RenderTransform = new TranslateTransform(-40, 0);
                title.Children.Add(_name);
            }

            if (UseLogo)
            {
                _logo = new Image
                {
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Opacity = 0,
                    Effect = Shadow(3, 14, 0.45),
                    RenderTransform = new TranslateTransform(-40, 0)
                };
                RenderOptions.SetBitmapScalingMode(_logo, BitmapScalingMode.HighQuality);
                title.Children.Add(_logo);
            }

            column.Children.Add(title);
            string details = SplashText.Join(Separator, Info.Genre, Info.ReleaseYear?.ToString(), Info.Developer);
            if (details != null)
            {
                _details = Text(details, 18, 350, White(0.85), 1.3);
                _details.MaxWidth = 600;
                _details.Margin = new Thickness(0, 10, 0, 0);
                _details.Effect = Shadow(2, 16, 0.45);
                _details.Opacity = 0;
                _details.RenderTransform = new TranslateTransform(-40, 0);
                column.Children.Add(_details);
            }

            _textBlock = new Grid { Width = 600, Height = SlabHeight, Children = { column } };
            _leftSlab.Layer.Children.Add(_textBlock);
            Stage.Children.Add(_left);

            // 4. Right slab with the cover at its tip
            _right.Motion.Children.Add(_rightShift);
            _right.Opacity = 0;
            _right.Decor.Children.Add(_rightSlab.Layer);
            if (HasCover)
            {
                _cover = new Image { Stretch = Stretch.Fill, Opacity = 0 };
                RenderOptions.SetBitmapScalingMode(_cover, BitmapScalingMode.HighQuality);
                var tipShape = Parallelogram(0, TipWidth);
                _tip = new Grid
                {
                    Width = TipBoxWidth,
                    Height = SlabHeight,
                    Clip = tipShape,
                    Children =
                    {
                        new Rectangle { Fill = White(0.08) },
                        new Canvas { RenderTransform = _tipScale, Children = { _cover } },
                        new Rectangle { Fill = AngledGradient(135, Gloss.White(0.12, 0), Gloss.White(0, 0.4)) }
                    }
                };
                _rightSlab.Layer.Children.Add(_tip);
                _rightSlab.Layer.Children.Add(new Path
                {
                    Data = new LineGeometry(new Point(Slant + TipWidth, 0), new Point(TipWidth, SlabHeight)),
                    Stroke = White(0.45),
                    StrokeThickness = 1.5
                });
            }

            Stage.Children.Add(_right);

            // 5. Flash where the slabs hit
            Stage.Children.Add(new Canvas { Children = { _flash } });
        }

        /// <summary>A parallelogram of the slab's height, slanted like the slabs, starting at x on its bottom edge.</summary>
        private static Geometry Parallelogram(double x, double width)
        {
            var figure = new PathFigure { StartPoint = new Point(x + Slant, 0), IsClosed = true, IsFilled = true };
            figure.Segments.Add(new PolyLineSegment(new[] { new Point(x + Slant + width, 0), new Point(x + width, SlabHeight), new Point(x, SlabHeight) }, true));
            var geometry = new PathGeometry(new[] { figure });
            geometry.Freeze();
            return geometry;
        }

        private static Geometry Polygon(params Point[] points)
        {
            var figure = new PathFigure { StartPoint = points[0], IsClosed = true, IsFilled = true };
            figure.Segments.Add(new PolyLineSegment(new List<Point>(points).GetRange(1, points.Length - 1), true));
            var geometry = new PathGeometry(new[] { figure });
            geometry.Freeze();
            return geometry;
        }

        protected override void OnStageWidth(double width)
        {
            double rightEdge = (width - GapAcross) / 2;
            double x0 = width - rightEdge;
            _travel = rightEdge + Slant + 20;

            // Left slab: from far off the left edge to its slanted edge left of the gap.
            Geometry left = Polygon(new Point(0, 0), new Point(rightEdge + Slant / 2 + width, 0),
                new Point(rightEdge - Slant / 2 + width, SlabHeight), new Point(0, SlabHeight));
            _left.SetShape(left, new Point(-width, SlabTop));
            _left.SetStageSize(width, StageHeight);
            _leftSlab.Layout(left, width, rightEdge + Slant / 2);
            Canvas.SetLeft(_textBlock, width + SafeLeft + 150);

            // Right slab: from the slanted edge right of the gap to far off the right edge.
            double origin = x0 - Slant / 2;
            Geometry right = Polygon(new Point(Slant, 0), new Point(2 * width - origin, 0),
                new Point(2 * width - origin, SlabHeight), new Point(0, SlabHeight));
            _right.SetShape(right, new Point(origin, SlabTop));
            _right.SetStageSize(width, StageHeight);
            _rightSlab.Layout(right, 0, width - origin);

            _flash.Data = new LineGeometry(new Point(width / 2 + Slant / 2, SlabTop), new Point(width / 2 - Slant / 2, SlabBottom));

            if (!_started)
            {
                _leftShift.X = -_travel;
                _rightShift.X = _travel;
            }
        }

        protected override void Start()
        {
            _started = true;
            StartLens();

            // One movement: speeding into the hit (gap closed), then bouncing back out to the gap.
            _leftShift.BeginAnimation(TranslateTransform.XProperty, Knock(-_travel, GapAcross / 2));
            _rightShift.BeginAnimation(TranslateTransform.XProperty, Knock(_travel, -GapAcross / 2));
            Animate(_left, UIElement.OpacityProperty, 0, 1, 0.33, 0.8);
            Animate(_right, UIElement.OpacityProperty, 0, 1, 0.33, 0.8);
            _flash.BeginAnimation(UIElement.OpacityProperty, Keys(1.53, (0, 0, null), (0.0675, 0.9, null), (0.45, 0, null)));

            if (_tip != null)
            {
                Animate(_tipScale, ScaleTransform.ScaleXProperty, 1.08, 1, 2.4, 0.9, QuintOut);
                Animate(_tipScale, ScaleTransform.ScaleYProperty, 1.08, 1, 2.4, 0.9, QuintOut);
            }

            _leftSlab.StartSheen();
            _rightSlab.StartSheen();
            if (_details != null)
            {
                SlideIn(_details, 1.45);
            }

            WaitForLogo();
        }

        private static DoubleAnimationUsingKeyFrames Knock(double from, double hit)
        {
            var animation = new DoubleAnimationUsingKeyFrames { BeginTime = TimeSpan.FromSeconds(0.8) };
            animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(from, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            animation.KeyFrames.Add(new SplineDoubleKeyFrame(hit, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.75)), new KeySpline(0.45, 0, 0.9, 0.55)));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.3)), CubicOut));
            return animation;
        }

        private static void SlideIn(FrameworkElement element, double delay)
        {
            Animate(element, UIElement.OpacityProperty, 0, 1, 1.4, delay, ExpoOut);
            Animate(element.RenderTransform, TranslateTransform.XProperty, -40, 0, 1.4, delay, ExpoOut);
        }

        protected override void ShowLogo(BitmapSource logo)
        {
            if (_name != null)
            {
                _name.Visibility = Visibility.Collapsed;
            }

            _logo.Source = logo;
            FitLogo(_logo, logo, 360, 100);
            SlideIn(_logo, FromShow(1.3));
        }

        protected override void ShowName()
        {
            if (_logo != null)
            {
                _logo.Visibility = Visibility.Collapsed;
            }

            if (_name != null)
            {
                SlideIn(_name, FromShow(1.3));
            }
        }

        protected override void Close()
        {
            CloseLens();
            Animate(_textBlock, UIElement.OpacityProperty, null, 0, 0.3);
            Animate(_leftShift, TranslateTransform.XProperty, null, -450, 0.7, 0, CubicIn);
            Animate(_left, UIElement.OpacityProperty, null, 0, 0.7, 0, CubicIn);
            Animate(_rightShift, TranslateTransform.XProperty, null, 450, 0.7, 0, CubicIn);
            Animate(_right, UIElement.OpacityProperty, null, 0, 0.7, 0, CubicIn);
        }

        public override void LoadImagesAsync(SplashImageSources sources, MonitorArea screen)
        {
            ScreenPixels(screen, out int width, out int height);
            bool useLogo = UseLogo;
            bool hasCover = HasCover;

            LoadInBackground(() => new Loaded
            {
                Artwork = LoadArtwork(width, height),
                Cover = hasCover ? ImageLoader.Load(Sources.Cover, UnitsToPixels(TipBoxWidth, height), UnitsToPixels(SlabHeight, height), cover: true) : null,
                Logo = useLogo ? ImageLoader.Load(Sources.Logo, UnitsToPixels(360, height), UnitsToPixels(100, height), cover: false) : null
            }, loaded =>
            {
                loaded = loaded ?? new Loaded();
                LensArrived(loaded.Artwork);
                _left.SetCopy(FrostCopy(loaded.Artwork), FrostScale(loaded.Artwork));
                _right.SetCopy(FrostCopy(loaded.Artwork), FrostScale(loaded.Artwork));
                if (_cover != null && loaded.Cover != null)
                {
                    PlaceCover(loaded.Cover);
                    FadeIn(_cover, 0.7, 0, SineOut);
                }

                if (useLogo)
                {
                    LogoLoaded(loaded.Logo);
                }
            });
        }

        /// <summary>The cover fills the tip's box, anchored at 50 % across and 35 % down.</summary>
        private void PlaceCover(BitmapSource cover)
        {
            double aspect = cover.PixelHeight > 0 ? (double)cover.PixelWidth / cover.PixelHeight : 2.0 / 3;
            double w = TipBoxWidth;
            double h = w / aspect;
            if (h < SlabHeight)
            {
                h = SlabHeight;
                w = h * aspect;
            }

            _cover.Source = cover;
            _cover.Width = w;
            _cover.Height = h;
            Canvas.SetLeft(_cover, (TipBoxWidth - w) * 0.5);
            Canvas.SetTop(_cover, (SlabHeight - h) * 0.35);
        }

        /// <summary>What lies on one slab: gloss over its part on screen, the inner edge line and the sheen.</summary>
        private sealed class Slab
        {
            private readonly Rectangle _gloss = new Rectangle
            {
                Height = SlabHeight,
                Fill = AngledGradient(135, Gloss.White(0.12, 0), Gloss.White(0.03, 0.35), Gloss.White(0, 0.55))
            };

            private readonly Path _edge = new Path { Stroke = White(0.45), StrokeThickness = 3 };
            private readonly Canvas _sheenHolder = new Canvas();
            private Rectangle _sheen;
            private TranslateTransform _sheenShift;
            private double _visibleWidth;

            public Slab()
            {
                Layer.Children.Add(_gloss);
                Layer.Children.Add(_edge);
                Layer.Children.Add(_sheenHolder);
            }

            /// <summary>Everything on the slab, in its own coordinates.</summary>
            public Canvas Layer { get; } = new Canvas();

            /// <summary>
            /// Sizes the slab's layers: the edge follows the whole outline (the clip keeps its inner 1.5 u), the gloss
            /// and sheen cover the part that is on screen when the slab rests.
            /// </summary>
            public void Layout(Geometry shape, double visibleLeft, double visibleWidth)
            {
                _edge.Data = shape;
                _gloss.Width = Math.Max(0, visibleWidth);
                Canvas.SetLeft(_gloss, visibleLeft);

                _visibleWidth = Math.Max(1, visibleWidth);
                _sheenHolder.Children.Clear();
                _sheen = Gloss.Sheen(_visibleWidth, SlabHeight, 0.22, out _sheenShift);
                _sheenHolder.Children.Add(_sheen);
                Canvas.SetLeft(_sheenHolder, visibleLeft);
            }

            public void StartSheen()
            {
                if (_sheenShift != null)
                {
                    Gloss.Sweep(_sheenShift, _visibleWidth, 9, 1.26, 3.6);
                }
            }
        }
    }
}
