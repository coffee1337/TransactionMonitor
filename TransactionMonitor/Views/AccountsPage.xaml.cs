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
    public sealed partial class AccountsPage : Page
    {
        private readonly DatabaseService _db = new DatabaseService();
        private List<AccountViewModel> _all = new();

        public AccountsPage()
        {
            this.InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            var accounts = _db.GetAccountsFull();
            _all = accounts.Select(a => new AccountViewModel(a)).ToList();
            AccountsList.ItemsSource = _all;

            TotalText.Text = _all.Count.ToString();
            ActiveText.Text = _all.Count(a => a.Status == "Active").ToString();
            FrozenText.Text = _all.Count(a => a.Status == "Frozen").ToString();

            var totalBalance = accounts.Sum(a => a.Balance);
            TotalBalanceText.Text = totalBalance.ToString("N2") + " ₽";

            CountText.Text = $"Всего: {_all.Count}";
        }

        private void ApplyFilters()
        {
            if (_all == null || _all.Count == 0) return;

            var query = SearchBox.Text?.ToLower() ?? "";
            var status = (StatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Все статусы";
            var type = (TypeFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Все типы";

            var filtered = _all.Where(a =>
            {
                bool matchSearch = string.IsNullOrEmpty(query) ||
                    (a.AccountNumber?.ToLower().Contains(query) ?? false) ||
                    (a.OwnerName?.ToLower().Contains(query) ?? false);
                bool matchStatus = status == "Все статусы" || a.Status == status;
                bool matchType = type == "Все типы" || a.AccountType == type;
                return matchSearch && matchStatus && matchType;
            }).ToList();

            AccountsList.ItemsSource = filtered;
            CountText.Text = $"Показано: {filtered.Count} из {_all.Count}";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            var export = new CsvExportService();
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(
                (Application.Current as App)?.MainWindow);

            var headers = new List<string> { "ID", "Номер счёта", "Клиент", "Баланс", "Валюта", "Тип", "Статус", "Дата открытия" };
            var rows = _all.Select(a => new List<string>
            {
                a.AccountID.ToString(),
                a.AccountNumber ?? "",
                a.OwnerName ?? "",
                a.Balance.ToString("N2"),
                a.Currency ?? "",
                a.AccountType ?? "",
                a.Status ?? "",
                a.OpenDateFormatted
            }).ToList();

            await export.ExportAsync(headers, rows, "accounts", windowHandle);
        }

        private bool _dialogOpen = false;
        private async void AccountsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_dialogOpen) return;
            if (e.ClickedItem is not AccountViewModel acc) return;
            _dialogOpen = true;

            var content = new StackPanel { Spacing = 12, MinWidth = 360 };
            content.Children.Add(MakeRow("Номер счёта:", acc.AccountNumber ?? ""));
            content.Children.Add(MakeRow("Владелец:", acc.OwnerName ?? ""));
            content.Children.Add(MakeRow("Баланс:", acc.BalanceFormatted));
            content.Children.Add(MakeRow("Тип:", acc.AccountType ?? ""));
            content.Children.Add(MakeRow("Статус:", acc.StatusText));
            content.Children.Add(MakeRow("Дата открытия:", acc.OpenDateFormatted));

            var dialog = new ContentDialog
            {
                Title = $"Счёт #{acc.AccountID}",
                Content = content,
                CloseButtonText = "Закрыть",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
            _dialogOpen = false;
        }

        private StackPanel MakeRow(string label, string value)
        {
            var panel = new StackPanel { Spacing = 4 };
            panel.Children.Add(new TextBlock
            {
                Text = label,
                FontSize = 12,
                Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });
            panel.Children.Add(new TextBlock { Text = value, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            return panel;
        }
    }

    public class AccountViewModel
    {
        public int AccountID { get; set; }
        public string? AccountNumber { get; set; }
        public string? OwnerName { get; set; }
        public decimal Balance { get; set; }
        public string? Currency { get; set; }
        public string? Status { get; set; }
        public string? AccountType { get; set; }
        public DateTime OpenDate { get; set; }

        public string IdText => $"#{AccountID}";
        public string BalanceFormatted => Balance.ToString("N2") + " " + Currency;
        public string OpenDateFormatted => OpenDate.ToString("dd.MM.yyyy");
        public string StatusText => Status switch
        {
            "Active" => "Активен",
            "Frozen" => "Заморожен",
            "Closed" => "Закрыт",
            _ => Status ?? ""
        };

        public SolidColorBrush BalanceForeground => Balance >= 0
            ? new SolidColorBrush(ColorHelper.FromArgb(255, 76, 175, 80))
            : new SolidColorBrush(ColorHelper.FromArgb(255, 244, 67, 54));

        public SolidColorBrush StatusBackground => Status switch
        {
            "Active" => new SolidColorBrush(ColorHelper.FromArgb(30, 76, 175, 80)),
            "Frozen" => new SolidColorBrush(ColorHelper.FromArgb(30, 255, 152, 0)),
            "Closed" => new SolidColorBrush(ColorHelper.FromArgb(30, 158, 158, 158)),
            _ => new SolidColorBrush(ColorHelper.FromArgb(30, 128, 128, 128))
        };

        public SolidColorBrush StatusForeground => Status switch
        {
            "Active" => new SolidColorBrush(ColorHelper.FromArgb(255, 76, 175, 80)),
            "Frozen" => new SolidColorBrush(ColorHelper.FromArgb(255, 255, 152, 0)),
            "Closed" => new SolidColorBrush(ColorHelper.FromArgb(255, 158, 158, 158)),
            _ => new SolidColorBrush(ColorHelper.FromArgb(255, 180, 180, 180))
        };

        public AccountViewModel(Account a)
        {
            AccountID = a.AccountID;
            AccountNumber = a.AccountNumber;
            OwnerName = a.OwnerName;
            Balance = a.Balance;
            Currency = a.Currency;
            Status = a.Status;
            AccountType = a.AccountType;
            OpenDate = a.OpenDate;
        }
    }
}