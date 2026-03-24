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
            var status = (StatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Все статусы";
            var risk = (RiskFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Все уровни";

            var filtered = _allTransactions.Where(t =>
            {
                bool matchSearch = string.IsNullOrEmpty(query) ||
                    (t.SenderName?.ToLower().Contains(query) ?? false) ||
                    (t.Description?.ToLower().Contains(query) ?? false) ||
                    (t.CounterpartyName?.ToLower().Contains(query) ?? false);
                bool matchStatus = status == "Все статусы" || t.Status == status;
                bool matchRisk = risk == "Все уровни" || t.RiskLevel == risk;
                return matchSearch && matchStatus && matchRisk;
            }).ToList();

            TransactionsList.ItemsSource = filtered;
            CountText.Text = $"Показано: {filtered.Count} из {_allTransactions.Count}";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private bool _dialogOpen = false;

        private async void AddTransaction_Click(object sender, RoutedEventArgs e)
        {
            if (_dialogOpen) return;
            _dialogOpen = true;

            var accounts = _db.GetAllAccounts();
            var counterparties = _db.GetCounterparties();

            var senderCombo = new ComboBox
            {
                PlaceholderText = "Счёт отправителя",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                ItemsSource = accounts,
                DisplayMemberPath = "DisplayName"
            };

            var counterpartyCombo = new ComboBox
            {
                PlaceholderText = "Контрагент (необязательно)",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                ItemsSource = counterparties.Select(c => new CounterpartyItem { CounterpartyID = c.CounterpartyID, Name = c.Name }).ToList(),
                DisplayMemberPath = "Name"
            };

            var amountBox = new NumberBox { PlaceholderText = "Сумма", Minimum = 1, Maximum = 99999999, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline, HorizontalAlignment = HorizontalAlignment.Stretch };

            var typeCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, SelectedIndex = 0 };
            typeCombo.Items.Add("Payment");
            typeCombo.Items.Add("Transfer");
            typeCombo.Items.Add("Withdrawal");
            typeCombo.Items.Add("Deposit");

            var descBox = new TextBox { PlaceholderText = "Описание платежа", HorizontalAlignment = HorizontalAlignment.Stretch };
            var ipBox = new TextBox { PlaceholderText = "IP-адрес", Text = "127.0.0.1", HorizontalAlignment = HorizontalAlignment.Stretch };

            var form = new StackPanel { Spacing = 10, MinWidth = 400 };
            form.Children.Add(new TextBlock { Text = "Счёт отправителя", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            form.Children.Add(senderCombo);
            form.Children.Add(new TextBlock { Text = "Контрагент", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            form.Children.Add(counterpartyCombo);
            form.Children.Add(new TextBlock { Text = "Сумма (RUB)", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            form.Children.Add(amountBox);
            form.Children.Add(new TextBlock { Text = "Тип транзакции", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            form.Children.Add(typeCombo);
            form.Children.Add(new TextBlock { Text = "Описание", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            form.Children.Add(descBox);
            form.Children.Add(new TextBlock { Text = "IP-адрес", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            form.Children.Add(ipBox);

            var dialog = new ContentDialog
            {
                Title = "Новая транзакция",
                Content = new ScrollViewer { Content = form, MaxHeight = 500 },
                PrimaryButtonText = "Создать",
                CloseButtonText = "Отмена",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var senderAccount = senderCombo.SelectedItem as Account;
                var counterparty = counterpartyCombo.SelectedItem as CounterpartyItem;

                if (senderAccount == null || double.IsNaN(amountBox.Value))
                {
                    _dialogOpen = false;
                    return;
                }

                _db.CreateTransaction(
                    senderAccount.AccountID, null,
                    counterparty?.CounterpartyID,
                    (decimal)amountBox.Value,
                    typeCombo.SelectedItem?.ToString() ?? "Payment",
                    descBox.Text, ipBox.Text
                );
                LoadData();
            }
            _dialogOpen = false;
        }

        private async void TransactionsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_dialogOpen) return;
            if (e.ClickedItem is not TransactionViewModel tx) return;
            _dialogOpen = true;

            var content = new StackPanel { Spacing = 12, MinWidth = 380 };
            content.Children.Add(MakeRow("Отправитель", tx.SenderName ?? ""));
            content.Children.Add(MakeRow("Контрагент", tx.CounterpartyName ?? "—"));
            content.Children.Add(MakeRow("Сумма", tx.AmountFormatted));
            content.Children.Add(MakeRow("Тип", tx.TransactionType ?? ""));
            content.Children.Add(MakeRow("Статус", tx.Status ?? ""));
            content.Children.Add(MakeRow("Описание", tx.Description ?? "—"));
            content.Children.Add(MakeRow("Уровень риска", tx.RiskLevel ?? "—"));
            content.Children.Add(MakeRow("Риск-скор", tx.RiskScore.HasValue ? $"{(tx.RiskScore.Value * 100):F0}%" : "—"));
            content.Children.Add(MakeRow("Дата", tx.DateFormatted));

            var dialog = new ContentDialog
            {
                Title = $"Транзакция #{tx.TransactionID}",
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

    public class CounterpartyItem
    {
        public int CounterpartyID { get; set; }
        public string? Name { get; set; }
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
        public string? RiskLevel { get; set; }
        public double? RiskScore { get; set; }

        public string IdText => $"#{TransactionID}";
        public string AmountFormatted => Amount.ToString("N2") + " RUB";
        public string DateFormatted => TransactionDate.ToString("dd.MM.yyyy HH:mm");

        public SolidColorBrush StatusBg => Status switch
        {
            "Completed" => new SolidColorBrush(ColorHelper.FromArgb(30, 76, 175, 80)),
            "Pending" => new SolidColorBrush(ColorHelper.FromArgb(30, 255, 193, 7)),
            "Cancelled" => new SolidColorBrush(ColorHelper.FromArgb(30, 244, 67, 54)),
            _ => new SolidColorBrush(ColorHelper.FromArgb(30, 128, 128, 128))
        };

        public SolidColorBrush StatusFg => Status switch
        {
            "Completed" => new SolidColorBrush(ColorHelper.FromArgb(255, 76, 175, 80)),
            "Pending" => new SolidColorBrush(ColorHelper.FromArgb(255, 255, 193, 7)),
            "Cancelled" => new SolidColorBrush(ColorHelper.FromArgb(255, 244, 67, 54)),
            _ => new SolidColorBrush(ColorHelper.FromArgb(255, 180, 180, 180))
        };

        public SolidColorBrush RiskBg => RiskLevel switch
        {
            "Low" => new SolidColorBrush(ColorHelper.FromArgb(30, 76, 175, 80)),
            "Medium" => new SolidColorBrush(ColorHelper.FromArgb(30, 255, 193, 7)),
            "High" => new SolidColorBrush(ColorHelper.FromArgb(30, 255, 152, 0)),
            "Critical" => new SolidColorBrush(ColorHelper.FromArgb(30, 244, 67, 54)),
            _ => new SolidColorBrush(ColorHelper.FromArgb(30, 128, 128, 128))
        };

        public SolidColorBrush RiskFg => RiskLevel switch
        {
            "Low" => new SolidColorBrush(ColorHelper.FromArgb(255, 76, 175, 80)),
            "Medium" => new SolidColorBrush(ColorHelper.FromArgb(255, 255, 193, 7)),
            "High" => new SolidColorBrush(ColorHelper.FromArgb(255, 255, 152, 0)),
            "Critical" => new SolidColorBrush(ColorHelper.FromArgb(255, 244, 67, 54)),
            _ => new SolidColorBrush(ColorHelper.FromArgb(255, 180, 180, 180))
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
            RiskLevel = t.RiskLevel;
            RiskScore = t.RiskScore;
        }
    }
}