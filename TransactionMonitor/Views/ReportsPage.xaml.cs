using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using TransactionMonitor.Services;

namespace TransactionMonitor.Views
{
    public sealed partial class ReportsPage : Page
    {
        private readonly DatabaseService _db = new DatabaseService();

        public ReportsPage()
        {
            this.InitializeComponent();
            this.Loaded += (s, e) => BuildSummaries();
        }

        private void BuildSummaries()
        {
            BuildRiskSummary();
            BuildTypeSummary();
            BuildClientsSummary();
        }

        private void BuildRiskSummary()
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
                var label = labels.ContainsKey(kv.Key) ? labels[kv.Key] : kv.Key;
                RiskSummaryPanel.Children.Add(MakeSummaryRow(label, kv.Value.ToString(), $"{(pct * 100):F1}%", color));
            }
        }

        private void BuildTypeSummary()
        {
            var data = _db.GetTransactionsByType();

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
                TypeSummaryPanel.Children.Add(
                    MakeSummaryRow(d.Type, $"{d.Count} тр.", d.Sum.ToString("N0") + " ₽", colors[i % colors.Length]));
            }
        }

        private void BuildClientsSummary()
        {
            var data = _db.GetTopClients();
            var medals = new string[] { "🥇", "🥈", "🥉", "4.", "5." };

            for (int i = 0; i < data.Count; i++)
            {
                var d = data[i];
                ClientsSummaryPanel.Children.Add(
                    MakeSummaryRow($"{medals[i]} {d.Name}", $"{d.TxCount} тр.", $"скоринг: {(d.Score * 100):F0}%",
                        ColorHelper.FromArgb(255, 66, 165, 245)));
            }
        }

        private Grid MakeSummaryRow(string label, string value, string extra, Windows.UI.Color color)
        {
            var grid = new Grid { Padding = new Thickness(12, 8, 12, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });

            var dot = new Border
            {
                Width = 12,
                Height = 12,
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(color),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(dot, 0);

            var labelText = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            };
            Grid.SetColumn(labelText, 1);

            var valueText = new TextBlock
            {
                Text = value,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(valueText, 2);

            var extraText = new TextBlock
            {
                Text = extra,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                FontSize = 13
            };
            Grid.SetColumn(extraText, 3);

            grid.Children.Add(dot);
            grid.Children.Add(labelText);
            grid.Children.Add(valueText);
            grid.Children.Add(extraText);

            return grid;
        }

        private async void ExportRisk_Click(object sender, RoutedEventArgs e)
        {
            var data = _db.GetRiskDistribution();
            var headers = new List<string> { "Уровень риска", "Количество" };
            var rows = data.Select(kv => new List<string> { kv.Key, kv.Value.ToString() }).ToList();
            await DoExport(headers, rows, "risk-distribution");
        }

        private async void ExportTypes_Click(object sender, RoutedEventArgs e)
        {
            var data = _db.GetTransactionsByType();
            var headers = new List<string> { "Тип", "Количество", "Сумма" };
            var rows = data.Select(d => new List<string> { d.Type, d.Count.ToString(), d.Sum.ToString("N2") }).ToList();
            await DoExport(headers, rows, "transactions-by-type");
        }

        private async void ExportClients_Click(object sender, RoutedEventArgs e)
        {
            var data = _db.GetTopClients();
            var headers = new List<string> { "Клиент", "Транзакций", "Скоринг" };
            var rows = data.Select(d => new List<string> { d.Name, d.TxCount.ToString(), $"{(d.Score * 100):F0}%" }).ToList();
            await DoExport(headers, rows, "top-clients");
        }

        private async void ExportAccounts_Click(object sender, RoutedEventArgs e)
        {
            var accounts = _db.GetAccountsFull();
            var headers = new List<string> { "ID", "Номер", "Клиент", "Баланс", "Валюта", "Тип", "Статус", "Открыт" };
            var rows = accounts.Select(a => new List<string>
            {
                a.AccountID.ToString(), a.AccountNumber ?? "", a.OwnerName ?? "",
                a.Balance.ToString("N2"), a.Currency ?? "", a.AccountType ?? "",
                a.Status ?? "", a.OpenDate.ToString("dd.MM.yyyy")
            }).ToList();
            await DoExport(headers, rows, "accounts");
        }

        private async System.Threading.Tasks.Task DoExport(List<string> headers, List<List<string>> rows, string filename)
        {
            var export = new CsvExportService();
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(
                (Application.Current as App)?.MainWindow);
            await export.ExportAsync(headers, rows, filename, windowHandle);
        }
    }
}