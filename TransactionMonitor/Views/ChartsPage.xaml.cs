using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using TransactionMonitor.Services;

namespace TransactionMonitor.Views
{
    public sealed partial class ChartsPage : Page
    {
        private readonly DatabaseService _db = new DatabaseService();
        private readonly List<(Border bar, double targetHeight)> _verticalBars = new();
        private readonly List<(Border segment, double targetWidth)> _segments = new();
        private DispatcherTimer? _animTimer;
        private int _animStep;

        public ChartsPage()
        {
            this.InitializeComponent();
            this.Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            BuildRiskSegmentBar();
            BuildVerticalBars();
            BuildClientsChart();
            StartAnimation();
        }

        private void BuildRiskSegmentBar()
        {
            var data = _db.GetRiskDistribution();
            int total = data.Values.Sum();
            if (total == 0) total = 1;

            var colors = new Dictionary<string, Windows.UI.Color>
            {
                { "Low", ColorHelper.FromArgb(255, 76, 175, 80) },
                { "Medium", ColorHelper.FromArgb(255, 255, 193, 7) },
                { "High", ColorHelper.FromArgb(255, 255, 152, 0) },
                { "Critical", ColorHelper.FromArgb(255, 244, 67, 54) }
            };

            var labels = new Dictionary<string, string>
            {
                { "Low", "Низкий" }, { "Medium", "Средний" },
                { "High", "Высокий" }, { "Critical", "Критический" }
            };

            double containerWidth = 700;
            int col = 0;

            foreach (var kv in data)
            {
                double pct = (double)kv.Value / total;
                var color = colors.ContainsKey(kv.Key) ? colors[kv.Key] : ColorHelper.FromArgb(255, 128, 128, 128);
                double targetW = containerWidth * pct;

                var segment = new Border
                {
                    Background = new SolidColorBrush(color),
                    Height = 40,
                    Width = 0,
                    Margin = new Thickness(0, 0, 2, 0)
                };
                SegmentBar.Children.Add(segment);
                _segments.Add((segment, targetW));

                var card = new Border
                {
                    Background = new SolidColorBrush(ColorHelper.FromArgb(15, color.R, color.G, color.B)),
                    BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(40, color.R, color.G, color.B)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(12)
                };

                var cardContent = new StackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center };
                cardContent.Children.Add(new TextBlock
                {
                    Text = "●  " + (labels.ContainsKey(kv.Key) ? labels[kv.Key] : kv.Key),
                    FontSize = 13,
                    Foreground = new SolidColorBrush(color)
                });
                cardContent.Children.Add(new TextBlock
                {
                    Text = kv.Value.ToString(),
                    FontSize = 28,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                cardContent.Children.Add(new TextBlock
                {
                    Text = $"{(pct * 100):F1}%",
                    FontSize = 13,
                    Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                card.Child = cardContent;
                Grid.SetColumn(card, col);
                RiskCardsGrid.Children.Add(card);
                col++;
            }
        }

        private void BuildVerticalBars()
        {
            var data = _db.GetTransactionsByType();
            int max = data.Count > 0 ? data.Max(d => d.Count) : 1;
            double maxHeight = 200;

            var colors = new Windows.UI.Color[]
            {
                ColorHelper.FromArgb(255, 66, 165, 245),
                ColorHelper.FromArgb(255, 171, 71, 188),
                ColorHelper.FromArgb(255, 255, 167, 38),
                ColorHelper.FromArgb(255, 38, 198, 218),
                ColorHelper.FromArgb(255, 239, 83, 80)
            };

            for (int i = 0; i < data.Count; i++)
            {
                var d = data[i];
                double pct = (double)d.Count / max;
                double targetH = maxHeight * pct;
                var color = colors[i % colors.Length];

                var column = new StackPanel
                {
                    Spacing = 8,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Width = 80
                };

                var valueText = new TextBlock
                {
                    Text = d.Count.ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    FontSize = 14
                };

                var bar = new Border
                {
                    Background = new SolidColorBrush(color),
                    CornerRadius = new CornerRadius(6, 6, 0, 0),
                    Width = 56,
                    Height = 0,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                var labelText = new TextBlock
                {
                    Text = d.Type,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 12,
                    Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                };

                var sumText = new TextBlock
                {
                    Text = d.Sum.ToString("N0") + " ₽",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(color)
                };

                column.Children.Add(valueText);
                column.Children.Add(bar);
                column.Children.Add(labelText);
                column.Children.Add(sumText);

                VerticalBarsPanel.Children.Add(column);
                _verticalBars.Add((bar, targetH));
            }
        }

        private void BuildClientsChart()
        {
            var data = _db.GetTopClients();
            int max = data.Count > 0 ? data.Max(d => d.TxCount) : 1;

            var medals = new string[] { "🥇", "🥈", "🥉", "4.", "5." };

            for (int i = 0; i < data.Count; i++)
            {
                var d = data[i];
                double pct = (double)d.TxCount / max;

                var row = new Grid { Height = 48 };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });

                var medal = new TextBlock
                {
                    Text = medals[i],
                    FontSize = 18,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(medal, 0);

                var name = new TextBlock
                {
                    Text = d.Name,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Grid.SetColumn(name, 1);

                var barBg = new Border
                {
                    Background = new SolidColorBrush(ColorHelper.FromArgb(20, 66, 165, 245)),
                    CornerRadius = new CornerRadius(4),
                    Height = 24,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var barFill = new Border
                {
                    Background = new LinearGradientBrush
                    {
                        StartPoint = new Windows.Foundation.Point(0, 0),
                        EndPoint = new Windows.Foundation.Point(1, 0),
                        GradientStops =
                        {
                            new GradientStop { Color = ColorHelper.FromArgb(255, 66, 165, 245), Offset = 0 },
                            new GradientStop { Color = ColorHelper.FromArgb(255, 171, 71, 188), Offset = 1 }
                        }
                    },
                    CornerRadius = new CornerRadius(4),
                    Height = 24,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = 0
                };

                var barContainer = new Grid { VerticalAlignment = VerticalAlignment.Center };
                barContainer.Children.Add(barBg);
                barContainer.Children.Add(barFill);
                Grid.SetColumn(barContainer, 2);

                var capturedPct = pct;
                barContainer.SizeChanged += (s, e) =>
                {
                    var maxW = e.NewSize.Width;
                    if (maxW > 0) barFill.Width = maxW * Math.Min(capturedPct, 1.0);
                };

                var info = new TextBlock
                {
                    Text = $"{d.TxCount} тр. | {(d.Score * 100):F0}%",
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                    FontSize = 13
                };
                Grid.SetColumn(info, 3);

                row.Children.Add(medal);
                row.Children.Add(name);
                row.Children.Add(barContainer);
                row.Children.Add(info);
                ClientsPanel.Children.Add(row);
            }
        }

        private void StartAnimation()
        {
            _animStep = 0;
            _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _animTimer.Tick += AnimTimer_Tick;
            _animTimer.Start();
        }

        private void AnimTimer_Tick(object? sender, object e)
        {
            _animStep++;
            double progress = Math.Min(_animStep / 30.0, 1.0);
            double ease = 1 - Math.Pow(1 - progress, 3);

            foreach (var (bar, target) in _verticalBars)
                bar.Height = target * ease;

            foreach (var (segment, target) in _segments)
                segment.Width = target * ease;

            if (progress >= 1.0)
                _animTimer?.Stop();
        }
    }
}