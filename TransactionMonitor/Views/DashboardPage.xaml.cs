using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using TransactionMonitor.Models;
using TransactionMonitor.Services;

namespace TransactionMonitor.Views
{
    public sealed partial class DashboardPage : Page
    {
        private readonly DatabaseService _db = new DatabaseService();

        public DashboardPage()
        {
            this.InitializeComponent();
            this.Loaded += (s, e) =>
            {
                LoadData();
                FadeInStoryboard.Begin();
            };
        }

        private void LoadData()
        {
            var stats = _db.GetDashboardStats();

            TotalClientsText.Text = stats.TotalClients.ToString();
            BlockedClientsText.Text = $"заблокировано: {stats.BlockedClients}";

            TotalTransactionsText.Text = stats.TotalTransactions.ToString();
            TotalAmountText.Text = $"сумма: {stats.TotalAmount:N0} RUB";

            FraudCountText.Text = stats.FraudCount.ToString();
            UnreviewedText.Text = $"непроверенных: {stats.UnreviewedHighRisk}";

            var avgPct = stats.AvgRiskScore * 100;
            AvgRiskText.Text = $"{avgPct:F1}%";
            AvgRiskBar.Value = avgPct;
            AvgRiskText.Foreground = new SolidColorBrush(
                avgPct < 25 ? ColorHelper.FromArgb(255, 76, 175, 80) :
                avgPct < 50 ? ColorHelper.FromArgb(255, 255, 193, 7) :
                avgPct < 75 ? ColorHelper.FromArgb(255, 255, 152, 0) :
                ColorHelper.FromArgb(255, 244, 67, 54));

            TotalAccountsText.Text = stats.TotalAccounts.ToString();
            ActiveAccountsText.Text = $"активных: {stats.ActiveAccounts}";
            FrozenAccountsText.Text = $"замороженных: {stats.FrozenAccounts}";

            TotalCounterpartiesText.Text = stats.TotalCounterparties.ToString();
            BlacklistedText.Text = $"в чёрном списке: {stats.BlacklistedCounterparties}";

            BuildRiskBars();
            LoadRecentTransactions();
        }

        private void BuildRiskBars()
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

            foreach (var kv in data)
            {
                double pct = (double)kv.Value / total;
                var color = colors.ContainsKey(kv.Key) ? colors[kv.Key] : ColorHelper.FromArgb(255, 128, 128, 128);

                var row = new Grid { Height = 36 };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });

                var label = new TextBlock
                {
                    Text = labels.ContainsKey(kv.Key) ? labels[kv.Key] : kv.Key,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    FontSize = 13
                };
                Grid.SetColumn(label, 0);

                var barBg = new Border
                {
                    Background = new SolidColorBrush(ColorHelper.FromArgb(20, 128, 128, 128)),
                    CornerRadius = new CornerRadius(4),
                    Height = 22,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var barFill = new Border
                {
                    Background = new SolidColorBrush(color),
                    CornerRadius = new CornerRadius(4),
                    Height = 22,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Width = 0
                };

                var barGrid = new Grid { VerticalAlignment = VerticalAlignment.Center };
                barGrid.Children.Add(barBg);
                barGrid.Children.Add(barFill);
                Grid.SetColumn(barGrid, 1);

                var capturedPct = pct;
                barGrid.SizeChanged += (s, e) =>
                {
                    var w = e.NewSize.Width;
                    if (w > 0) barFill.Width = w * Math.Min(capturedPct, 1.0);
                };

                var valueText = new TextBlock
                {
                    Text = $"{kv.Value} ({(pct * 100):F0}%)",
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    FontSize = 13,
                    Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                };
                Grid.SetColumn(valueText, 2);

                row.Children.Add(label);
                row.Children.Add(barGrid);
                row.Children.Add(valueText);
                RiskBarsPanel.Children.Add(row);
            }
        }

        private void LoadRecentTransactions()
        {
            var transactions = _db.GetTransactions().Take(10)
                .Select(t => new DashboardTransactionVM(t)).ToList();
            RecentTransactionsList.ItemsSource = transactions;
        }
    }

    public class DashboardTransactionVM
    {
        public int TransactionID { get; set; }
        public string? SenderName { get; set; }
        public decimal Amount { get; set; }
        public string? TransactionType { get; set; }
        public string? RiskLevel { get; set; }

        public string IdText => $"#{TransactionID}";
        public string AmountFormatted => Amount.ToString("N2") + " RUB";

        public SolidColorBrush RiskBackground => RiskLevel switch
        {
            "Low" => new SolidColorBrush(ColorHelper.FromArgb(30, 76, 175, 80)),
            "Medium" => new SolidColorBrush(ColorHelper.FromArgb(30, 255, 193, 7)),
            "High" => new SolidColorBrush(ColorHelper.FromArgb(30, 255, 152, 0)),
            "Critical" => new SolidColorBrush(ColorHelper.FromArgb(30, 244, 67, 54)),
            _ => new SolidColorBrush(ColorHelper.FromArgb(30, 128, 128, 128))
        };

        public SolidColorBrush RiskForeground => RiskLevel switch
        {
            "Low" => new SolidColorBrush(ColorHelper.FromArgb(255, 76, 175, 80)),
            "Medium" => new SolidColorBrush(ColorHelper.FromArgb(255, 255, 193, 7)),
            "High" => new SolidColorBrush(ColorHelper.FromArgb(255, 255, 152, 0)),
            "Critical" => new SolidColorBrush(ColorHelper.FromArgb(255, 244, 67, 54)),
            _ => new SolidColorBrush(ColorHelper.FromArgb(255, 180, 180, 180))
        };

        public DashboardTransactionVM(Transaction t)
        {
            TransactionID = t.TransactionID;
            SenderName = t.SenderName;
            Amount = t.Amount;
            TransactionType = t.TransactionType;
            RiskLevel = t.RiskLevel;
        }
    }
}