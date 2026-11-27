using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace SplashAddonPlus
{
    /// <summary>
    /// Settings page built in code (no XAML), so the project builds without the WPF markup compiler.
    /// DataContext is the <see cref="SplashAddonSettings"/> instance.
    /// </summary>
    public class SplashAddonSettingsView : UserControl
    {
        private static readonly IValueConverter Inverse = new BoolConverter(false, true);
        private static readonly IValueConverter DimWhenDisabled = new BoolConverter(1.0, 0.5);
        private static readonly IValueConverter IntToDouble = new IntDoubleConverter();
        private static readonly IValueConverter StyleDescription = new StyleConverter(DescribeStyle);

        private static string DescribeStyle(SplashStyle style)
        {
            switch (style)
            {
                case SplashStyle.Cinema: return Loc.StyleCinemaHint;
                case SplashStyle.Showcase: return Loc.StyleShowcaseHint;
                case SplashStyle.Gallery: return Loc.StyleGalleryHint;
                case SplashStyle.Plinth: return Loc.StylePlinthHint;
                case SplashStyle.Drawer: return Loc.StyleDrawerHint;
                case SplashStyle.Band: return Loc.StyleBandHint;
                case SplashStyle.TextMode: return Loc.StyleTextModeHint;
                case SplashStyle.TextModeDark: return Loc.StyleTextModeDarkHint;
                case SplashStyle.PopFrost: return Loc.StylePopFrostHint;
                case SplashStyle.SwingFrost: return Loc.StyleSwingFrostHint;
                case SplashStyle.MeetFrost: return Loc.StyleMeetFrostHint;
                case SplashStyle.RiseFrost: return Loc.StyleRiseFrostHint;
                case SplashStyle.LootDrop: return Loc.StyleLootDropHint;
                case SplashStyle.LockOn: return Loc.StyleLockOnHint;
                case SplashStyle.Showdown: return Loc.StyleShowdownHint;
                case SplashStyle.Focus: return Loc.StyleFocusHint;
                case SplashStyle.Slant: return Loc.StyleSlantHint;
                case SplashStyle.IrisGloss: return Loc.StyleIrisGlossHint;
                case SplashStyle.SlateGloss: return Loc.StyleSlateGlossHint;
                case SplashStyle.Random: return Loc.StyleRandomHint;
                default: return Loc.StyleClassicHint;
            }
        }

        /// <summary>
        /// One page, most used first: the look, when the splash closes, games with their own settings; everything
        /// rarely changed sits in a collapsed Advanced section.
        /// </summary>
        /// <param name="resolveGameName">Name of a game in the library for a game ID, or null.</param>
        /// <param name="preview">Shows the splash screen with the current settings, or null when it cannot.</param>
        public SplashAddonSettingsView(IList<string> platforms, Func<string, string> resolveGameName, Action preview)
        {
            var root = new StackPanel { Margin = new Thickness(20) };

            // ---------- Look ----------
            root.Children.Add(SectionHeader(Loc.SectionAppearance, first: true));
            var styleRow = new StackPanel { Orientation = Orientation.Horizontal };
            styleRow.Children.Add(Choice(Loc.StyleLabel, "Style", StyleChoices(withDefault: false)));
            var previewButton = new Button
            {
                Content = Loc.Preview,
                ToolTip = Loc.PreviewTooltip,
                IsEnabled = preview != null,
                VerticalAlignment = VerticalAlignment.Bottom,
                Padding = new Thickness(14, 3, 14, 3),
                Margin = new Thickness(10, 0, 0, 10)
            };
            previewButton.Click += (s, e) => preview?.Invoke();
            styleRow.Children.Add(previewButton);
            root.Children.Add(styleRow);

            TextBlock styleHint = Hint(string.Empty);
            styleHint.Margin = new Thickness(0, -5, 0, 10);
            styleHint.SetBinding(TextBlock.TextProperty, new Binding("Style") { Converter = StyleDescription });
            root.Children.Add(styleHint);

            // The look options, collapsed (most people never change them); shown only when Custom, the one style that
            // uses them, is the style or some game's own style.
            var options = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
            options.Children.Add(Choice(Loc.LogoPositionLabel, "LogoPosition",
                Option(LogoPlacement.BottomLeft, Loc.LogoBottomLeft),
                Option(LogoPlacement.BottomCenter, Loc.LogoBottomCenter),
                Option(LogoPlacement.BottomRight, Loc.LogoBottomRight),
                Option(LogoPlacement.Center, Loc.LogoCenter),
                Option(LogoPlacement.TopLeft, Loc.LogoTopLeft),
                Option(LogoPlacement.TopRight, Loc.LogoTopRight)));
            options.Children.Add(PercentSlider(Loc.LogoWidth, "LogoWidthPercent", Loc.LogoWidthHint));
            options.Children.Add(PercentSlider(Loc.BackgroundDim, "BackgroundDimPercent", null));
            options.Children.Add(Toggle(Loc.VignetteToggle, "BackgroundVignette"));
            options.Children.Add(Toggle(Loc.ZoomToggle, "BackgroundZoom"));
            var customize = new Expander
            {
                Header = new TextBlock { Text = Loc.CustomizeStyle, FontWeight = FontWeights.Bold },
                Content = options
            };
            customize.SetBinding(VisibilityProperty,
                new Binding("ShowCustomOptions") { Converter = new BoolConverter(Visibility.Visible, Visibility.Collapsed) });
            root.Children.Add(customize);

            // ---------- When the splash screen closes ----------
            root.Children.Add(Divider());
            root.Children.Add(SectionHeader(Loc.SectionClosing));
            string group = "CloseMode" + Guid.NewGuid().ToString("N");
            root.Children.Add(ModeOption(Loc.CloseWhenAppears, CloseMode.WhenGameAppears, group));
            root.Children.Add(ModeOption(Loc.CloseFixed, CloseMode.FixedTime, group));
            var fixedTime = (FrameworkElement)NumberField(Loc.FixedTimeSeconds, "SplashScreenDuration", null);
            fixedTime.Margin = new Thickness(24, 4, 0, 0);
            BindEnabled(fixedTime, "AutoDetectFullscreen", inverse: true);
            root.Children.Add(fixedTime);
            root.Children.Add(Hint(Loc.ManualCloseHint, emphasize: true));

            // ---------- Games with their own settings ----------
            root.Children.Add(Divider());
            root.Children.Add(SectionHeader(Loc.SectionGames));
            root.Children.Add(Hint(Loc.ExceptionsTip));
            root.Children.Add(new GameExceptionsEditor(resolveGameName));

            // ---------- Advanced (collapsed) ----------
            var advanced = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };

            // Desktop mode only; the hint says plainly that Fullscreen mode is not affected.
            advanced.Children.Add(Choice(Loc.DesktopSplashSize, "DesktopSplashSize",
                Option(DesktopSplashSize.FullScreen, Loc.SizeFullScreen),
                Option(DesktopSplashSize.Large, Loc.SizeLarge),
                Option(DesktopSplashSize.Small, Loc.SizeSmall),
                Option(DesktopSplashSize.PlayniteWindow, Loc.SizePlayniteWindow)));
            TextBlock sizeHint = Hint(Loc.DesktopSplashSizeHint);
            sizeHint.Margin = new Thickness(0, -5, 0, 10);
            advanced.Children.Add(sizeHint);

            advanced.Children.Add(Label(Loc.SectionAutoDetect));
            advanced.Children.Add(NumberField(Loc.MaximumWait, "MaximumWaitSeconds", Loc.MaximumWaitHint));
            var detection = new StackPanel();
            BindEnabled(detection, "AutoDetectFullscreen", inverse: false);
            detection.Children.Add(NumberField(Loc.MinimumDisplay, "MinimumDisplaySeconds", Loc.MinimumDisplayHint));
            detection.Children.Add(NumberField(Loc.DetectionDelay, "DetectionDelaySeconds", Loc.DetectionDelayHint));
            detection.Children.Add(Toggle(Loc.WindowedToggle, "DetectWindowedGames"));
            detection.Children.Add(Hint(Loc.WindowedHint));
            detection.Children.Add(Label(Loc.IgnoredProcesses));
            detection.Children.Add(Hint(Loc.IgnoredProcessesHint));
            detection.Children.Add(MultilineBox("IgnoredProcessNamesText", 70));
            advanced.Children.Add(detection);

            var platformTimes = new StackPanel();
            BindEnabled(platformTimes, "AutoDetectFullscreen", inverse: true);
            platformTimes.Children.Add(Label(Loc.SectionPlatformTimes));
            platformTimes.Children.Add(Toggle(Loc.PlatformTimersToggle, "UsePlatformSpecificTimers"));
            var platformEditor = new PlatformListEditor(platforms);
            BindEnabled(platformEditor, "UsePlatformSpecificTimers", inverse: false);
            platformTimes.Children.Add(platformEditor);
            platformTimes.Children.Add(Hint(Loc.PriorityHint));
            advanced.Children.Add(platformTimes);

            advanced.Children.Add(Label(Loc.SectionDiagnostics));
            advanced.Children.Add(Toggle(Loc.DiagnosticsToggle, "EnableDiagnosticLogging"));
            advanced.Children.Add(Hint(Loc.DiagnosticsHint));

            root.Children.Add(Divider());
            root.Children.Add(new Expander
            {
                Header = new TextBlock { Text = Loc.SectionAdvanced, FontSize = 18, FontWeight = FontWeights.SemiBold },
                Margin = new Thickness(0, 15, 0, 0),
                Content = advanced
            });

            Content = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = root
            };
        }

        /// <summary>One option of the "when to close" choice.</summary>
        private static RadioButton ModeOption(string text, CloseMode mode, string group)
        {
            var option = new RadioButton
            {
                Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap },
                GroupName = group,
                Margin = new Thickness(0, 0, 0, 6)
            };
            option.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty,
                new Binding("CloseMode") { Converter = new ModeConverter(mode), UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            return option;
        }

        private static KeyValuePair<object, string> Option(object value, string label) => new KeyValuePair<object, string>(value, label);

        /// <summary>
        /// The style list: Custom and Random on top, a line, then the ready-made styles A–Z. With
        /// <paramref name="withDefault"/> a "Default" entry (key null: the style from the settings) comes first.
        /// </summary>
        private static List<object> StyleChoices(bool withDefault)
        {
            var items = new List<object>();
            if (withDefault)
            {
                items.Add(Option(null, Loc.GameDefault));
            }

            items.AddRange(StyleList.Special.Select(style => (object)Option(style, StyleList.Name(style))));
            items.Add(new ComboBoxItem
            {
                Content = new Border
                {
                    Height = 1,
                    Background = Brushes.Gray,
                    Opacity = 0.7,
                    SnapsToDevicePixels = true,
                    Margin = new Thickness(0, 3, 0, 3)
                },
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                IsEnabled = false,
                Focusable = false,
                IsHitTestVisible = false,
                Padding = new Thickness(4, 0, 4, 0)
            });
            items.AddRange(StyleList.Designed.Select(style => (object)Option(style, StyleList.Name(style))));
            return items;
        }

        private static UIElement Choice(string label, string path, params KeyValuePair<object, string>[] options)
        {
            return Choice(label, path, (System.Collections.IEnumerable)options);
        }

        private static UIElement Choice(string label, string path, System.Collections.IEnumerable options)
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 5)
            });

            var combo = new ComboBox
            {
                ItemsSource = options,
                DisplayMemberPath = "Value",
                SelectedValuePath = "Key",
                Width = 280,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 10)
            };
            combo.SetBinding(Selector.SelectedValueProperty,
                new Binding(path) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            panel.Children.Add(combo);
            return panel;
        }

        /// <summary>A 0–90% slider with the current value shown next to it.</summary>
        private static UIElement PercentSlider(string label, string path, string hint)
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 5)
            });

            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, hint == null ? 15 : 5) };
            var slider = new Slider
            {
                Minimum = 0,
                Maximum = SplashAddonSettings.MaxPercent,
                SmallChange = 1,
                LargeChange = 5,
                TickFrequency = 1,
                IsSnapToTickEnabled = true,
                Width = 280,
                VerticalAlignment = VerticalAlignment.Center
            };
            slider.SetBinding(RangeBase.ValueProperty,
                new Binding(path) { Mode = BindingMode.TwoWay, Converter = IntToDouble, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            row.Children.Add(slider);

            var value = new TextBlock { Width = 50, Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            value.SetBinding(TextBlock.TextProperty, new Binding(path) { StringFormat = "{0}%" });
            row.Children.Add(value);
            panel.Children.Add(row);

            if (hint != null)
            {
                TextBlock hintBlock = Hint(hint);
                hintBlock.Margin = new Thickness(0, 0, 0, 15);
                panel.Children.Add(hintBlock);
            }

            return panel;
        }

        /// <summary>
        /// Games with their own settings in one table: each row shows the game's name, its style, whether its splash
        /// screen is as set for all games, off or has a fixed time (with the seconds), and a Remove button. Games are
        /// added from the game context menu.
        /// </summary>
        private sealed class GameExceptionsEditor : StackPanel
        {
            private const double NameWidth = 260;
            private const double StyleWidth = 150;
            private const double ModeWidth = 130;

            private readonly Func<string, string> _resolveName;
            private readonly StackPanel _rows = new StackPanel();
            private readonly Grid _header = new Grid();
            private SplashAddonSettings _settings;

            public GameExceptionsEditor(Func<string, string> resolveName)
            {
                _resolveName = resolveName;
                Margin = new Thickness(0, 0, 0, 10);

                AddColumns(_header);
                AddToGrid(_header, new TextBlock { Text = Loc.GameColumn, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) }, 0, 0);
                AddToGrid(_header, new TextBlock { Text = Loc.StyleLabel, FontWeight = FontWeights.Bold, Margin = new Thickness(5, 0, 0, 5) }, 0, 1);
                AddToGrid(_header, new TextBlock { Text = Loc.SplashColumn, FontWeight = FontWeights.Bold, Margin = new Thickness(5, 0, 0, 5) }, 0, 2);
                AddToGrid(_header, new TextBlock { Text = Loc.TimeColumn, FontWeight = FontWeights.Bold, Margin = new Thickness(5, 0, 0, 5) }, 0, 3);
                Children.Add(_header);
                Children.Add(_rows);

                DataContextChanged += (s, e) => Attach(e.NewValue as SplashAddonSettings);
                Unloaded += (s, e) => Attach(null);
                Loaded += (s, e) =>
                {
                    if (_settings == null)
                    {
                        Attach(DataContext as SplashAddonSettings);
                    }
                };
            }

            private void Attach(SplashAddonSettings settings)
            {
                if (_settings != null)
                {
                    _settings.PropertyChanged -= OnSettingsChanged;
                }

                _settings = settings;
                if (_settings != null)
                {
                    _settings.PropertyChanged += OnSettingsChanged;
                }

                Rebuild();
            }

            // Changing a row, removing a game, or Cancel in the settings dialog replaces a whole list.
            private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == "GameSpecificDurations" || e.PropertyName == "ExcludedGameIds" || e.PropertyName == "GameStyles")
                {
                    Rebuild();
                }
            }

            private void Rebuild()
            {
                _rows.Children.Clear();
                if (_settings == null)
                {
                    return;
                }

                List<GameEntry> games = GameEntries.Sort(_settings.GamesWithOwnSettings, _resolveName);
                _header.Visibility = games.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                if (games.Count == 0)
                {
                    TextBlock empty = Hint(Loc.NoGames);
                    empty.Margin = new Thickness(0, 0, 0, 5);
                    _rows.Children.Add(empty);
                    return;
                }

                foreach (GameEntry game in games)
                {
                    _rows.Children.Add(CreateRow(game));
                }
            }

            private static void AddColumns(Grid grid)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(NameWidth) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(StyleWidth + 5) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(ModeWidth + 5) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }

            private UIElement CreateRow(GameEntry game)
            {
                GameSplashMode current = _settings.GetGameSplashMode(game.Id);
                var row = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                AddColumns(row);

                AddToGrid(row, new TextBlock
                {
                    Text = game.Name ?? Loc.UnknownGame(game.Id),
                    ToolTip = game.Id,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center,
                    Opacity = game.Name == null ? 0.6 : 1.0
                }, 0, 0);

                // The game's own style, or Default (the style from the settings). A game whose splash screen is off
                // shows none, so its style is greyed out.
                List<object> styles = StyleChoices(withDefault: true);
                SplashStyle? own = _settings.GetGameStyle(game.Id);
                var style = new ComboBox
                {
                    ItemsSource = styles,
                    DisplayMemberPath = "Value",
                    SelectedItem = styles.OfType<KeyValuePair<object, string>>().First(item => Equals(item.Key, own)),
                    Width = StyleWidth,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(5, 0, 0, 0),
                    IsEnabled = current != GameSplashMode.Off,
                    Opacity = current != GameSplashMode.Off ? 1.0 : 0.5
                };

                // Applied after the selection event, as it rebuilds the rows (and this combo box with them).
                style.SelectionChanged += (s, e) =>
                {
                    if (style.SelectedItem is KeyValuePair<object, string> picked)
                    {
                        var chosen = (SplashStyle?)picked.Key;
                        Dispatcher.BeginInvoke((Action)(() => _settings?.SetGameStyle(new[] { game.Id }, chosen)));
                    }
                };
                AddToGrid(row, style, 0, 1);

                var modes = new[] { GameSplashMode.Default, GameSplashMode.FixedTime, GameSplashMode.Off };
                var mode = new ComboBox
                {
                    ItemsSource = new[] { Loc.GameDefault, Loc.GameFixedTime, Loc.GameSplashOff },
                    SelectedIndex = Array.IndexOf(modes, current),
                    Width = ModeWidth,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(5, 0, 0, 0)
                };

                mode.SelectionChanged += (s, e) =>
                {
                    GameSplashMode chosen = modes[Math.Max(0, mode.SelectedIndex)];
                    Dispatcher.BeginInvoke((Action)(() => _settings?.SetGameSplashMode(game.Id, chosen)));
                };
                AddToGrid(row, mode, 0, 2);

                if (current == GameSplashMode.FixedTime)
                {
                    int seconds0;
                    _settings.GameSpecificDurations.TryGetValue(game.Id, out seconds0);
                    var seconds = new TextBox
                    {
                        Text = seconds0.ToString(CultureInfo.InvariantCulture),
                        Width = 80,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(5, 0, 0, 0)
                    };

                    // Written straight into the dictionary: replacing it would rebuild the rows and lose the caret.
                    // Invalid or zero values are reported by the settings check on Save.
                    seconds.TextChanged += (s, e) =>
                    {
                        if (_settings?.GameSpecificDurations != null && int.TryParse(seconds.Text.Trim(), out int value))
                        {
                            _settings.GameSpecificDurations[game.Id] = value;
                        }
                    };
                    AddToGrid(row, seconds, 0, 3);
                }

                var remove = new Button { Content = Loc.Remove, Padding = new Thickness(10, 2, 10, 2), Margin = new Thickness(5, 0, 0, 0) };
                remove.Click += (s, e) => _settings?.ForgetGame(game.Id);
                AddToGrid(row, remove, 0, 4);
                return row;
            }
        }

        /// <summary>Rows of platform + seconds, with buttons to add and remove rows.</summary>
        private sealed class PlatformListEditor : StackPanel
        {
            private readonly IList<string> _platforms;
            private readonly StackPanel _rows = new StackPanel();
            private SplashAddonSettings _settings;
            private ObservableCollection<PlatformDuration> _collection;

            public PlatformListEditor(IList<string> platforms)
            {
                _platforms = platforms ?? new List<string>();
                Margin = new Thickness(0, 0, 0, 10);

                var header = new Grid();
                header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
                header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                AddToGrid(header, new TextBlock { Text = Loc.PlatformColumn, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) }, 0, 0);
                AddToGrid(header, new TextBlock { Text = Loc.TimeColumn, FontWeight = FontWeights.Bold, Margin = new Thickness(5, 0, 0, 5) }, 0, 1);
                Children.Add(header);
                Children.Add(_rows);

                var add = new Button
                {
                    Content = Loc.AddPlatform,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(12, 4, 12, 4),
                    Margin = new Thickness(0, 6, 0, 0)
                };
                add.Click += (s, e) => _collection?.Add(new PlatformDuration());
                Children.Add(add);

                DataContextChanged += (s, e) => Attach(e.NewValue as SplashAddonSettings);

                // Let go of the settings when the dialog closes, so closed pages are not kept alive by event handlers.
                Unloaded += (s, e) => Attach(null);
                Loaded += (s, e) =>
                {
                    if (_settings == null)
                    {
                        Attach(DataContext as SplashAddonSettings);
                    }
                };
            }

            private void Attach(SplashAddonSettings settings)
            {
                if (_settings != null)
                {
                    _settings.PropertyChanged -= OnSettingsChanged;
                }

                _settings = settings;
                if (_settings != null)
                {
                    _settings.PropertyChanged += OnSettingsChanged;
                }

                AttachCollection(_settings?.PlatformDurations);
            }

            private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
            {
                // Cancel in the settings dialog replaces the whole collection.
                if (e.PropertyName == "PlatformDurations")
                {
                    AttachCollection(_settings?.PlatformDurations);
                }
            }

            private void AttachCollection(ObservableCollection<PlatformDuration> collection)
            {
                if (_collection != null)
                {
                    _collection.CollectionChanged -= OnCollectionChanged;
                }

                _collection = collection;
                if (_collection != null)
                {
                    _collection.CollectionChanged += OnCollectionChanged;
                }

                Rebuild();
            }

            private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => Rebuild();

            private void Rebuild()
            {
                _rows.Children.Clear();
                if (_collection == null)
                {
                    return;
                }

                foreach (PlatformDuration entry in _collection)
                {
                    _rows.Children.Add(CreateRow(entry));
                }
            }

            private UIElement CreateRow(PlatformDuration entry)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 4) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var choices = new List<string>(_platforms);
                if (!string.IsNullOrEmpty(entry.Platform) && !choices.Contains(entry.Platform))
                {
                    choices.Add(entry.Platform);
                }

                var combo = new ComboBox { ItemsSource = choices, Width = 280, HorizontalAlignment = HorizontalAlignment.Left };
                combo.SetBinding(Selector.SelectedItemProperty,
                    new Binding("Platform") { Source = entry, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                AddToGrid(row, combo, 0, 0);

                var seconds = new TextBox { Width = 80, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(5, 0, 0, 0) };
                seconds.SetBinding(TextBox.TextProperty,
                    new Binding("Seconds") { Source = entry, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                AddToGrid(row, seconds, 0, 1);

                var remove = new Button { Content = Loc.Remove, Padding = new Thickness(10, 2, 10, 2), Margin = new Thickness(5, 0, 0, 0) };
                remove.Click += (s, e) => _collection?.Remove(entry);
                AddToGrid(row, remove, 0, 2);
                return row;
            }
        }

        private static void AddToGrid(Grid grid, UIElement element, int row, int column)
        {
            Grid.SetRow(element, row);
            Grid.SetColumn(element, column);
            grid.Children.Add(element);
        }

        private static void BindEnabled(FrameworkElement element, string path, bool inverse)
        {
            element.SetBinding(IsEnabledProperty, new Binding(path) { Converter = inverse ? Inverse : null });
            element.SetBinding(OpacityProperty, new Binding("IsEnabled")
            {
                RelativeSource = RelativeSource.Self,
                Converter = DimWhenDisabled
            });
        }

        private static TextBlock SectionHeader(string text, bool first = false)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, first ? 0 : 15, 0, 10)
            };
        }

        /// <summary>A thin line between sections, so it is clear where one ends and the next begins.</summary>
        private static Border Divider()
        {
            return new Border
            {
                Height = 1,
                Background = Brushes.Gray,
                Opacity = 0.45,
                SnapsToDevicePixels = true,
                Margin = new Thickness(0, 25, 0, 0)
            };
        }

        private static TextBlock Label(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 5)
            };
        }

        private static TextBlock Hint(string text, bool emphasize = false)
        {
            return new TextBlock
            {
                Text = text,
                FontStyle = FontStyles.Italic,
                FontSize = 11,
                Foreground = emphasize ? Brushes.Orange : Brushes.Gray,
                FontWeight = emphasize ? FontWeights.Bold : FontWeights.Normal,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };
        }


        private static CheckBox Toggle(string text, string path, bool bold = false)
        {
            var checkBox = new CheckBox
            {
                Content = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap },
                FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
                Margin = new Thickness(0, 0, 0, 10)
            };
            checkBox.SetBinding(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty,
                new Binding(path) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            return checkBox;
        }

        private static UIElement NumberField(string label, string path, string hint)
        {
            var panel = new StackPanel();
            panel.Children.Add(new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 5)
            });

            var box = new TextBox
            {
                Width = 100,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, hint == null ? 15 : 5)
            };
            box.SetBinding(TextBox.TextProperty, new Binding(path) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            panel.Children.Add(box);

            if (hint != null)
            {
                TextBlock hintBlock = Hint(hint);
                hintBlock.Margin = new Thickness(0, 0, 0, 15);
                panel.Children.Add(hintBlock);
            }

            return panel;
        }

        private static TextBox MultilineBox(string path, double height)
        {
            var box = new TextBox
            {
                Height = height,
                AcceptsReturn = true,
                AcceptsTab = false,
                TextWrapping = TextWrapping.NoWrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                Padding = new Thickness(5),
                FontFamily = new FontFamily("Consolas"),
                Margin = new Thickness(0, 0, 0, 10)
            };
            box.SetBinding(TextBox.TextProperty, new Binding(path) { UpdateSourceTrigger = UpdateSourceTrigger.LostFocus });

            // The settings dialog may treat Enter as "Save"; keep Enter inside the text box instead.
            box.PreviewKeyDown += (sender, e) =>
            {
                if (e.Key != Key.Enter && e.Key != Key.Return)
                {
                    return;
                }

                int caret = box.SelectionStart;
                box.SelectedText = Environment.NewLine;
                box.SelectionLength = 0;
                box.CaretIndex = caret + Environment.NewLine.Length;
                e.Handled = true;
            };

            return box;
        }

        /// <summary>Slider values are doubles, the settings store whole percents.</summary>
        private sealed class IntDoubleConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return value is int number ? (double)number : 0.0;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return value is double number ? (int)Math.Round(number) : 0;
            }
        }

        /// <summary>One-way: a value derived from the selected style.</summary>
        private sealed class StyleConverter : IValueConverter
        {
            private readonly Func<SplashStyle, object> _convert;

            public StyleConverter(Func<SplashStyle, object> convert)
            {
                _convert = convert;
            }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return _convert(value is SplashStyle style ? style : SplashStyle.Classic);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return Binding.DoNothing;
            }
        }

        /// <summary>A radio button of the "when to close" choice: checked for its own mode, checking it selects that mode.</summary>
        private sealed class ModeConverter : IValueConverter
        {
            private readonly CloseMode _mode;

            public ModeConverter(CloseMode mode)
            {
                _mode = mode;
            }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return value is CloseMode mode && mode == _mode;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return value is bool isChecked && isChecked ? (object)_mode : Binding.DoNothing;
            }
        }

        private sealed class BoolConverter : IValueConverter
        {
            private readonly object _whenTrue;
            private readonly object _whenFalse;

            public BoolConverter(object whenTrue, object whenFalse)
            {
                _whenTrue = whenTrue;
                _whenFalse = whenFalse;
            }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return value is bool b && b ? _whenTrue : _whenFalse;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return Equals(value, _whenTrue);
            }
        }
    }
}
