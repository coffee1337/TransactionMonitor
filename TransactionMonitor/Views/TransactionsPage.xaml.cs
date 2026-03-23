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
    public sealed partial class TransactionsPage : Page
    {
        private readonly DatabaseService _db = new DatabaseService();
        private List<TransactionViewModel> _allTransactions = new();

        public TransactionsPage()
        {
            this.InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            var transactions = _db.GetTransactions();
            _allTransactions = transactions.Select(t => new TransactionViewModel(t)).ToList();
            TransactionsList.ItemsSource = _allTransactions;
            CountText.Text = $"Всего: {_allTransactions.Count}";
        }

        private void ApplyFilters()
        {
            if (_allTransactions == null || _allTransactions.Count == 0) return;

            var query = SearchBox.Text?.ToLower() ?? "";

            var statusItem = StatusFilter.SelectedItem as ComboBoxItem;
            var status = statusItem?.Content?.ToString() ?? "Все статусы";

            var riskItem = RiskFilter.SelectedItem as ComboBoxItem;
            var risk = riskItem?.Content?.ToString() ?? "Все уровни";

            var filtered = _allTransactions.Where(t =>
            {
                bool matchSearch = string.IsNullOrEmpty(query) ||
                    (t.SenderName?.ToLower().Contains(query) ?? false) ||
                    (t.Description?.ToLower().Contains(query) ?? false);

                bool matchStatus = status == "Все статусы" || t.Status == status;

                bool matchRisk = risk == "Все уровни" || t.RiskLevel == risk;

                return matchSearch && matchStatus && matchRisk;
            }).ToList();

            TransactionsList.ItemsSource = filtered;
            CountText.Text = $"Показано: {filtered.Count} из {_allTransactions.Count}";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private async void TransactionsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is TransactionViewModel tx)
            {
                var dialog = new ContentDialog
                {
                    Title = $"Транзакция #{tx.TransactionID}",
                    Content = $"Клиент: {tx.SenderName}\n" +
                              $"Сумма: {tx.AmountFormatted}\n" +
                              $"Дата: {tx.DateFormatted}\n" +
                              $"Тип: {tx.TransactionType}\n" +
                              $"Статус: {tx.Status}\n" +
                              $"Контрагент: {tx.CounterpartyName}\n" +
                              $"Риск: {tx.RiskLevel} ({tx.RiskScoreFormatted})\n" +
                              $"IP: {tx.IPAddress}\n" +
                              $"Описание: {tx.Description}",
                    CloseButtonText = "Закрыть",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }
        private bool _dialogOpen = false;

        private async void AddTransaction_Click(object sender, RoutedEventArgs e)
        {
            if (_dialogOpen) return;
            _dialogOpen = true;

            var accounts = _db.GetAllAccounts();
            var counterparties = _db.GetCounterparties();

            // Выпадающий список счетов
            var senderCombo = new ComboBox
            {
                PlaceholderText = "Выберите счёт отправителя",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                ItemsSource = accounts,
                DisplayMemberPath = "DisplayName"
            };

            var counterpartyCombo = new ComboBox
            {
                PlaceholderText = "Выберите контрагента",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                ItemsSource = counterparties,
                DisplayMemberPath = "Name"
            };

            var amountBox = new NumberBox
            {
                PlaceholderText = "Сумма в рублях",
                Minimum = 1,
                Maximum = 99999999,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var typeCombo = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                SelectedIndex = 0
            };
            typeCombo.Items.Add("Payment");
            typeCombo.Items.Add("Transfer");
            typeCombo.Items.Add("Withdrawal");
            typeCombo.Items.Add("Deposit");

            var descBox = new TextBox
            {
                PlaceholderText = "Описание платежа",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var ipBox = new TextBox
            {
                PlaceholderText = "IP-адрес (необязательно)",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Text = "127.0.0.1"
            };

            var form = new StackPanel { Spacing = 12, Width = 420 };
            form.Children.Add(new TextBlock { Text = "Счёт отправителя" });
            form.Children.Add(senderCombo);
            form.Children.Add(new TextBlock { Text = "Контрагент" });
            form.Children.Add(counterpartyCombo);
            form.Children.Add(new TextBlock { Text = "Сумма (₽)" });
            form.Children.Add(amountBox);
            form.Children.Add(new TextBlock { Text = "Тип транзакции" });
            form.Children.Add(typeCombo);
            form.Children.Add(new TextBlock { Text = "Описание" });
            form.Children.Add(descBox);
            form.Children.Add(new TextBlock { Text = "IP-адрес" });
            form.Children.Add(ipBox);

            var dialog = new ContentDialog
            {
                Title = "Новая транзакция",
                Content = form,
                PrimaryButtonText = "Создать",
                CloseButtonText = "Отмена",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var senderAccount = senderCombo.SelectedItem as Account; var counterparty = counterpartyCombo.SelectedItem as CounterpartyViewModel;

                if (senderAccount == null || double.IsNaN(amountBox.Value))
                {
                    _dialogOpen = false;
                    return;
                }

                var newId = _db.CreateTransaction(
                    senderAccount.AccountID,
                    null,
                    counterparty != null ? (int?)counterparty.CounterpartyID : null,
                    (decimal)amountBox.Value,
                    typeCombo.SelectedItem?.ToString() ?? "Payment",
                    descBox.Text,
                    ipBox.Text
                );

                // Обновляем список
                LoadData();

                var confirm = new ContentDialog
                {
                    Title = "✅ Транзакция создана",
                    Content = $"Транзакция #{newId} успешно создана!\nРиск-скор рассчитан автоматически.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await confirm.ShowAsync();
            }

            _dialogOpen = false;
        }
    }

    public class TransactionViewModel
    {
        public int TransactionID { get; set; }
        public string? SenderName { get; set; }
        public string? CounterpartyName { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? TransactionType { get; set; }
        public string? Status { get; set; }
        public string? Description { get; set; }
        public string? IPAddress { get; set; }
        public string? RiskLevel { get; set; }
        public double? RiskScore { get; set; }

        public string AmountFormatted => Amount.ToString("N2") + " ₽";
        public string DateFormatted => TransactionDate.ToString("dd.MM.yyyy HH:mm");
        public string RiskScoreFormatted => RiskScore.HasValue
            ? (RiskScore.Value * 100).ToString("F0") + "%"
            : "—";

        public SolidColorBrush StatusBackground => Status switch
        {
            "Completed" => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 40, 167, 69)),
            "Rejected" => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 220, 53, 69)),
            _ => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 255, 193, 7))
        };

        public SolidColorBrush StatusForeground => Status switch
        {
            "Completed" => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 100, 220, 100)),
            "Rejected" => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 100, 100)),
            _ => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 200, 0))
        };

        public SolidColorBrush RiskBackground => RiskLevel switch
        {
            "Low" => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 40, 167, 69)),
            "Medium" => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 255, 193, 7)),
            "High" => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 255, 140, 0)),
            "Critical" => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 220, 53, 69)),
            _ => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 128, 128, 128))
        };

        public SolidColorBrush RiskForeground => RiskLevel switch
        {
            "Low" => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 100, 220, 100)),
            "Medium" => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 200, 0)),
            "High" => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 140, 0)),
            "Critical" => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 80, 80)),
            _ => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 180, 180, 180))
        };

        public TransactionViewModel(Transaction t)
        {
            TransactionID = t.TransactionID;
            SenderName = t.SenderName;
            CounterpartyName = t.CounterpartyName;
            Amount = t.Amount;
            TransactionDate = t.TransactionDate;
            TransactionType = t.TransactionType;
            Status = t.Status;
            Description = t.Description;
            IPAddress = t.IPAddress;
            RiskLevel = t.RiskLevel;
            RiskScore = t.RiskScore;
        }
    }
}