using System;
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
    /// A glass shape of any outline over the whole stage. Its frost (a blurred copy of the art) stays aligned with the
    /// screen; <see cref="Motion"/> moves and scales only the window onto it, together with the shadow and whatever is
    /// placed in <see cref="Decor"/> (in the shape's own coordinates, clipped to it). Like real glass sliding over a
    /// picture, the picture seen through it stays put.
    /// </summary>
    internal sealed class ScreenGlass : Grid
    {
        private readonly double _shadowOffset;
        private readonly Path _shadow;
        private readonly Image _copy = CoverImage();
        private readonly GeometryGroup _frostClip = new GeometryGroup();
        private readonly TranslateTransform _origin = new TranslateTransform();
        private readonly Canvas _decorLayer = new Canvas();

        /// <param name="fill">Over the frost: milky white or smoke.</param>
        public ScreenGlass(Brush fill, double shadowOffset, double shadowBlur, double shadowOpacity)
        {
            IsHitTestVisible = false;
            _shadowOffset = shadowOffset;

            // The shadow is blurred once into a bitmap; moving it only moves the bitmap.
            _shadow = new Path
            {
                Fill = new SolidColorBrush(Color.FromArgb((byte)Math.Round(shadowOpacity * 255), 0, 0, 0)),
                Effect = new BlurEffect { Radius = shadowBlur, KernelType = KernelType.Gaussian },
                CacheMode = new BitmapCache(),
                RenderTransform = Motion
            };

            _copy.Opacity = 0;
            RenderOptions.SetBitmapScalingMode(_copy, BitmapScalingMode.HighQuality);
            var clipTransform = new TransformGroup();
            clipTransform.Children.Add(Motion);
            clipTransform.Children.Add(_origin);
            _frostClip.Transform = clipTransform;
            var frost = new Grid { Clip = _frostClip, Children = { _copy, new Rectangle { Fill = fill } } };

            Decor.RenderTransform = Motion;
            _decorLayer.Children.Add(Decor);

            Children.Add(new Canvas { Children = { _shadow } });
            Children.Add(frost);
            Children.Add(_decorLayer);
        }

        /// <summary>Moves and scales the glass (in the shape's own coordinates), but not the picture in its frost.</summary>
        public TransformGroup Motion { get; } = new TransformGroup();

        /// <summary>Content on the glass, in the shape's own coordinates and clipped to it.</summary>
        public Grid Decor { get; } = new Grid();

        /// <summary>The outline (starting at 0, 0) and where its origin lies on the stage.</summary>
        public void SetShape(Geometry shape, Point origin)
        {
            Geometry frozen = shape.IsFrozen ? shape : (Geometry)shape.GetAsFrozen();
            _frostClip.Children.Clear();
            _frostClip.Children.Add(frozen);
            _origin.X = origin.X;
            _origin.Y = origin.Y;

            _shadow.Data = frozen;
            Canvas.SetLeft(_shadow, origin.X);
            Canvas.SetTop(_shadow, origin.Y + _shadowOffset);

            Rect bounds = frozen.Bounds;
            Decor.Width = Math.Max(0, bounds.Right);
            Decor.Height = Math.Max(0, bounds.Bottom);
            Decor.Clip = frozen;
            Canvas.SetLeft(Decor, origin.X);
            Canvas.SetTop(Decor, origin.Y);
        }

        /// <summary>The frost covers the stage (call when the stage width is known).</summary>
        public void SetStageSize(double width, double height)
        {
            _copy.Width = width;
            _copy.Height = height;
        }

        /// <summary>The blurred copy, scaled around the stage center like the art; fades in in 0.9 s.</summary>
        public void SetCopy(BitmapSource blurred, double scale)
        {
            if (blurred == null)
            {
                return;
            }

            _copy.Source = blurred;
            _copy.RenderTransform = new ScaleTransform(scale, scale);
            FadeIn(_copy, 0.9, 0, SineOut);
        }
    }

    /// <summary>
    /// A reflection on glass: a gradient layer that trails behind the glass's movement (it enters from an offset) and
    /// drifts slowly afterwards. Offsets are shares of the layer's own size.
    /// </summary>
    internal sealed class Reflection
    {
        private readonly TranslateTransform _enter = new TranslateTransform();
        private readonly TranslateTransform _drift = new TranslateTransform();
        private readonly double _width;
        private readonly double _height;

        public Reflection(FrameworkElement element, double left, double top, double width, double height)
        {
            Element = element;
            _width = width;
            _height = height;
            element.Width = width;
            element.Height = height;
            element.IsHitTestVisible = false;
            Canvas.SetLeft(element, left);
            Canvas.SetTop(element, top);
            var transforms = new TransformGroup();
            transforms.Children.Add(_enter);
            transforms.Children.Add(_drift);
            element.RenderTransform = transforms;
        }

        public FrameworkElement Element { get; }

        /// <summary>Starts offset and settles in place (ExponentialEase Out).</summary>
        public void EnterFrom(double xShare, double yShare, double seconds, double delay)
        {
            _enter.X = xShare * _width;
            _enter.Y = yShare * _height;
            Animate(_enter, TranslateTransform.XProperty, xShare * _width, 0, seconds, delay, ExpoOut);
            Animate(_enter, TranslateTransform.YProperty, yShare * _height, 0, seconds, delay, ExpoOut);
        }

        /// <summary>Drifts to the offset and back forever (SineEase InOut).</summary>
        public void Drift(double xShare, double yShare, double seconds, double delay)
        {
            foreach ((DependencyProperty property, double to) in new[] { (TranslateTransform.XProperty, xShare * _width), (TranslateTransform.YProperty, yShare * _height) })
            {
                if (to == 0)
                {
                    continue;
                }

                _drift.BeginAnimation(property, new DoubleAnimation(0, to, TimeSpan.FromSeconds(seconds))
                {
                    BeginTime = TimeSpan.FromSeconds(delay),
                    EasingFunction = SineInOut,
                    AutoReverse = true,
                    RepeatBehavior = RepeatBehavior.Forever
                });
            }
        }

        /// <summary>Closing: slides to the offset in 0.6 s (CubicEase In).</summary>
        public void Leave(double xShare, double yShare)
        {
            Animate(_enter, TranslateTransform.XProperty, null, xShare * _width, 0.6, 0, CubicIn);
            Animate(_enter, TranslateTransform.YProperty, null, yShare * _height, 0.6, 0, CubicIn);
        }
    }

    /// <summary>Layers of glossy glass: reflection cap, streaks, lit inner edges and a sweeping sheen.</summary>
    internal static class Gloss
    {
        public static GradientStop White(double alpha, double offset) =>
            new GradientStop(Color.FromArgb((byte)Math.Round(alpha * 255), 255, 255, 255), offset);

        /// <summary>
        /// An elliptical reflection cap: the ellipse spans (x, y, w, h) as shares of the surface, with a radial gradient
        /// out to its edge.
        /// </summary>
        public static Reflection Cap(double width, double height, double x, double y, double w, double h, params GradientStop[] stops)
        {
            var fill = new RadialGradientBrush();
            foreach (GradientStop stop in stops)
            {
                fill.GradientStops.Add(stop);
            }

            return new Reflection(new Ellipse { Fill = fill }, x * width, y * height, w * width, h * height);
        }

        /// <summary>Streaks: a layer twice the surface's width (from −50 %) with a 115° gradient.</summary>
        public static Reflection Streaks(double width, double height, params GradientStop[] stops)
        {
            return new Reflection(new Rectangle { Fill = AngledGradient(115, stops) }, -0.5 * width, 0, 2 * width, height);
        }

        /// <summary>
        /// Lit inner edges of a shape (like CSS inset shadows): light along the top left (offset +1.5, +1.5), a fainter
        /// line along the bottom right (−1, −1) and, when <paramref name="around"/> is above 0, a 1 u line all round.
        /// </summary>
        public static Grid LitEdges(Geometry shape, double topLeft, double bottomRight, double around)
        {
            var edges = new Grid { IsHitTestVisible = false };
            edges.Children.Add(Crescent(shape, 1.5, 1.5, topLeft));
            edges.Children.Add(Crescent(shape, -1, -1, bottomRight));
            if (around > 0)
            {
                edges.Children.Add(new Path { Data = shape, Stroke = StyleKit.White(around), StrokeThickness = 2, Clip = shape });
            }

            return edges;
        }

        private static Path Crescent(Geometry shape, double dx, double dy, double alpha)
        {
            Geometry moved = shape.Clone();
            moved.Transform = new TranslateTransform(dx, dy);
            return new Path { Data = new CombinedGeometry(GeometryCombineMode.Exclude, shape, moved), Fill = StyleKit.White(alpha) };
        }

        /// <summary>A sheen band (105°, peak <paramref name="alpha"/>) waiting off the surface to the left.</summary>
        public static Rectangle Sheen(double width, double height, double alpha, out TranslateTransform shift)
        {
            shift = new TranslateTransform(-1.3 * width, 0);
            return new Rectangle
            {
                Width = width,
                Height = height,
                Fill = AngledGradient(105, White(0, 0.38), White(alpha, 0.5), White(0, 0.62)),
                RenderTransform = shift
            };
        }

        /// <summary>Every <paramref name="period"/> seconds from <paramref name="start"/>, the sheen crosses in <paramref name="sweep"/> seconds.</summary>
        public static void Sweep(TranslateTransform shift, double width, double period, double sweep, double start)
        {
            var animation = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromSeconds(period),
                BeginTime = TimeSpan.FromSeconds(start),
                RepeatBehavior = RepeatBehavior.Forever
            };
            animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(-1.3 * width, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            animation.KeyFrames.Add(new EasingDoubleKeyFrame(1.3 * width, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(sweep)), SineInOut));
            shift.BeginAnimation(TranslateTransform.XProperty, animation);
        }
    }
}
