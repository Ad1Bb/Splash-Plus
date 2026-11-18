using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static SplashAddonPlus.StyleKit;

namespace SplashAddonPlus
{
    /// <summary>
    /// Pop Frost: the cover, the logo or name and a line of details stand on the screen's axis, on one pane of frosted
    /// glass over the sharp artwork.
    /// </summary>
    internal sealed class PopFrostScene : FrostScene
    {
        private const double FrameHeight = 570;
        private const double FrameCenter = 0.40 * StageHeight;
        private const double TextWidth = 680;
        private const double PaneWidth = 760;
        private const double TextGap = 36;

        private readonly Grid _frame;
        private readonly Image _cover;
        private readonly ScaleTransform _coverScale = new ScaleTransform(0.6, 0.6);
        private readonly StackPanel _block;
        private readonly Border _title;
        private readonly TranslateTransform _titleShift = new TranslateTransform(0, 30);
        private readonly Border _details;
        private readonly ScaleTransform _detailsScale = new ScaleTransform(0.7, 0.7);

        public PopFrostScene(SplashGameInfo info, SplashImageSources sources)
            : base(info, sources, strip: false)
        {
            // 6. Cover, scaled around its center
            if (HasCover)
            {
                _frame = NewCoverFrame(out _cover);
                _frame.HorizontalAlignment = HorizontalAlignment.Center;
                _frame.Margin = new Thickness(0, FrameTop, 0, 0);
                _frame.RenderTransformOrigin = new Point(0.5, 0.5);
                _frame.RenderTransform = _coverScale;
                Stage.Children.Add(_frame);
            }

            // 7. Text block: the logo or name, then the details line, centered
            _block = new StackPanel
            {
                Width = TextWidth,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = HasCover ? VerticalAlignment.Top : VerticalAlignment.Center,
                Margin = new Thickness(0, HasCover ? FrameTop + FrameHeight + TextGap : 0, 0, 0)
            };
            _title = Item(Title, _titleShift, new Thickness(0), HorizontalAlignment.Center);
            _block.Children.Add(_title);
            TextBlock details = InfoLine(Info.ReleaseYear?.ToString(), Info.Genre, Info.Platform);
            if (details != null)
            {
                _details = Item(details, _detailsScale, new Thickness(0, 20, 0, 0), HorizontalAlignment.Center);
                _details.RenderTransformOrigin = new Point(0.5, 0.5);
                _block.Children.Add(_details);
            }

            Stage.Children.Add(_block);
        }

        private static double FrameTop => FrameCenter - FrameHeight / 2;

        protected override double CoverHeight => FrameHeight;

        protected override double LogoWidthShare => HasCover ? 0.15 : 0.24;

        protected override double LogoMaxHeight => HasCover ? 110 : 300;

        protected override void OnStageWidth(double width)
        {
            FitTitle(TextWidth, HasCover ? 44 : 72, HasCover ? 32 : 50, centered: true);
        }

        protected override void StartIntro()
        {
            if (_frame != null)
            {
                SpringFade(_frame, 1.05, 0.15);
                Spring(_coverScale, 0.6, 1.04, 1.0, 1.05, 0.15);
            }
        }

        protected override void PlaceGlass()
        {
            Rect text = Bounds(_block);
            double top = HasCover ? FrameTop - GlassPadding : text.Top - GlassPadding;
            double bottom = text.Bottom + GlassPadding;
            PlacePane(new Rect((StageWidth - PaneWidth) / 2, top, PaneWidth, bottom - top));
        }

        protected override void RevealText()
        {
            double title = HasCover ? 0.52 : 0.25;
            SpringFade(_title, 1.0, FromShow(title));
            Spring(_titleShift, TranslateTransform.YProperty, 30, -6, 0, 1.0, FromShow(title));
            if (_details != null)
            {
                SpringFade(_details, 0.8, FromShow(title + 0.2));
                Spring(_detailsScale, 0.7, 1.08, 1.0, 0.8, FromShow(title + 0.2));
            }
        }

        protected override void ShowCover(BitmapSource cover) => FadeInCover(_cover, cover);

        protected override void Close()
        {
            if (_frame != null)
            {
                // A small lift, then it shrinks away (650 ms).
                var scale = Keys(0, (0.195, 1.04, SineOut), (0.65, 0.82, CubicIn));
                _coverScale.BeginAnimation(ScaleTransform.ScaleXProperty, scale);
                _coverScale.BeginAnimation(ScaleTransform.ScaleYProperty, scale);
                _frame.BeginAnimation(UIElement.OpacityProperty, Keys(0, (0.195, 1, null), (0.65, 0, CubicIn)));
            }

            Animate(_block, UIElement.OpacityProperty, null, 0, 0.35);
        }
    }
}
