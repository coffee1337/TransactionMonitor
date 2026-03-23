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
    public sealed partial class ClientsPage : Page
    {
        private readonly DatabaseService _db = new DatabaseService();
        private List<ClientViewModel> _allClients = new();

        public ClientsPage()
        {
            this.InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            var clients = _db.GetClients();
            _allClients = clients.Select(c => new ClientViewModel(c)).ToList();
            ClientsList.ItemsSource = _allClients;
            CountText.Text = $"Всего: {_allClients.Count}";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = SearchBox.Text.ToLower();
            var filtered = _allClients.Where(c =>
                (c.FullName?.ToLower().Contains(query) ?? false) ||
                (c.Phone?.ToLower().Contains(query) ?? false) ||
                (c.Email?.ToLower().Contains(query) ?? false)
            ).ToList();

            ClientsList.ItemsSource = filtered;
            CountText.Text = $"Показано: {filtered.Count} из {_allClients.Count}";
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            var export = new CsvExportService();
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(
                (Application.Current as App)?.MainWindow);

            var headers = new List<string> { "ID", "ФИО", "Телефон", "Email", "Риск-скор", "Статус" };
            var rows = _allClients.Select(c => new List<string>
            {
                c.ClientID.ToString(),
                c.FullName ?? "",
                c.Phone ?? "",
                c.Email ?? "",
                c.RiskScoreFormatted,
                c.StatusText
            }).ToList();

            await export.ExportAsync(headers, rows, "clients", windowHandle);
        }

        private bool _dialogOpen = false;
        private async void ClientsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_dialogOpen) return;
            if (e.ClickedItem is not ClientViewModel client) return;
            _dialogOpen = true;
            var (accounts, transactions) = _db.GetClientDetails(client.ClientID);

            // Строим текст счетов
            var accountsText = accounts.Count == 0 ? "Нет счетов" :
                string.Join("\n", accounts.Select(a =>
                    $"• {a.AccountNumber} | {a.BalanceFormatted} | {a.Status} | {a.AccountType}"));

            // Строим текст транзакций
            var txText = transactions.Count == 0 ? "Нет транзакций" :
                string.Join("\n", transactions.Select(t =>
                    $"• #{t.TransactionID} | {t.Amount:N0} ₽ | {t.TransactionType} | {t.Status} | {t.RiskLevel}"));

            var dialog = new ContentDialog
            {
                Title = $"👤 {client.FullName}",
                Content = new ScrollViewer
                {
                    Content = new StackPanel
                    {
                        Spacing = 16,
                        Children =
                {
                    new TextBlock
                    {
                        Text = $"📞 {client.Phone}    📧 {client.Email}",
                        Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 180, 180, 180))
                    },
                    new TextBlock
                    {
                        Text = $"Риск-скор: {client.RiskScoreFormatted}    Статус: {client.StatusText}",
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                    },
                    new TextBlock
                    {
                        Text = "🏦 Счета:",
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                    },
                    new TextBlock
                    {
                        Text = accountsText,
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas")
                    },
                    new TextBlock
                    {
                        Text = "💳 Последние транзакции:",
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                    },
                    new TextBlock
                    {
                        Text = txText,
                        FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas")
                    }
                }
                    },
                    MaxHeight = 400
                },
                CloseButtonText = "Закрыть",
                XamlRoot = this.XamlRoot
            };

            try
            {
                await dialog.ShowAsync();
            }
            finally
            {
                _dialogOpen = false;
            }
        }

    }

    public class ClientViewModel
    {
        public int ClientID { get; set; }
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public double ScoringScore { get; set; }
        public bool IsBlocked { get; set; }

        public string RiskScoreFormatted => (ScoringScore * 100).ToString("F0") + "%";
        public string StatusText => IsBlocked ? "Заблокирован" : "Активен";

        public SolidColorBrush StatusBackground => IsBlocked
            ? new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 220, 53, 69))
            : new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 40, 167, 69));

        public SolidColorBrush StatusForeground => IsBlocked
            ? new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 100, 100))
            : new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 100, 220, 100));

        public SolidColorBrush RiskColor
        {
            get
            {
                if (ScoringScore < 0.2)
                    return new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 100, 220, 100));
                if (ScoringScore < 0.5)
                    return new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 200, 0));
                if (ScoringScore < 0.8)
                    return new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 140, 0));
                return new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 80, 80));
            }
        }

        public ClientViewModel(Client c)
        {
            ClientID = c.ClientID;
            FullName = c.FullName;
            Phone = c.Phone;
            Email = c.Email;
            ScoringScore = c.ScoringScore;
            IsBlocked = c.IsBlocked;
        }
    }
}