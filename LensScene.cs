using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static SplashAddonPlus.StyleKit;

namespace SplashAddonPlus
{
    /// <summary>
    /// Base of the styles whose art pulls into focus like a lens (Focus, Slant, Iris Gloss, Slate Gloss): on a dark
    /// gradient, the art (the background, or the cover) appears heavily blurred and zoomed in from the moment it
    /// arrives, sharpens and pulls back to the full frame, then pushes in very slowly. Sharpness is only a crossfade of
    /// three ready-made blurs; no blur radius is ever animated. On closing it falls back to the blur.
    /// </summary>
    internal abstract class LensScene : StyledScene
    {
        private readonly ScaleTransform _kb = new ScaleTransform(1, 1);
        private readonly ScaleTransform _zoom = new ScaleTransform(1.12, 1.12);
        private readonly Image _high = CoverImage();
        private readonly Image _middle = CoverImage();
        private readonly Image _sharpest = CoverImage();
        private bool _lensStarted;
        private Layers _arrived;

        protected LensScene(SplashGameInfo info, SplashImageSources sources)
            : base(info, sources)
        {
        }

        /// <summary>Adds the base gradient (160°, #1E1E1E → #0B0B0B) and the focus layers; call first.</summary>
        protected void AddLens()
        {
            Stage.Children.Add(new Rectangle { Fill = AngledGradient(160, Stop(255, 0x1E, 0), Stop(255, 0x0B, 1)) });
            foreach (Image layer in new[] { _high, _middle, _sharpest })
            {
                layer.Opacity = 0;
                RenderOptions.SetBitmapScalingMode(layer, BitmapScalingMode.HighQuality);
            }

            // KB around Z around the layers, cached so the zooms scale a finished bitmap.
            Stage.Children.Add(new Grid
            {
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = _kb,
                Children =
                {
                    new Grid
                    {
                        RenderTransformOrigin = new Point(0.5, 0.5),
                        RenderTransform = _zoom,
                        Children = { new Grid { CacheMode = new BitmapCache(), Children = { _high, _middle, _sharpest } } }
                    }
                }
            });
        }

        /// <summary>Call from Start: from now on the focus pull runs as soon as the art is there.</summary>
        protected void StartLens()
        {
            _lensStarted = true;
            if (_arrived != null)
            {
                PullFocus(_arrived);
                _arrived = null;
            }
        }

        /// <summary>The art has arrived (UI thread): makes the three blurs once and starts the pull (A = now).</summary>
        protected void LensArrived(Artwork artwork)
        {
            if (artwork?.Small == null)
            {
                return;
            }

            bool fromCover = artwork.FromCover;
            var layers = new Layers
            {
                FromCover = fromCover,
                High = BlurCovering(artwork.Small, fromCover ? 72 : 64),
                Middle = BlurCovering(artwork.Small, fromCover ? 40 : 22),
                Sharp = fromCover ? BlurCovering(artwork.Small, 16) : artwork.Sharp ?? artwork.Small
            };
            if (_lensStarted)
            {
                PullFocus(layers);
            }
            else
            {
                _arrived = layers;
            }
        }

        /// <summary>Closing: the sharp layers fade back to the blur while the art zooms in a little.</summary>
        protected void CloseLens()
        {
            Animate(_sharpest, UIElement.OpacityProperty, null, 0, 0.5, 0, SineInOut);
            Animate(_middle, UIElement.OpacityProperty, null, 0, 0.7, 0.2, SineInOut);
            Animate(_zoom, ScaleTransform.ScaleXProperty, null, 1.06, 0.9, 0, CubicInOut);
            Animate(_zoom, ScaleTransform.ScaleYProperty, null, 1.06, 0.9, 0, CubicInOut);
        }

        /// <summary>The frosted copy for glass: σ 30 u (42 u when the cover stands in).</summary>
        protected BitmapSource FrostCopy(Artwork artwork) => GlassCopy(artwork, 30, 42);

        /// <summary>Scale of the frosted copy around the stage center: 1.08 (1.18 when the cover stands in).</summary>
        protected static double FrostScale(Artwork artwork) => artwork?.FromCover == true ? 1.18 : 1.08;

        private void PullFocus(Layers layers)
        {
            double scale = layers.FromCover ? 1.15 : 1;
            foreach ((Image image, BitmapSource source) in new[] { (_high, layers.High), (_middle, layers.Middle), (_sharpest, layers.Sharp) })
            {
                image.Source = source;
                image.RenderTransform = new ScaleTransform(scale, scale);
            }

            Animate(_high, UIElement.OpacityProperty, 0, 1, 0.5, 0, SineOut);
            Animate(_middle, UIElement.OpacityProperty, 0, 1, 1.0, 0.35, SineInOut);
            Animate(_sharpest, UIElement.OpacityProperty, 0, 1, 1.3, 0.95, SineInOut);
            Animate(_zoom, ScaleTransform.ScaleXProperty, 1.12, 1, 2.8, 0, QuintOut);
            Animate(_zoom, ScaleTransform.ScaleYProperty, 1.12, 1, 2.8, 0, QuintOut);
            Animate(_kb, ScaleTransform.ScaleXProperty, 1, 1.035, 24, 2.8, SineInOut);
            Animate(_kb, ScaleTransform.ScaleYProperty, 1, 1.035, 24, 2.8, SineInOut);
        }

        /// <summary>The images of the lens styles, loaded together off the UI thread.</summary>
        protected sealed class Loaded
        {
            public Artwork Artwork { get; set; }
            public BitmapSource Cover { get; set; }
            public BitmapSource Logo { get; set; }
        }

        private sealed class Layers
        {
            public bool FromCover { get; set; }
            public BitmapSource High { get; set; }
            public BitmapSource Middle { get; set; }
            public BitmapSource Sharp { get; set; }
        }
    }
}
