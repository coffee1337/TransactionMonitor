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

        private bool _dialogOpen = false;

        private async void AddClient_Click(object sender, RoutedEventArgs e)
        {
            if (_dialogOpen) return;
            _dialogOpen = true;

            var nameBox = new TextBox { PlaceholderText = "Иванов Иван Иванович", HorizontalAlignment = HorizontalAlignment.Stretch };
            var phoneBox = new TextBox { PlaceholderText = "+79001234567", HorizontalAlignment = HorizontalAlignment.Stretch };
            var emailBox = new TextBox { PlaceholderText = "email@example.com", HorizontalAlignment = HorizontalAlignment.Stretch };
            var seriesBox = new TextBox { PlaceholderText = "4510", MaxLength = 4, HorizontalAlignment = HorizontalAlignment.Stretch };
            var numberBox = new TextBox { PlaceholderText = "123456", MaxLength = 6, HorizontalAlignment = HorizontalAlignment.Stretch };
            var birthDate = new DatePicker { HorizontalAlignment = HorizontalAlignment.Stretch };

            var form = new StackPanel { Spacing = 10, MinWidth = 380 };
            form.Children.Add(new TextBlock { Text = "ФИО", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            form.Children.Add(nameBox);
            form.Children.Add(new TextBlock { Text = "Телефон", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            form.Children.Add(phoneBox);
            form.Children.Add(new TextBlock { Text = "Email", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            form.Children.Add(emailBox);

            var passportPanel = new Grid { ColumnSpacing = 12 };
            passportPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            passportPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(seriesBox, 0);
            Grid.SetColumn(numberBox, 1);
            passportPanel.Children.Add(seriesBox);
            passportPanel.Children.Add(numberBox);

            form.Children.Add(new TextBlock { Text = "Паспорт (серия / номер)", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            form.Children.Add(passportPanel);
            form.Children.Add(new TextBlock { Text = "Дата рождения", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            form.Children.Add(birthDate);

            var dialog = new ContentDialog
            {
                Title = "Новый клиент",
                Content = form,
                PrimaryButtonText = "Создать",
                CloseButtonText = "Отмена",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var name = nameBox.Text.Trim();
                var phone = phoneBox.Text.Trim();
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(phone))
                {
                    _dialogOpen = false;
                    return;
                }

                _db.CreateClient(
                    name, phone, emailBox.Text.Trim(),
                    seriesBox.Text.Trim(), numberBox.Text.Trim(),
                    birthDate.Date.DateTime
                );
                LoadData();
            }
            _dialogOpen = false;
        }

        private async void ClientsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_dialogOpen) return;
            if (e.ClickedItem is not ClientViewModel client) return;
            _dialogOpen = true;

            var (accounts, transactions) = _db.GetClientDetails(client.ClientID);

            var accountsText = accounts.Count == 0 ? "Нет счетов" :
                string.Join("\n", accounts.Select(a =>
                    $"  {a.AccountNumber}  |  {a.BalanceFormatted}  |  {a.Status}  |  {a.AccountType}"));

            var txText = transactions.Count == 0 ? "Нет транзакций" :
                string.Join("\n", transactions.Select(t =>
                    $"  #{t.TransactionID}  |  {t.Amount:N2} RUB  |  {t.TransactionType}  |  {t.RiskLevel ?? "-"}"));

            var content = new StackPanel { Spacing = 16, MinWidth = 420 };
            content.Children.Add(MakeInfoRow("ФИО", client.FullName ?? ""));
            content.Children.Add(MakeInfoRow("Телефон", client.Phone ?? ""));
            content.Children.Add(MakeInfoRow("Email", client.Email ?? "—"));
            content.Children.Add(MakeInfoRow("Скоринг", client.RiskScoreFormatted));
            content.Children.Add(MakeInfoRow("Статус", client.StatusText));
            content.Children.Add(MakeSection("Счета", accountsText));
            content.Children.Add(MakeSection("Последние транзакции", txText));

            var scrollViewer = new ScrollViewer
            {
                Content = content,
                MaxHeight = 500
            };

            var dialog = new ContentDialog
            {
                Title = $"Клиент #{client.ClientID}",
                Content = scrollViewer,
                CloseButtonText = "Закрыть",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
            _dialogOpen = false;
        }

        private StackPanel MakeInfoRow(string label, string value)
        {
            var p = new StackPanel { Spacing = 2 };
            p.Children.Add(new TextBlock { Text = label, FontSize = 12, Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"] });
            p.Children.Add(new TextBlock { Text = value, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            return p;
        }

        private StackPanel MakeSection(string title, string body)
        {
            var p = new StackPanel { Spacing = 4 };
            p.Children.Add(new TextBlock { Text = title, FontSize = 13, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            p.Children.Add(new TextBlock { Text = body, FontSize = 12, FontFamily = new FontFamily("Consolas"), TextWrapping = TextWrapping.Wrap, Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"] });
            return p;
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

        public string IdText => $"#{ClientID}";
        public string RiskScoreFormatted => $"{(ScoringScore * 100):F0}%";
        public string StatusText => IsBlocked ? "Заблокирован" : "Активен";

        public SolidColorBrush ScoreBg => ScoringScore > 0.7
            ? new SolidColorBrush(ColorHelper.FromArgb(30, 244, 67, 54))
            : ScoringScore > 0.4
                ? new SolidColorBrush(ColorHelper.FromArgb(30, 255, 193, 7))
                : new SolidColorBrush(ColorHelper.FromArgb(30, 76, 175, 80));

        public SolidColorBrush ScoreFg => ScoringScore > 0.7
            ? new SolidColorBrush(ColorHelper.FromArgb(255, 244, 67, 54))
            : ScoringScore > 0.4
                ? new SolidColorBrush(ColorHelper.FromArgb(255, 255, 193, 7))
                : new SolidColorBrush(ColorHelper.FromArgb(255, 76, 175, 80));

        public SolidColorBrush StatusBg => IsBlocked
            ? new SolidColorBrush(ColorHelper.FromArgb(30, 244, 67, 54))
            : new SolidColorBrush(ColorHelper.FromArgb(30, 76, 175, 80));

        public SolidColorBrush StatusFg => IsBlocked
            ? new SolidColorBrush(ColorHelper.FromArgb(255, 244, 67, 54))
            : new SolidColorBrush(ColorHelper.FromArgb(255, 76, 175, 80));

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