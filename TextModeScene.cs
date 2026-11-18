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
    /// Text Mode: a retro text-mode setup screen. The cover is blurred into soft patches of colour under a coarse
    /// dark checkerboard; bars at the top and bottom, and a dialog with a double frame and a hard shadow holding
    /// the cover, a list of the game's details, a bar with a sliding block and "Please wait".
    /// Everything moves in steps. Two palettes: light grey panels with black text (Text Mode) and black panels
    /// with light grey text (Text Mode Dark); layout and timing are the same.
    /// </summary>
    internal sealed class TextModeScene : StyledScene
    {
        /// <summary>Colours of one Text Mode variant (the dialog's shadow is the same in both).</summary>
        internal sealed class Palette
        {
            public static readonly Palette Light = new Palette(Gray(0xC0), Brushes.Black, Gray(0x80), Color.FromArgb(64, 0, 0, 0));
            public static readonly Palette Dark = new Palette(Brushes.Black, Gray(0xC8), Gray(0x1E), Color.FromArgb(56, 255, 255, 255));

            private Palette(Brush panel, Brush ink, Brush placeholder, Color stripe)
            {
                Panel = panel;
                Ink = ink;
                Placeholder = placeholder;
                Stripe = stripe;
            }

            /// <summary>Bars, dialog and the title's background.</summary>
            public Brush Panel { get; }

            /// <summary>All text and lines: frames, borders, the block and the cursor.</summary>
            public Brush Ink { get; }

            /// <summary>The cover's frame until the image arrives.</summary>
            public Brush Placeholder { get; }

            /// <summary>The thin stripes inside the bar.</summary>
            public Color Stripe { get; }
        }

        private const double DialogHeight = 520;
        private const double ShadowOffset = 20;
        private const double Inset = 48;
        private const double CoverWidth = 240;
        private const double CoverHeight = 360;
        private const double CoverGap = 44;
        private const double BarHeight = 34;
        private const double FontSize = 24;

        private readonly Palette _palette;
        private readonly double _dialogWidth;
        private readonly double _barInterior;
        private readonly Grid _blobs = new Grid { Opacity = 0 };
        private readonly Image _blobImage = new Image { Stretch = Stretch.UniformToFill };
        private readonly DrawingBrush _checker;
        private readonly DrawingBrush _stripes;
        private readonly Grid _topBar;
        private readonly Grid _bottomBar;
        private readonly Grid _dialog;
        private readonly RectangleGeometry _dialogClip = new RectangleGeometry(Rect.Empty);
        private readonly Image _cover;
        private readonly RectangleGeometry _coverClip;
        private readonly List<FrameworkElement> _items = new List<FrameworkElement>();
        private readonly Rectangle _block;
        private readonly TranslateTransform _blockShift = new TranslateTransform();
        private readonly Rectangle _cursor;

        public TextModeScene(SplashGameInfo info, SplashImageSources sources, Palette palette)
            : base(info, sources)
        {
            _palette = palette ?? Palette.Light;
            _dialogWidth = HasCover ? 1120 : 860;

            // 1–2. Base and the patches of colour
            Stage.Children.Add(new Rectangle { Fill = Gray(0x20) });
            RenderOptions.SetBitmapScalingMode(_blobImage, BitmapScalingMode.HighQuality);
            _blobs.Children.Add(_blobImage);
            _blobs.Children.Add(Dim(0.35));
            Stage.Children.Add(_blobs);

            // 3. Checkerboard of dark "pixels"
            _checker = Tiles(16,
                (new Rect(0, 0, 8, 8), Color.FromArgb(128, 0, 0, 0)),
                (new Rect(8, 8, 8, 8), Color.FromArgb(128, 0, 0, 0)),
                (new Rect(8, 0, 8, 8), Color.FromArgb(10, 255, 255, 255)),
                (new Rect(0, 8, 8, 8), Color.FromArgb(10, 255, 255, 255)));
            var checker = new Rectangle { Fill = _checker };
            RenderOptions.SetEdgeMode(checker, EdgeMode.Aliased);
            Stage.Children.Add(checker);

            // 4–5. Bars
            _topBar = Bar(Info.Developer, Info.ReleaseYear?.ToString(), VerticalAlignment.Top);
            _bottomBar = Bar(Loc.Starting, Info.Platform, VerticalAlignment.Bottom);
            Stage.Children.Add(_topBar);
            Stage.Children.Add(_bottomBar);

            // 6. Dialog with its hard shadow, revealed from the top
            var window = new Grid { Width = _dialogWidth, Height = DialogHeight, Background = _palette.Panel, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            window.Children.Add(new Border { Margin = new Thickness(12), BorderBrush = _palette.Ink, BorderThickness = new Thickness(2) });
            window.Children.Add(new Border { Margin = new Thickness(16), BorderBrush = _palette.Ink, BorderThickness = new Thickness(2) });

            TextBlock title = Text(Info.Name, FontSize, 700, _palette.Ink, 30.0 / FontSize, 1, Consolas);
            title.Background = _palette.Panel;
            title.Padding = new Thickness(14, 0, 14, 0);
            title.HorizontalAlignment = HorizontalAlignment.Center;
            title.VerticalAlignment = VerticalAlignment.Top;
            title.Margin = new Thickness(0, 2, 0, 0);
            title.MaxWidth = 0.8 * _dialogWidth;
            window.Children.Add(title);

            var inner = new Grid { Margin = new Thickness(Inset) };
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            if (HasCover)
            {
                _cover = CoverImage();
                RenderOptions.SetBitmapScalingMode(_cover, BitmapScalingMode.HighQuality);
                _coverClip = new RectangleGeometry(new Rect(0, 0, CoverWidth - 4, 0));
                inner.Children.Add(new Border
                {
                    Width = CoverWidth,
                    Height = CoverHeight,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 0, CoverGap, 0),
                    BorderBrush = _palette.Ink,
                    BorderThickness = new Thickness(2),
                    Background = _palette.Placeholder,
                    Child = new Grid { ClipToBounds = true, Clip = _coverClip, Children = { _cover } }
                });
            }

            double columnWidth = _dialogWidth - 2 * Inset - (HasCover ? CoverWidth + CoverGap : 0);
            var column = new DockPanel { LastChildFill = false };
            Grid.SetColumn(column, 1);

            // At the bottom: the decorative bar and "Please wait"
            _barInterior = columnWidth - 4;
            double blockWidth = 0.22 * _barInterior;
            _stripes = Tiles(6, (new Rect(0, 0, 2, 6), _palette.Stripe));
            _block = new Rectangle { Width = blockWidth, Fill = _palette.Ink, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 3, 0, 3), RenderTransform = _blockShift };
            var bar = new Border
            {
                Height = BarHeight,
                BorderBrush = _palette.Ink,
                BorderThickness = new Thickness(2),
                Child = new Grid { Background = _stripes, Children = { _block } }
            };
            RenderOptions.SetEdgeMode(bar, EdgeMode.Aliased);

            var wait = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 14, 0, 0) };
            TextBlock waitText = Text(Loc.PleaseWait, FontSize, 400, _palette.Ink, 1.2, 1, Consolas);
            _cursor = new Rectangle
            {
                Width = 0.6 * FontSize,
                Height = 4,
                Fill = _palette.Ink,
                VerticalAlignment = VerticalAlignment.Top,

                // On the baseline: Consolas's ascent is 0.743 of the line box set by Text().
                Margin = new Thickness(6, 0.743 * waitText.LineHeight - 4, 0, 0)
            };
            wait.Children.Add(waitText);
            wait.Children.Add(_cursor);

            var bottom = new StackPanel { Children = { bar, wait } };
            DockPanel.SetDock(bottom, Dock.Bottom);
            column.Children.Add(bottom);

            // At the top: the details (values only), empty ones left out
            var rows = new StackPanel();
            DockPanel.SetDock(rows, Dock.Top);
            foreach (string line in new[]
            {
                Info.Platform,
                Info.ReleaseYear?.ToString(),
                Info.Developer,
                Info.Genre,
                SplashText.Playtime(Info.Playtime),
                Info.Library
            })
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    TextBlock row = Text(line, FontSize, 400, _palette.Ink, 36.0 / FontSize, 1, Consolas);
                    row.Opacity = 0;
                    rows.Children.Add(row);
                    _items.Add(row);
                }
            }

            column.Children.Add(rows);
            bar.Opacity = 0;
            wait.Opacity = 0;
            _items.Add(bar);
            _items.Add(wait);
            inner.Children.Add(column);
            window.Children.Add(inner);

            _dialog = new Grid
            {
                Width = _dialogWidth + ShadowOffset,
                Height = DialogHeight + ShadowOffset,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Clip = _dialogClip,
                Children =
                {
                    new Rectangle
                    {
                        Width = _dialogWidth,
                        Height = DialogHeight,
                        Fill = new SolidColorBrush(Color.FromArgb(179, 0, 0, 0)),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(ShadowOffset, ShadowOffset, 0, 0)
                    },
                    window
                }
            };
            SafeArea().Children.Add(_dialog);
        }

        protected override void OnStageWidth(double width)
        {
            double cell = SnapToPixels(8);
            _checker.Viewport = new Rect(0, 0, 2 * cell, 2 * cell);
            double stripe = SnapToPixels(2);
            _stripes.Viewport = new Rect(0, 0, 3 * stripe, 3 * stripe);
        }

        protected override void Start()
        {
            CutTo(_topBar, 1, 0.15);
            CutTo(_bottomBar, 1, 0.15);

            // The dialog (with its shadow) appears from the top in 8 steps, 40 ms apart.
            double fullHeight = DialogHeight + ShadowOffset;
            var reveal = new RectAnimationUsingKeyFrames();
            for (int k = 1; k <= 8; k++)
            {
                reveal.KeyFrames.Add(new DiscreteRectKeyFrame(new Rect(0, 0, _dialogWidth + ShadowOffset, fullHeight * k / 8),
                    KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.3 + 0.04 * k))));
            }

            _dialogClip.BeginAnimation(RectangleGeometry.RectProperty, reveal);

            for (int i = 0; i < _items.Count; i++)
            {
                CutTo(_items[i], 1, 0.7 + i * 0.08);
            }

            // The block slides across the bar and back in 14 steps each way (decoration, not progress).
            double from = 3;
            double to = Math.Max(from + 1, _barInterior - 3 - _block.Width);

            var slide = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromSeconds(1.8), AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever };
            for (int k = 0; k <= 14; k++)
            {
                slide.KeyFrames.Add(new DiscreteDoubleKeyFrame(from + (to - from) * k / 14, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.8 * k / 14))));
            }

            _blockShift.BeginAnimation(TranslateTransform.XProperty, slide);

            var blink = Steps((0, 1), (0.45, 0));
            blink.Duration = TimeSpan.FromSeconds(0.9);
            blink.RepeatBehavior = RepeatBehavior.Forever;
            _cursor.BeginAnimation(UIElement.OpacityProperty, blink);
        }

        protected override void Close()
        {
            CutTo(_dialog, 0, 0.12);
            CutTo(_topBar, 0, 0.32);
            CutTo(_bottomBar, 0, 0.32);
        }

        public override void LoadImagesAsync(SplashImageSources sources, MonitorArea screen)
        {
            ScreenPixels(screen, out int _, out int height);
            bool hasCover = HasCover;
            LoadInBackground(() => new Loaded
            {
                Blobs = ImageLoader.Load(Sources.Cover, 24, 0, cover: false) ?? ImageLoader.Load(Sources.Background, 24, 0, cover: false),

                // The cover itself in its original quality, decoded for the frame's size on screen.
                Cover = hasCover ? ImageLoader.Load(Sources.Cover, UnitsToPixels(CoverWidth, height), UnitsToPixels(CoverHeight, height), cover: true) : null
            }, loaded =>
            {
                loaded = loaded ?? new Loaded();
                if (loaded.Blobs != null)
                {
                    _blobImage.Source = RenderBlobs(loaded.Blobs);
                    _blobs.BeginAnimation(UIElement.OpacityProperty, Steps((0.125, 0.25), (0.25, 0.5), (0.375, 0.75), (0.5, 1)));
                }

                if (_cover != null && loaded.Cover != null)
                {
                    _cover.Source = loaded.Cover;
                    var reveal = new RectAnimationUsingKeyFrames();
                    for (int k = 1; k <= 6; k++)
                    {
                        reveal.KeyFrames.Add(new DiscreteRectKeyFrame(new Rect(0, 0, CoverWidth - 4, (CoverHeight - 4) * k / 6),
                            KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.08 * k))));
                    }

                    _coverClip.BeginAnimation(RectangleGeometry.RectProperty, reveal);
                }
            });
        }

        /// <summary>
        /// The tiny image stretched over a 16:9 canvas at 1.5× and blurred into unrecognisable patches of colour,
        /// rendered once into a small bitmap.
        /// </summary>
        private static BitmapSource RenderBlobs(BitmapSource tiny)
        {
            const int width = 192;
            const int height = 108;
            var image = new Image
            {
                Source = tiny,
                Stretch = Stretch.UniformToFill,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(1.5, 1.5)
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
            var canvas = new Grid
            {
                Width = width,
                Height = height,
                ClipToBounds = true,
                Effect = new BlurEffect { Radius = 60.0 * height / StageHeight, KernelType = KernelType.Gaussian },
                Children = { image }
            };
            canvas.Measure(new Size(width, height));
            canvas.Arrange(new Rect(0, 0, width, height));
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(canvas);
            bitmap.Freeze();
            return bitmap;
        }

        /// <summary>A light grey bar with text at both ends (an empty end stays empty).</summary>
        private Grid Bar(string left, string right, VerticalAlignment edge)
        {
            var bar = new Grid { Height = 40, VerticalAlignment = edge, Background = _palette.Panel, Opacity = 0 };
            if (!string.IsNullOrWhiteSpace(left))
            {
                TextBlock text = Text(left, FontSize, 400, _palette.Ink, 1.2, 1, Consolas);
                text.HorizontalAlignment = HorizontalAlignment.Left;
                text.VerticalAlignment = VerticalAlignment.Center;
                text.Margin = new Thickness(20, 0, 20, 0);
                bar.Children.Add(text);
            }

            if (!string.IsNullOrWhiteSpace(right))
            {
                TextBlock text = Text(right, FontSize, 400, _palette.Ink, 1.2, 1, Consolas);
                text.HorizontalAlignment = HorizontalAlignment.Right;
                text.VerticalAlignment = VerticalAlignment.Center;
                text.Margin = new Thickness(20, 0, 20, 0);
                bar.Children.Add(text);
            }

            return bar;
        }

        private sealed class Loaded
        {
            public BitmapSource Blobs { get; set; }
            public BitmapSource Cover { get; set; }
        }
    }
}
