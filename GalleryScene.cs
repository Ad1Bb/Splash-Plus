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
    /// Gallery: on a dark wall under a flickering spotlight hang two pictures in thin black frames, the background
    /// (16:9) and the cover (2:3), equally spaced; one drops in from above, the other rises from below.
    /// </summary>
    internal sealed class GalleryScene : StyledScene
    {
        private const double WallSigma = 50;
        private const double WallDim = 0.72;
        private const double FrameBorder = 3;
        private const double PairHeight = 562.5;
        private const double MinGap = 80;
        private const double Wide = 16.0 / 9;
        private const double Tall = 2.0 / 3;
        private const double DropHeight = 140;
        private const double BounceHeight = 16;

        private readonly bool _hasBackground;

        private readonly Grid _wall = new Grid { Opacity = 0 };
        private readonly Image _wallImage = CoverImage(1.2);
        private readonly Rectangle _spotlight;
        private readonly Grid _area;

        private readonly Border _left;
        private readonly Image _leftPicture;
        private readonly TranslateTransform _leftShift = new TranslateTransform(0, -DropHeight);
        private readonly Border _right;
        private readonly Image _rightPicture;
        private readonly TranslateTransform _rightShift = new TranslateTransform(0, DropHeight);

        private readonly TextBlock _name;
        private readonly TranslateTransform _nameShift = new TranslateTransform(0, 12);

        public GalleryScene(SplashGameInfo info, SplashImageSources sources)
            : base(info, sources)
        {
            _hasBackground = !string.IsNullOrEmpty(Sources.Background);

            // 1. Base
            var baseBrush = new RadialGradientBrush { Center = new Point(0.5, 0.3), GradientOrigin = new Point(0.5, 0.3), RadiusX = 0.71, RadiusY = 1.0 };
            baseBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x2A, 0x2A, 0x2A), 0));
            baseBrush.GradientStops.Add(new GradientStop(Color.FromRgb(0x11, 0x11, 0x11), 0.75));
            Stage.Children.Add(new Rectangle { Fill = baseBrush });

            // 2–3. Wall: the blurred artwork, dimmed
            _wall.Children.Add(_wallImage);
            Stage.Children.Add(_wall);
            Stage.Children.Add(Dim(WallDim));

            // 4. Spotlight from the ceiling
            var light = new RadialGradientBrush { Center = new Point(0.5, -0.08), GradientOrigin = new Point(0.5, -0.08), RadiusX = 0.42, RadiusY = 0.75 };
            light.GradientStops.Add(Stop(43, 255, 0));
            light.GradientStops.Add(Stop(0, 255, 0.72));
            _spotlight = new Rectangle { Fill = light, Opacity = 0 };
            Stage.Children.Add(_spotlight);

            // 5. The pictures: background on the left, cover on the right; a lone picture hangs in the middle.
            _area = SafeArea();
            if (_hasBackground)
            {
                _left = Frame(_leftShift, out _leftPicture);
                _area.Children.Add(_left);
            }

            if (HasCover)
            {
                // A lone cover drops in from above like a single picture.
                _right = Frame(_hasBackground ? _rightShift : _leftShift, out _rightPicture);
                _area.Children.Add(_right);
            }

            // Without pictures: the name in the middle
            if (_left == null && _right == null)
            {
                _name = Text(Info.Name, 52, 700, Brushes.White, 1.2, 3, new FontFamily("Georgia"));
                _name.TextAlignment = TextAlignment.Center;
                _name.HorizontalAlignment = HorizontalAlignment.Center;
                _name.VerticalAlignment = VerticalAlignment.Center;
                _name.Opacity = 0;
                _name.RenderTransform = _nameShift;
                _area.Children.Add(_name);
            }
        }

        private bool Pair => _left != null && _right != null;

        /// <summary>
        /// Inner height of the pair and the gap: the same gap at both edges and between the frames, at least 80 u
        /// (the pictures shrink when the safe area is too narrow).
        /// </summary>
        private static void PairLayout(double safeWidth, out double height, out double gap)
        {
            height = PairHeight;
            gap = (safeWidth - (height * Wide + 2 * FrameBorder) - (height * Tall + 2 * FrameBorder)) / 3;
            if (gap < MinGap)
            {
                gap = MinGap;
                height = (safeWidth - 3 * MinGap - 4 * FrameBorder) / (Wide + Tall);
            }
        }

        protected override void OnStageWidth(double width)
        {
            if (_name != null)
            {
                _name.MaxWidth = 0.7 * SafeWidth;
                return;
            }

            if (Pair)
            {
                PairLayout(SafeWidth, out double height, out double gap);
                Place(_left, height * Wide, height, gap);
                Place(_right, height * Tall, height, gap + height * Wide + 2 * FrameBorder + gap);
            }
            else if (_left != null)
            {
                Place(_left, 1000, PairHeight, null);
            }
            else
            {
                Place(_right, 420, 630, null);
            }
        }

        /// <summary>Sizes a frame's window and hangs it centered vertically, at <paramref name="left"/> or in the middle.</summary>
        private static void Place(Border frame, double innerWidth, double innerHeight, double? left)
        {
            frame.Child.SetValue(FrameworkElement.WidthProperty, innerWidth);
            frame.Child.SetValue(FrameworkElement.HeightProperty, innerHeight);
            frame.HorizontalAlignment = left.HasValue ? HorizontalAlignment.Left : HorizontalAlignment.Center;
            frame.Margin = new Thickness(left ?? 0, (StageHeight - innerHeight) / 2 - FrameBorder, 0, 0);
        }

        protected override void Start()
        {
            var flicker = new DoubleAnimationUsingKeyFrames { BeginTime = TimeSpan.FromSeconds(0.25) };
            flicker.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            flicker.KeyFrames.Add(new LinearDoubleKeyFrame(0.6, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(144))));
            flicker.KeyFrames.Add(new LinearDoubleKeyFrame(0.2, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(240))));
            flicker.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1200))));
            _spotlight.BeginAnimation(UIElement.OpacityProperty, flicker);

            // Hung on the wall: lands, bounces back a little and settles (the right one mirrored, from below).
            Hang(_left, _leftShift, -1);
            Hang(_right, _hasBackground ? _rightShift : _leftShift, _hasBackground ? 1 : -1);

            if (_name != null)
            {
                Animate(_name, UIElement.OpacityProperty, 0, 1, 0.9, 0.6, CubicOut);
                Animate(_nameShift, TranslateTransform.YProperty, 12, 0, 0.9, 0.6, CubicOut);
            }
        }

        private static void Hang(Border frame, TranslateTransform shift, double side)
        {
            if (frame == null)
            {
                return;
            }

            Animate(frame, UIElement.OpacityProperty, 0, 1, 0.35, 0.15, SineOut);
            var drop = new DoubleAnimationUsingKeyFrames { BeginTime = TimeSpan.FromSeconds(0.15) };
            drop.KeyFrames.Add(new DiscreteDoubleKeyFrame(side * DropHeight, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            drop.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.55)), QuadIn));
            drop.KeyFrames.Add(new EasingDoubleKeyFrame(side * BounceHeight, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.75)), QuadOut));
            drop.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.97)), QuadIn));
            shift.BeginAnimation(TranslateTransform.YProperty, drop);
        }

        protected override void Close()
        {
            // The light goes out in two steps.
            var off = new DoubleAnimationUsingKeyFrames();
            off.KeyFrames.Add(new DiscreteDoubleKeyFrame(0.5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(110))));
            off.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(220))));
            _spotlight.BeginAnimation(UIElement.OpacityProperty, off);

            // The pair parts: the left one up, the right one down. A lone picture sinks a little.
            Leave(_left, _leftShift, Pair ? -24 : 24);
            Leave(_right, _hasBackground ? _rightShift : _leftShift, 24);
            Leave(_name, _nameShift, 24);
        }

        private static void Leave(FrameworkElement element, TranslateTransform shift, double by)
        {
            if (element != null)
            {
                Animate(element, UIElement.OpacityProperty, null, 0, 0.7, 0.2, CubicIn);
                Animate(shift, TranslateTransform.YProperty, null, by, 0.7, 0.2, CubicIn);
            }
        }

        public override void LoadImagesAsync(SplashImageSources sources, MonitorArea screen)
        {
            ScreenPixels(screen, out int width, out int height);
            bool pair = Pair;
            bool left = _left != null;
            bool right = _right != null;

            // Decode at the size on screen.
            PairLayout(Math.Min(SafeAreaWidth, StageHeight * width / height), out double pairHeight, out _);
            double leftWidth = pair ? pairHeight * Wide : 1000;
            double leftHeight = pair ? pairHeight : PairHeight;
            double rightWidth = pair ? pairHeight * Tall : 420;
            double rightHeight = pair ? pairHeight : 630;

            LoadInBackground(() => new Loaded
            {
                Wall = ImageLoader.Load(Sources.Background, 480, 0, cover: false) ?? ImageLoader.Load(Sources.Cover, 480, 0, cover: false),
                Left = left ? ImageLoader.Load(Sources.Background, UnitsToPixels(leftWidth, height), UnitsToPixels(leftHeight, height), cover: true) : null,
                Right = right ? ImageLoader.Load(Sources.Cover, UnitsToPixels(rightWidth, height), UnitsToPixels(rightHeight, height), cover: true) : null
            }, loaded =>
            {
                loaded = loaded ?? new Loaded();
                if (loaded.Wall != null)
                {
                    _wallImage.Source = Blur(loaded.Wall, SigmaToRadius(WallSigma) / StageHeight);
                    FadeIn(_wall, 1.0, 0, SineOut);
                }

                ShowPicture(_leftPicture, loaded.Left);
                ShowPicture(_rightPicture, loaded.Right);
            });
        }

        private static void ShowPicture(Image picture, BitmapSource image)
        {
            if (picture != null && image != null)
            {
                picture.Source = image;
                FadeIn(picture, 0.9, 0, SineOut);
            }
        }

        /// <summary>A picture in a 3 u black frame with a deep shadow (its window is sized by <see cref="Place"/>).</summary>
        private static Border Frame(TranslateTransform shift, out Image picture)
        {
            picture = CoverImage();
            picture.Opacity = 0;
            RenderOptions.SetBitmapScalingMode(picture, BitmapScalingMode.HighQuality);
            return new Border
            {
                BorderBrush = Brushes.Black,
                BorderThickness = new Thickness(FrameBorder),
                Effect = Shadow(36, 80, 0.7),
                Child = new Grid { Background = Gray(0x1A), ClipToBounds = true, Children = { picture } },
                VerticalAlignment = VerticalAlignment.Top,
                Opacity = 0,
                RenderTransform = shift
            };
        }

        private sealed class Loaded
        {
            public BitmapSource Wall { get; set; }
            public BitmapSource Left { get; set; }
            public BitmapSource Right { get; set; }
        }
    }
}
