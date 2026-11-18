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
    /// A pane of frosted glass: a blurred copy of the artwork that lies exactly under the sharp one (see
    /// <see cref="AlignCopy"/>), clipped to the pane, with optional dimming, edge and shadow. The blur is rendered
    /// once; the pane only shows it. Moving or scaling the pane moves its copy with it; a <see cref="Reveal"/>
    /// transform moves only the pane's outline, so the copy stays in place under it.
    /// </summary>
    internal sealed class GlassPane : Grid
    {
        private readonly double _radius;
        private readonly Grid _inside = new Grid();
        private readonly Canvas _copyHolder = new Canvas();
        private readonly Image _copy = CoverImage();
        private readonly RectangleGeometry _shape = new RectangleGeometry();
        private readonly Grid _decor = new Grid();
        private readonly Grid _shadowSlot = new Grid();
        private (double Offset, double Blur, double Opacity)? _shadow;

        /// <param name="radius">Corner radius in units (0 for a strip).</param>
        /// <param name="dim">Black over the copy (0 for none).</param>
        /// <param name="fill">A tint over the copy, or null.</param>
        public GlassPane(double radius, double dim = 0, Brush fill = null)
        {
            _radius = radius;
            IsHitTestVisible = false;
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;

            _copy.Opacity = 0;
            RenderOptions.SetBitmapScalingMode(_copy, BitmapScalingMode.HighQuality);
            _copyHolder.Children.Add(_copy);
            _inside.Children.Add(_copyHolder);
            if (dim > 0)
            {
                _inside.Children.Add(Dim(dim));
            }

            if (fill != null)
            {
                _inside.Children.Add(new Rectangle { Fill = fill });
            }

            _inside.Clip = _shape;
            Children.Add(_shadowSlot);
            Children.Add(_inside);
            Children.Add(_decor);
        }

        /// <summary>Moves only the pane's outline (clip and edge), not the copy: for panes that grow or slide open.</summary>
        public Transform Reveal
        {
            set
            {
                _shape.Transform = value;
                _decor.RenderTransform = value;
            }
        }

        /// <summary>Sets the pane's size in units (its shape, and its shadow when it has one).</summary>
        public void SetSize(double width, double height)
        {
            Width = Math.Max(0, width);
            Height = Math.Max(0, height);
            _shape.Rect = new Rect(0, 0, Width, Height);
            _shape.RadiusX = _radius;
            _shape.RadiusY = _radius;
            _shadowSlot.Children.Clear();
            if (_shadow.HasValue)
            {
                (double offset, double blur, double opacity) = _shadow.Value;
                _shadowSlot.Children.Add(SoftShadow(Width, Height, _radius, offset, blur, opacity));
            }
        }

        /// <summary>Places the copy so it lies under the stage's artwork: the pane's top left corner on the stage.</summary>
        public void AlignCopy(Point topLeft, double stageWidth, double stageHeight)
        {
            _copy.Width = stageWidth;
            _copy.Height = stageHeight;
            Canvas.SetLeft(_copy, -topLeft.X);
            Canvas.SetTop(_copy, -topLeft.Y);
        }

        /// <summary>The blurred copy (scaled around the stage center like the artwork under it); fades in in 800 ms.</summary>
        public void SetImage(BitmapSource blurred, double scale)
        {
            if (blurred == null)
            {
                return;
            }

            _copy.Source = blurred;
            _copy.RenderTransform = new ScaleTransform(scale, scale);
            FadeIn(_copy, 0.8, 0, SineOut);
        }

        /// <summary>A shadow outside the pane (offset down, blur, opacity), drawn from the next <see cref="SetSize"/>.</summary>
        public GlassPane WithShadow(double offset, double blur, double opacity)
        {
            _shadow = (offset, blur, opacity);
            return this;
        }

        /// <summary>
        /// Glass edge: a 1 u outline at 16 %, a 1.5 u light line at 22 % along the top (wrapping the top corners) and
        /// a faint reflection from the top left corner (7 % → 0 at 45 % of the diagonal).
        /// </summary>
        public GlassPane WithEdge(double hairline)
        {
            _decor.Children.Clear();
            var sheen = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
            sheen.GradientStops.Add(new GradientStop(Color.FromArgb(18, 255, 255, 255), 0));
            sheen.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.45));
            _decor.Children.Add(new Rectangle { Fill = sheen, RadiusX = _radius, RadiusY = _radius });
            _decor.Children.Add(new Border
            {
                CornerRadius = new CornerRadius(_radius),
                BorderThickness = new Thickness(Math.Max(1, hairline)),
                BorderBrush = White(0.16)
            });
            _decor.Children.Add(new Border
            {
                CornerRadius = new CornerRadius(_radius),
                BorderThickness = new Thickness(0, Math.Max(1.5, hairline), 0, 0),
                BorderBrush = White(0.22)
            });
            return this;
        }

        /// <summary>
        /// Strip edge: a 1 u light line at 30 % on top, a 1 u line at 6 % under it, and a glow fading out by
        /// <paramref name="glowHeight"/> units.
        /// </summary>
        public GlassPane WithStripEdge(double hairline, double glowHeight)
        {
            _decor.Children.Clear();
            double line = Math.Max(1, hairline);
            var glow = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(0, 1) };
            glow.GradientStops.Add(new GradientStop(Color.FromArgb(15, 255, 255, 255), 0));
            glow.GradientStops.Add(new GradientStop(Color.FromArgb(0, 255, 255, 255), 1));
            _decor.Children.Add(new Rectangle { Fill = glow, Height = glowHeight, VerticalAlignment = VerticalAlignment.Top });
            _decor.Children.Add(new Rectangle { Fill = White(0.30), Height = line, VerticalAlignment = VerticalAlignment.Top });
            _decor.Children.Add(new Rectangle { Fill = White(0.06), Height = line, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, line, 0, 0) });
            return this;
        }
    }
}
