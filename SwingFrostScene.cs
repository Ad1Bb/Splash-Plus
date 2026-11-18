using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static SplashAddonPlus.StyleKit;

namespace SplashAddonPlus
{
    /// <summary>
    /// Swing Frost: the cover hangs on the left like a picture on a nail and swings in; the logo or name, a short
    /// dash and two lines of details stand on the right. Both share one wide pane of frosted glass.
    /// </summary>
    internal sealed class SwingFrostScene : FrostScene
    {
        private const double FrameHeight = 630;
        private const double FrameLeft = 250;
        private const double ColumnGap = 110;
        private const double ColumnWidth = 640;
        private const double CenteredWidth = 1000;

        private readonly Grid _frame;
        private readonly Image _cover;
        private readonly RotateTransform _swing = new RotateTransform(-10);
        private readonly TranslateTransform _drop = new TranslateTransform(0, -60);
        private readonly StackPanel _column;
        private readonly List<Border> _items = new List<Border>();

        public SwingFrostScene(SplashGameInfo info, SplashImageSources sources)
            : base(info, sources, strip: false)
        {
            Grid area = SafeArea();
            const HorizontalAlignment align = HorizontalAlignment.Center;

            // 6. Cover, turning around the middle of its top edge
            if (HasCover)
            {
                _frame = NewCoverFrame(out _cover);
                _frame.Margin = new Thickness(FrameLeft, FrameTop, 0, 0);
                _frame.RenderTransformOrigin = new Point(0.5, 0);
                _frame.RenderTransform = new TransformGroup { Children = { _swing, _drop } };
                area.Children.Add(_frame);
            }

            // 7. Column: the logo or name, the dash, studio · year, genre · play time
            _column = new StackPanel
            {
                Width = HasCover ? ColumnWidth : CenteredWidth,
                HorizontalAlignment = HasCover ? HorizontalAlignment.Left : HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(HasCover ? ColumnLeft : 0, 0, 0, 0)
            };
            AddItem(Title, 0, align);
            var dash = new Rectangle { Width = 90, Height = 3, RadiusX = 1.5, RadiusY = 1.5, Fill = White(0.75) };
            AddItem(dash, 26, align);
            TextBlock first = Line(SplashText.Join(Separator, Info.Developer, Info.ReleaseYear?.ToString()), 24, 400, 0.94);
            TextBlock second = Line(SplashText.Join(Separator, Info.Genre, SplashText.Playtime(Info.Playtime)), 20, 400, 0.75);
            if (first != null)
            {
                AddItem(first, 22, align);
            }

            if (second != null)
            {
                AddItem(second, first != null ? 4 : 22, align);
            }

            area.Children.Add(_column);
        }

        private static double FrameTop => (StageHeight - FrameHeight) / 2;

        private double ColumnLeft => FrameLeft + CoverWidth + ColumnGap;

        protected override double CoverHeight => FrameHeight;

        protected override double LogoWidthShare => 0.25;

        protected override double LogoMaxHeight => 240;

        protected override void OnStageWidth(double width)
        {
            FitTitle(_column.Width, 68, 50, centered: true);
        }

        protected override void StartIntro()
        {
            if (_frame == null)
            {
                return;
            }

            // Swings in: falls into place by the half, then +3°, −1°, 0° (1.5 s).
            const double delay = 0.15;
            _frame.BeginAnimation(UIElement.OpacityProperty, Keys(delay, (0, 0, null), (0.75, 1, CubicOut)));
            _drop.BeginAnimation(TranslateTransform.YProperty, Keys(delay, (0, -60, null), (0.75, 0, CubicOut)));
            _swing.BeginAnimation(RotateTransform.AngleProperty, Keys(delay, (0, -10, null), (0.75, 3, CubicOut), (1.17, -1, SineInOut), (1.5, 0, SineInOut)));
        }

        protected override void PlaceGlass()
        {
            Rect column = Bounds(_column);
            Rect pane;
            if (HasCover)
            {
                double left = SafeLeft + FrameLeft - GlassPadding;
                double right = column.Right + GlassPadding;
                double top = Math.Min(FrameTop, column.Top) - GlassPadding;
                double bottom = Math.Max(FrameTop + FrameHeight, column.Bottom) + GlassPadding;
                pane = new Rect(left, top, right - left, bottom - top);
            }
            else
            {
                pane = column;
                pane.Inflate(GlassPadding, GlassPadding);
            }

            PlacePane(pane);
        }

        protected override void RevealText()
        {
            // The title at 650 ms, then the rest 110 ms later and 100 ms apart.
            for (int i = 0; i < _items.Count; i++)
            {
                double delay = FromShow(i == 0 ? 0.65 : 0.76 + (i - 1) * 0.1);
                SpringFade(_items[i], 1.0, delay);
                Spring(_items[i].RenderTransform, TranslateTransform.XProperty, 70, -8, 0, 1.0, delay);
            }
        }

        protected override void ShowCover(BitmapSource cover) => FadeInCover(_cover, cover);

        protected override void Close()
        {
            if (_frame != null)
            {
                // Tips the other way, then falls off its nail (700 ms).
                _swing.BeginAnimation(RotateTransform.AngleProperty, Keys(0, (0.21, 4, SineOut), (0.7, -8, CubicIn)));
                _drop.BeginAnimation(TranslateTransform.YProperty, Keys(0, (0.21, 0, null), (0.7, 50, CubicIn)));
                _frame.BeginAnimation(UIElement.OpacityProperty, Keys(0, (0.21, 1, null), (0.7, 0, CubicIn)));
            }

            Animate(_column, UIElement.OpacityProperty, null, 0, 0.4);
        }

        private void AddItem(UIElement content, double gap, HorizontalAlignment align)
        {
            Border item = Item(content, new TranslateTransform(70, 0), new Thickness(0, _items.Count == 0 ? 0 : gap, 0, 0), align);
            _items.Add(item);
            _column.Children.Add(item);
        }
    }
}
