using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static SplashAddonPlus.StyleKit;

namespace SplashAddonPlus
{
    /// <summary>
    /// Base of the Frost styles (Pop, Swing, Meet and Rise Frost): the sharp artwork over a #1A1A1A base, with one
    /// pane of frosted glass that blurs only what lies under it; the cover frame, the logo or name, the text lines
    /// and the spring moves are shared, so the styles differ only in layout and timing.
    /// </summary>
    internal abstract class FrostScene : StyledScene
    {
        /// <summary>Space between the glass and what stands on it.</summary>
        protected const double GlassPadding = 44;

        private const double PaneRadius = 18;
        private const double StripOverhang = 60;

        private readonly bool _strip;
        private readonly ScaleTransform _paneScale = new ScaleTransform(1, 1);
        private readonly TranslateTransform _stripSlide = new TranslateTransform();
        private double _stripHeight;

        protected FrostScene(SplashGameInfo info, SplashImageSources sources, bool strip)
            : base(info, sources)
        {
            _strip = strip;

            // 1–2. Base and the sharp artwork
            Stage.Children.Add(new Rectangle { Fill = Gray(0x1A) });
            AddSharpBackdrop();

            // 3. One pane of glass (placed once the text is final); none when there is nothing to blur.
            Glass = new GlassPane(strip ? 0 : PaneRadius);
            if (strip)
            {
                Glass.Reveal = _stripSlide;
            }
            else
            {
                Glass.WithShadow(20, 50, 0.18);
                Glass.Opacity = 0;
                Glass.RenderTransformOrigin = new Point(0.5, 0.5);
                Glass.RenderTransform = _paneScale;
            }

            if (HasCover || !string.IsNullOrEmpty(Sources.Background))
            {
                Stage.Children.Add(Glass);
            }

            // The logo or the name, in one slot that is animated as a whole.
            Name = Text(Info.Name, 44, 600, Brushes.White, 1.12, 2);
            Name.Effect = TextShadow();
            Title.Children.Add(Name);
            if (UseLogo)
            {
                Logo = new Image { Stretch = Stretch.Uniform, Effect = Shadow(3, 14, 0.45) };
                RenderOptions.SetBitmapScalingMode(Logo, BitmapScalingMode.HighQuality);
                Title.Children.Add(Logo);
            }
        }

        protected GlassPane Glass { get; }

        /// <summary>The logo or the name; place it in the layout, it is shown once the logo rule decides.</summary>
        protected Grid Title { get; } = new Grid();

        protected TextBlock Name { get; }

        protected Image Logo { get; }

        /// <summary>Height of the cover frame (for decoding the cover at its size on screen).</summary>
        protected abstract double CoverHeight { get; }

        /// <summary>Logo width as a share of the screen width, and the logo's height limit in units.</summary>
        protected abstract double LogoWidthShare { get; }

        protected abstract double LogoMaxHeight { get; }

        /// <summary>Width of the cover frame, following the cover's proportions.</summary>
        protected double CoverWidth => CoverHeight * CoverAspect;

        /// <summary>Starts what does not depend on the logo/name choice (the cover).</summary>
        protected abstract void StartIntro();

        /// <summary>The text is final (logo or name): place the glass with <see cref="PlacePane"/> or <see cref="PlaceStrip"/>.</summary>
        protected abstract void PlaceGlass();

        /// <summary>Starts the text animations (the title and what follows it).</summary>
        protected abstract void RevealText();

        /// <summary>The cover arrived (sharp, decoded at its size on screen).</summary>
        protected abstract void ShowCover(BitmapSource cover);

        protected sealed override void Start()
        {
            StartIntro();
            WaitForLogo();
        }

        protected sealed override void ShowLogo(BitmapSource logo)
        {
            Name.Visibility = Visibility.Collapsed;
            Logo.Source = logo;
            FitLogo(Logo, logo, Math.Min(LogoWidthShare * StageWidth, Title.MaxWidth), LogoMaxHeight);
            Settle();
        }

        protected sealed override void ShowName()
        {
            if (Logo != null)
            {
                Logo.Visibility = Visibility.Collapsed;
            }

            Settle();
        }

        private void Settle()
        {
            Stage.UpdateLayout();
            PlaceGlass();
            double delay = FromShow(0.1);
            if (_strip)
            {
                // The strip slides up from behind the bottom edge (→ −10 u → 0); the blur stays in place under it.
                Spring(_stripSlide, TranslateTransform.YProperty, _stripHeight + StripOverhang, -10, 0, 1.0, delay);
            }
            else
            {
                Spring(_paneScale, 0.94, 1.012, 1.0, 1.0, delay);
                SpringFade(Glass, 1.0, delay);
            }

            RevealText();
        }

        /// <summary>A rounded pane at <paramref name="pane"/> (stage units).</summary>
        protected void PlacePane(Rect pane)
        {
            Glass.SetSize(pane.Width, pane.Height);
            Glass.WithEdge(OnePixel);
            PlaceGlassAt(pane.TopLeft);
        }

