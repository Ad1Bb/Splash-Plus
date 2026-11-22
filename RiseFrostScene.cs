using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using static SplashAddonPlus.StyleKit;

namespace SplashAddonPlus
{
    /// <summary>
    /// Rise Frost: a strip of frosted glass runs along the bottom of the screen with the logo or name and two lines of
    /// details centered under it on the left; the cover rises in the bottom right, well above the strip, and hovers.
    /// </summary>
    internal sealed class RiseFrostScene : FrostScene
    {
        private const double FrameHeight = 600;
        private const double FrameRight = 170;
        private const double FrameBottom = 110;
        private const double ColumnLeft = 150;
        private const double ColumnGap = 100;
        private const double ColumnRight = 150;
        private const double ColumnBottom = 130;
        private const double StripAbove = 50;

        private readonly Grid _float;
        private readonly TranslateTransform _floatShift = new TranslateTransform();
        private readonly Grid _frame;
        private readonly Image _cover;
        private readonly TranslateTransform _frameShift = new TranslateTransform(0, 700);
        private readonly StackPanel _column;
        private readonly TranslateTransform _columnShift = new TranslateTransform();
        private readonly Border _title;
        private readonly Border _meta;
        private readonly Border _details;

        public RiseFrostScene(SplashGameInfo info, SplashImageSources sources)
            : base(info, sources, strip: true)
        {
            Grid area = SafeArea();

            // 6. Cover in a floating holder
            if (HasCover)
            {
                _frame = NewCoverFrame(out _cover);
                _frame.RenderTransform = _frameShift;
                _float = new Grid
                {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Margin = new Thickness(0, 0, FrameRight, FrameBottom),
                    RenderTransform = _floatShift,
                    Children = { _frame }
                };
                area.Children.Add(_float);
            }

            // 7. Column growing up from 130 u above the bottom: the logo or name, studio · year, genre · platform · play time;
            // as wide as its widest line, everything centered on one axis
            _column = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(ColumnLeft, 0, 0, ColumnBottom),
                RenderTransform = _columnShift
            };
            _title = Item(Title, new TranslateTransform(0, 80), new Thickness(0), HorizontalAlignment.Center);
            _column.Children.Add(_title);
            TextBlock meta = Line(SplashText.Join(Separator, Info.Developer, Info.ReleaseYear?.ToString()), 24, 400, 0.92);
            if (meta != null)
            {
                _meta = Item(meta, new TranslateTransform(0, 80), new Thickness(0, 16, 0, 0), HorizontalAlignment.Center);
                _column.Children.Add(_meta);
            }

            TextBlock details = InfoLine(Info.Genre, Info.Platform, SplashText.Playtime(Info.Playtime));
            if (details != null)
            {
                _details = Item(details, new TranslateTransform(0, 80), new Thickness(0, 18, 0, 0), HorizontalAlignment.Center);
                _column.Children.Add(_details);
            }

            area.Children.Add(_column);
        }

        protected override double CoverHeight => FrameHeight;

        protected override double LogoWidthShare => 0.15;

        protected override double LogoMaxHeight => 260;

        protected override void OnStageWidth(double width)
        {
            // Room for the cover and at least 640 u of text; below that (a narrow window) the cover side shrinks.
            double fit = HasCover ? FitWidth(SafeWidth, ColumnLeft + 640 + ColumnGap + CoverWidth + FrameRight, 0) : 1;
            double right = HasCover ? fit * (FrameRight + CoverWidth + ColumnGap) : ColumnRight;
            if (fit < 1)
            {
                _frame.LayoutTransform = new ScaleTransform(fit, fit);
                _float.Margin = new Thickness(0, 0, fit * FrameRight, FrameBottom);
                _column.Margin = new Thickness(fit * ColumnLeft, 0, 0, ColumnBottom);
            }

            _column.MaxWidth = Math.Max(1, SafeWidth - (fit < 1 ? fit * ColumnLeft : ColumnLeft) - right);
            FitTitle(_column.MaxWidth, 76, 52, centered: true);
        }

        protected override void StartIntro()
        {
            if (_frame == null)
            {
                return;
            }

            SpringFade(_frame, 1.3, 0.15);
            Spring(_frameShift, TranslateTransform.YProperty, 700, -14, 0, 1.3, 0.15);

            // Hovers 8 u up and down from 1.5 s on.
            var hover = new DoubleAnimation(0, -8, TimeSpan.FromSeconds(3.2))
            {
                EasingFunction = SineInOut,
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime = TimeSpan.FromSeconds(1.5)
            };
            _floatShift.BeginAnimation(TranslateTransform.YProperty, hover);
        }

        protected override void PlaceGlass()
        {
            PlaceStrip(Bounds(_column).Top - StripAbove);
        }

        protected override void RevealText()
        {
            Rise(_title, 0.45);
            Rise(_meta, 0.56);
            Rise(_details, _meta != null ? 0.67 : 0.56);
        }

        protected override void ShowCover(BitmapSource cover) => FadeInCover(_cover, cover);

        protected override void Close()
        {
            if (_float != null)
            {
                // The hover stops where it is; the cover sinks away.
                double at = _floatShift.Y;
                Animate(_floatShift, TranslateTransform.YProperty, at, at + 500, 0.65, 0, CubicIn);
                Animate(_float, UIElement.OpacityProperty, null, 0, 0.65, 0, CubicIn);
            }

            Animate(_columnShift, TranslateTransform.YProperty, 0, 60, 0.45, 0, CubicIn);
            Animate(_column, UIElement.OpacityProperty, null, 0, 0.45, 0, CubicIn);
        }

        private void Rise(Border item, double at)
        {
            if (item == null)
            {
                return;
            }

            SpringFade(item, 1.0, FromShow(at));
            Spring(item.RenderTransform, TranslateTransform.YProperty, 80, -8, 0, 1.0, FromShow(at));
        }
    }
}
