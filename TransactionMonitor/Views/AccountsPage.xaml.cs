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
            TotalBalanceText.Text = accounts.Sum(a => a.Balance).ToString("N2") + " RUB";
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

        private bool _dialogOpen = false;

        private async void AddAccount_Click(object sender, RoutedEventArgs e)
        {
            if (_dialogOpen) return;
            _dialogOpen = true;

            var clients = _db.GetClients();
            var clientCombo = new ComboBox
            {
                PlaceholderText = "Выберите клиента",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                ItemsSource = clients.Select(c => new { c.ClientID, Display = $"{c.FullName} (#{c.ClientID})" }).ToList(),
                DisplayMemberPath = "Display"
            };

            var numberBox = new TextBox { PlaceholderText = "40817810000000000001", HorizontalAlignment = HorizontalAlignment.Stretch };
            var balanceBox = new NumberBox { PlaceholderText = "0.00", Minimum = 0, Value = 0, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline, HorizontalAlignment = HorizontalAlignment.Stretch };

            var typeCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, SelectedIndex = 0 };
            typeCombo.Items.Add("Debit");
            typeCombo.Items.Add("Credit");
            typeCombo.Items.Add("Savings");
            typeCombo.Items.Add("Corporate");

            var form = new StackPanel { Spacing = 10, MinWidth = 380 };
            form.Children.Add(new TextBlock { Text = "Клиент", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            form.Children.Add(clientCombo);
            form.Children.Add(new TextBlock { Text = "Номер счёта", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            form.Children.Add(numberBox);
            form.Children.Add(new TextBlock { Text = "Начальный баланс (RUB)", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            form.Children.Add(balanceBox);
            form.Children.Add(new TextBlock { Text = "Тип счёта", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            form.Children.Add(typeCombo);

            var dialog = new ContentDialog
            {
                Title = "Новый счёт",
                Content = form,
                PrimaryButtonText = "Создать",
                CloseButtonText = "Отмена",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                dynamic selected = clientCombo.SelectedItem;
                if (selected == null || string.IsNullOrEmpty(numberBox.Text.Trim()))
                {
                    _dialogOpen = false;
                    return;
                }
                int clientId = selected.ClientID;
                _db.CreateAccount(clientId, numberBox.Text.Trim(), (decimal)balanceBox.Value, typeCombo.SelectedItem?.ToString() ?? "Debit");
                LoadData();
            }
            _dialogOpen = false;
        }

        private async void AccountsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_dialogOpen) return;
            if (e.ClickedItem is not AccountViewModel acc) return;
            _dialogOpen = true;

            var content = new StackPanel { Spacing = 12, MinWidth = 360 };
            content.Children.Add(MakeRow("Номер счёта", acc.AccountNumber ?? ""));
            content.Children.Add(MakeRow("Владелец", acc.OwnerName ?? ""));
            content.Children.Add(MakeRow("Баланс", acc.BalanceFormatted));
            content.Children.Add(MakeRow("Тип", acc.AccountType ?? ""));
            content.Children.Add(MakeRow("Статус", acc.StatusText));
            content.Children.Add(MakeRow("Дата открытия", acc.OpenDateFormatted));

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
            var p = new StackPanel { Spacing = 2 };
            p.Children.Add(new TextBlock { Text = label, FontSize = 12, Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"] });
            p.Children.Add(new TextBlock { Text = value, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            return p;
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