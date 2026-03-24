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
    public sealed partial class CounterpartiesPage : Page
    {
        private readonly DatabaseService _db = new DatabaseService();
        private List<CounterpartyViewModel> _all = new();

        public CounterpartiesPage()
        {
            this.InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            _all = _db.GetCounterparties().Select(c => new CounterpartyViewModel(c)).ToList();
            CounterpartiesList.ItemsSource = _all;
            CountText.Text = $"Всего: {_all.Count}";
            AddCounterpartyButton.Visibility = SessionService.CanCreate
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ApplyFilters()
        {
            if (_all == null || _all.Count == 0) return;
            var query = SearchBox.Text?.ToLower() ?? "";
            var bl = (BlacklistFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Все";

            var filtered = _all.Where(c =>
            {
                bool matchSearch = string.IsNullOrEmpty(query) ||
                    (c.Name?.ToLower().Contains(query) ?? false) ||
                    (c.TaxID?.ToLower().Contains(query) ?? false);
                bool matchBl = bl == "Все" ||
                    (bl == "В чёрном списке" && c.IsBlacklisted) ||
                    (bl == "Не в списке" && !c.IsBlacklisted);
                return matchSearch && matchBl;
            }).ToList();

            CounterpartiesList.ItemsSource = filtered;
            CountText.Text = $"Показано: {filtered.Count} из {_all.Count}";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private bool _dialogOpen = false;

        private async void AddCounterparty_Click(object sender, RoutedEventArgs e)
        {
            if (_dialogOpen) return;
            _dialogOpen = true;

            var nameBox = new TextBox { PlaceholderText = "ООО «Компания»", HorizontalAlignment = HorizontalAlignment.Stretch };
            var taxBox = new TextBox { PlaceholderText = "1234567890", MaxLength = 12, HorizontalAlignment = HorizontalAlignment.Stretch };
            var categoryBox = new TextBox { PlaceholderText = "Торговля, Услуги...", HorizontalAlignment = HorizontalAlignment.Stretch };
            var countryBox = new TextBox { PlaceholderText = "Russia", Text = "Russia", HorizontalAlignment = HorizontalAlignment.Stretch };

            var riskCombo = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch, SelectedIndex = 0 };
            riskCombo.Items.Add("Low");
            riskCombo.Items.Add("Medium");
            riskCombo.Items.Add("High");

            var blacklistSwitch = new ToggleSwitch { Header = "В чёрном списке", IsOn = false };

            var form = new StackPanel { Spacing = 10, MinWidth = 380 };
            form.Children.Add(new TextBlock { Text = "Название", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            form.Children.Add(nameBox);
            form.Children.Add(new TextBlock { Text = "ИНН", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            form.Children.Add(taxBox);
            form.Children.Add(new TextBlock { Text = "Категория деятельности", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            form.Children.Add(categoryBox);
            form.Children.Add(new TextBlock { Text = "Страна", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            form.Children.Add(countryBox);
            form.Children.Add(new TextBlock { Text = "Уровень риска", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            form.Children.Add(riskCombo);
            form.Children.Add(blacklistSwitch);

            var dialog = new ContentDialog
            {
                Title = "Новый контрагент",
                Content = new ScrollViewer { Content = form, MaxHeight = 500 },
                PrimaryButtonText = "Создать",
                CloseButtonText = "Отмена",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var name = nameBox.Text.Trim();
                var tax = taxBox.Text.Trim();
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(tax))
                {
                    _dialogOpen = false;
                    return;
                }

                _db.CreateCounterparty(name, tax, categoryBox.Text.Trim(),
                    riskCombo.SelectedItem?.ToString() ?? "Low",
                    blacklistSwitch.IsOn, countryBox.Text.Trim());
                LoadData();
            }
            _dialogOpen = false;
        }

        private async void CounterpartiesList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (_dialogOpen) return;
            if (e.ClickedItem is not CounterpartyViewModel cp) return;
            _dialogOpen = true;

            var content = new StackPanel { Spacing = 12, MinWidth = 360 };
            content.Children.Add(MakeRow("Название", cp.Name ?? ""));
            content.Children.Add(MakeRow("ИНН", cp.TaxID ?? ""));
            content.Children.Add(MakeRow("Категория", cp.ActivityCategory ?? "—"));
            content.Children.Add(MakeRow("Уровень риска", cp.RiskLevel ?? ""));
            content.Children.Add(MakeRow("Чёрный список", cp.BlacklistText));
            content.Children.Add(MakeRow("Страна", cp.CountryOfRegistry ?? ""));
            content.Children.Add(MakeRow("Добавлен", cp.AddedDateFormatted));

            var dialog = new ContentDialog
            {
                Title = $"Контрагент #{cp.CounterpartyID}",
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

    public class CounterpartyViewModel
    {
        public int CounterpartyID { get; set; }
        public string? Name { get; set; }
        public string? TaxID { get; set; }
        public string? ActivityCategory { get; set; }
        public string? RiskLevel { get; set; }
        public bool IsBlacklisted { get; set; }
        public string? CountryOfRegistry { get; set; }
        public DateTime AddedDate { get; set; }

        public string IdText => $"#{CounterpartyID}";
        public string BlacklistText => IsBlacklisted ? "Да" : "Нет";
        public string AddedDateFormatted => AddedDate.ToString("dd.MM.yyyy");

        public SolidColorBrush RiskBg => RiskLevel switch
        {
            "Low" => new SolidColorBrush(ColorHelper.FromArgb(30, 76, 175, 80)),
            "Medium" => new SolidColorBrush(ColorHelper.FromArgb(30, 255, 193, 7)),
            "High" => new SolidColorBrush(ColorHelper.FromArgb(30, 255, 152, 0)),
            _ => new SolidColorBrush(ColorHelper.FromArgb(30, 128, 128, 128))
        };

        public SolidColorBrush RiskFg => RiskLevel switch
        {
            "Low" => new SolidColorBrush(ColorHelper.FromArgb(255, 76, 175, 80)),
            "Medium" => new SolidColorBrush(ColorHelper.FromArgb(255, 255, 193, 7)),
            "High" => new SolidColorBrush(ColorHelper.FromArgb(255, 255, 152, 0)),
            _ => new SolidColorBrush(ColorHelper.FromArgb(255, 180, 180, 180))
        };

        public SolidColorBrush BlacklistBg => IsBlacklisted
            ? new SolidColorBrush(ColorHelper.FromArgb(30, 244, 67, 54))
            : new SolidColorBrush(ColorHelper.FromArgb(30, 76, 175, 80));

        public SolidColorBrush BlacklistFg => IsBlacklisted
            ? new SolidColorBrush(ColorHelper.FromArgb(255, 244, 67, 54))
            : new SolidColorBrush(ColorHelper.FromArgb(255, 76, 175, 80));

        public CounterpartyViewModel(Counterparty c)
        {
            CounterpartyID = c.CounterpartyID;
            Name = c.Name;
            TaxID = c.TaxID;
            ActivityCategory = c.ActivityCategory;
            RiskLevel = c.RiskLevel;
            IsBlacklisted = c.IsBlacklisted;
            CountryOfRegistry = c.CountryOfRegistry;
            AddedDate = c.AddedDate;
        }
    }
}