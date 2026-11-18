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
    /// Meet Frost: the cover slides in from the left and a block with the logo or name and the game's details
    /// (values only, in two columns) from the right; they meet on one pane of frosted glass, the text straight on it.
    /// </summary>
    internal sealed class MeetFrostScene : FrostScene
    {
        private const double FrameHeight = 630;
        private const double Gap = 28;
        private const double BlockWidth = 520;
        private const double BlockHeight = 560;
        private const double BlockTop = 35;
        private const double Inset = 48;
        private const double ColumnGap = 24;
        private const double RowGap = 18;

        private readonly Grid _group;
        private readonly Grid _frame;
        private readonly Image _cover;
        private readonly TranslateTransform _coverShift = new TranslateTransform();
        private readonly Border _block;
        private readonly TranslateTransform _blockShift = new TranslateTransform();
        private readonly Border _title;
        private readonly List<Border> _details = new List<Border>();
        private readonly Rectangle _rule;

        public MeetFrostScene(SplashGameInfo info, SplashImageSources sources)
            : base(info, sources, strip: false)
        {
            // The group (cover, gap, block) stands in the middle of the screen.
            _group = new Grid
            {
                Width = GroupWidth,
                Height = HasCover ? FrameHeight : BlockHeight,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // 6. Cover
            if (HasCover)
            {
                _frame = NewCoverFrame(out _cover);
                _frame.RenderTransform = _coverShift;
                _group.Children.Add(_frame);
            }

            // 7. Details block, without a card of its own: the text stands on the glass.
            var content = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            _title = Item(Title, new TranslateTransform(0, 16), new Thickness(0), HorizontalAlignment.Center);
            content.Children.Add(_title);

            var cells = new List<TextBlock>();
            foreach (string value in new[] { Info.Developer, Info.ReleaseYear?.ToString(), Info.Genre, Info.Platform, SplashText.Playtime(Info.Playtime), Info.Library })
            {
                TextBlock cell = Line(value, 22, 400, 1.0, 1.25, 2);
                if (cell != null)
                {
                    cell.TextAlignment = TextAlignment.Center;
                    cell.HorizontalAlignment = HorizontalAlignment.Center;
                    cells.Add(cell);
                }
            }

            if (cells.Count > 0)
            {
                _rule = new Rectangle { Height = 1, Fill = White(0.22) };
                AddDetail(content, _rule, new Thickness(0, 26, 0, 22), HorizontalAlignment.Stretch);

                // Two columns, filled in order with the values there are, each centered in its cell.
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ColumnGap) });
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                for (int i = 0; i < cells.Count; i++)
                {
                    if (i % 2 == 0)
                    {
                        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    }

                    Border cell = Item(cells[i], new TranslateTransform(0, 16), new Thickness(0, i >= 2 ? RowGap : 0, 0, 0), HorizontalAlignment.Stretch);
                    Grid.SetRow(cell, i / 2);
                    Grid.SetColumn(cell, i % 2 * 2);
                    grid.Children.Add(cell);
                    _details.Add(cell);
                }

                content.Children.Add(grid);
            }

            _block = new Border
            {
                Width = BlockWidth,
                Height = BlockHeight,
                Padding = new Thickness(Inset),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = HasCover ? new Thickness(CoverWidth + Gap, BlockTop, 0, 0) : new Thickness(0),
                Opacity = 0,
                RenderTransform = _blockShift,
                Child = content
            };
            _group.Children.Add(_block);
            SafeArea().Children.Add(_group);
        }

        private double GroupWidth => HasCover ? CoverWidth + Gap + BlockWidth : BlockWidth;

        protected override double CoverHeight => FrameHeight;

        protected override double LogoWidthShare => 0.15;

        protected override double LogoMaxHeight => 150;

        protected override void OnStageWidth(double width)
        {
            FitTitle(BlockWidth - 2 * Inset, 44, 32, centered: true);
            if (_rule != null)
            {
                _rule.Height = OnePixel;
            }
        }

        protected override void StartIntro()
        {
            // Both sides come from 60 % of the screen width away (without a cover the block rises from below).
            double distance = 0.6 * StageWidth;
            if (_frame != null)
            {
                SpringFade(_frame, 1.15, 0.15);
                Spring(_coverShift, TranslateTransform.XProperty, -distance, 14, 0, 1.15, 0.15);
                SpringFade(_block, 1.15, 0.15);
                Spring(_blockShift, TranslateTransform.XProperty, distance, -14, 0, 1.15, 0.15);
            }
            else
            {
                SpringFade(_block, 1.1, 0.15);
                Spring(_blockShift, TranslateTransform.YProperty, 60, -8, 0, 1.1, 0.15);
            }
        }

        protected override void PlaceGlass()
        {
            double width = GroupWidth + 2 * GlassPadding;
            double height = (HasCover ? FrameHeight : BlockHeight) + 2 * GlassPadding;
            var pane = new Rect((StageWidth - width) / 2, (StageHeight - height) / 2, width, height);
            PlacePane(pane);
        }

        protected override void RevealText()
        {
            SpringFade(_title, 1.0, FromShow(0.9));
            Spring(_title.RenderTransform, TranslateTransform.YProperty, 16, -3, 0, 1.0, FromShow(0.9));
            for (int i = 0; i < _details.Count; i++)
            {
                double delay = FromShow(1.0 + i * 0.09);
                SpringFade(_details[i], 0.8, delay);
                Spring(_details[i].RenderTransform, TranslateTransform.YProperty, 16, -3, 0, 0.8, delay);
            }
        }

        protected override void ShowCover(BitmapSource cover) => FadeInCover(_cover, cover);

        protected override void Close()
        {
            if (_frame != null)
            {
                Animate(_coverShift, TranslateTransform.XProperty, null, -70, 0.6, 0, CubicIn);
                Animate(_frame, UIElement.OpacityProperty, null, 0, 0.6, 0, CubicIn);
                Animate(_blockShift, TranslateTransform.XProperty, null, 70, 0.6, 0, CubicIn);
            }

            Animate(_block, UIElement.OpacityProperty, null, 0, 0.6, 0, CubicIn);
        }

        private void AddDetail(Panel panel, FrameworkElement element, Thickness margin, HorizontalAlignment align)
        {
            Border item = Item(element, new TranslateTransform(0, 16), margin, align);
            panel.Children.Add(item);
            _details.Add(item);
        }
    }
}