        /// <summary>The strip: full width from <paramref name="top"/> down past the bottom edge.</summary>
        protected void PlaceStrip(double top)
        {
            _stripHeight = StageHeight - top;
            Glass.SetSize(StageWidth, _stripHeight + StripOverhang);
            Glass.WithStripEdge(OnePixel, 0.3 * _stripHeight);
            _stripSlide.Y = _stripHeight + StripOverhang;
            PlaceGlassAt(new Point(0, top));
        }

        private void PlaceGlassAt(Point topLeft)
        {
            Glass.Margin = new Thickness(topLeft.X, topLeft.Y, 0, 0);
            Glass.AlignCopy(topLeft, StageWidth, StageHeight);
        }

        /// <summary>Fits the name into the title width (large size in 2 lines, else the small one, then "…").</summary>
        protected void FitTitle(double width, double large, double small, bool centered)
        {
            Title.MaxWidth = width;
            Title.HorizontalAlignment = centered ? HorizontalAlignment.Center : HorizontalAlignment.Left;
            Name.HorizontalAlignment = Title.HorizontalAlignment;
            Name.TextAlignment = centered ? TextAlignment.Center : TextAlignment.Left;
            FitText(Name, width, large, 2, small, 2, 1.12);
            if (Logo != null)
            {
                Logo.HorizontalAlignment = Title.HorizontalAlignment;
            }
        }

        /// <summary>An element's box on the stage (read after layout; includes its own transforms).</summary>
        protected Rect Bounds(FrameworkElement element)
        {
            Point topLeft = element.TranslatePoint(new Point(0, 0), Stage);
            return new Rect(topLeft, new Size(element.ActualWidth, element.ActualHeight));
        }

        /// <summary>The cover frame: 12 u corners, a 10 % placeholder until the image arrives, a soft shadow.</summary>
        protected Grid NewCoverFrame(out Image image)
        {
            Grid frame = CoverFrame(CoverWidth, CoverHeight, 12, false, White(0.10), Shadow(26, 60, 0.45), out image);
            frame.HorizontalAlignment = HorizontalAlignment.Left;
            frame.VerticalAlignment = VerticalAlignment.Top;
            frame.Opacity = 0;
            return frame;
        }

        /// <summary>The cover fades in when it arrives (500 ms).</summary>
        protected static void FadeInCover(Image image, BitmapSource cover)
        {
            if (image != null && cover != null)
            {
                image.Source = cover;
                FadeIn(image, 0.5, 0, SineOut);
            }
        }

        /// <summary>Shadow of the white text: 2 u down, 16 u blur, 45 %.</summary>
        protected static DropShadowEffect TextShadow() => Shadow(2, 16, 0.45);

        /// <summary>A line of white text with its shadow, or null when empty.</summary>
        protected static TextBlock Line(string text, double size, int weight, double alpha, double lineHeight = 1.4, int maxLines = 1)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            TextBlock line = Text(text, size, weight, White(alpha), lineHeight, maxLines);
            line.Effect = TextShadow();
            return line;
        }

        /// <summary>The details line under a title (Semibold 19 u, 90 %), or null when all values are empty.</summary>
        protected static TextBlock InfoLine(params string[] values) => Line(SplashText.Join(Separator, values), 19, 600, 0.9, 1.3);

        /// <summary>Wraps an element for its own entrance (hidden until then) with the given transform.</summary>
        protected static Border Item(UIElement content, Transform transform, Thickness margin, HorizontalAlignment align)
        {
            return new Border { Child = content, Opacity = 0, RenderTransform = transform, Margin = margin, HorizontalAlignment = align };
        }

        public sealed override void LoadImagesAsync(SplashImageSources sources, MonitorArea screen)
        {
            ScreenPixels(screen, out int width, out int height);
            bool useLogo = UseLogo;
            bool hasCover = HasCover;
            double coverWidth = CoverWidth;
            double coverHeight = CoverHeight;
            int logoWidth = (int)(width * LogoWidthShare);
            int logoHeight = UnitsToPixels(LogoMaxHeight, height);

            LoadInBackground(() => new Loaded
            {
                Artwork = LoadArtwork(width, height),
                Cover = hasCover ? ImageLoader.Load(Sources.Cover, UnitsToPixels(coverWidth, height), UnitsToPixels(coverHeight, height), cover: true) : null,
                Logo = useLogo ? ImageLoader.Load(Sources.Logo, logoWidth, logoHeight, cover: false) : null
            }, loaded =>
            {
                loaded = loaded ?? new Loaded();
                ShowSharpBackdrop(loaded.Artwork);
                BitmapSource copy = GlassCopy(loaded.Artwork, 24, 44);
                if (copy == null)
                {
                    // Nothing to blur: no glass, the style stands on the plain base.
                    Glass.Visibility = Visibility.Collapsed;
                }
                else
                {
                    Glass.SetImage(copy, loaded.Artwork.FromCover ? 1.15 : 1.06);
                }

                ShowCover(loaded.Cover);
                if (useLogo)
                {
                    LogoLoaded(loaded.Logo);
                }
            });
        }

        private sealed class Loaded
        {
            public Artwork Artwork { get; set; }
            public BitmapSource Cover { get; set; }
            public BitmapSource Logo { get; set; }
        }
    }
}
