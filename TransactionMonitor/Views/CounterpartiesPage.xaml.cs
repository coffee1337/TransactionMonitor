using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
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
            _all = _db.GetCounterparties()
                      .Select(c => new CounterpartyViewModel(c))
                      .ToList();
            CounterpartiesList.ItemsSource = _all;
            CountText.Text = $"Всего: {_all.Count}";
        }

        private void ApplyFilters()
        {
            if (_all == null || _all.Count == 0) return;

            var query = SearchBox.Text?.ToLower() ?? "";
            var blacklist = (BlacklistFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Все";

            var filtered = _all.Where(c =>
            {
                bool matchSearch = string.IsNullOrEmpty(query) ||
                    (c.Name?.ToLower().Contains(query) ?? false) ||
                    (c.TaxID?.ToLower().Contains(query) ?? false);

                bool matchBlacklist = blacklist == "Все" ||
                    (blacklist == "В чёрном списке" && c.IsBlacklisted) ||
                    (blacklist == "Не в списке" && !c.IsBlacklisted);

                return matchSearch && matchBlacklist;
            }).ToList();

            CounterpartiesList.ItemsSource = filtered;
            CountText.Text = $"Показано: {filtered.Count} из {_all.Count}";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();
    }

    public class CounterpartyViewModel
    {
        public int CounterpartyID { get; set; }
        public string? Name { get; set; }
        public string? TaxID { get; set; }
        public string? ActivityCategory { get; set; }
        public string? RiskLevel { get; set; }
        public bool IsBlacklisted { get; set; }

        public string BlacklistText => IsBlacklisted ? "В чёрном списке" : "Нет";

        public SolidColorBrush BlacklistBackground => IsBlacklisted
            ? new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 220, 53, 69))
            : new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 40, 167, 69));

        public SolidColorBrush BlacklistForeground => IsBlacklisted
            ? new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 100, 100))
            : new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 100, 220, 100));

        public SolidColorBrush RiskBackground => RiskLevel switch
        {
            "Low" => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 40, 167, 69)),
            "Medium" => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 255, 193, 7)),
            "High" => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 220, 53, 69)),
            _ => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 128, 128, 128))
        };

        public SolidColorBrush RiskForeground => RiskLevel switch
        {
            "Low" => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 100, 220, 100)),
            "Medium" => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 200, 0)),
            "High" => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 100, 100)),
            _ => new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 180, 180, 180))
        };

        public CounterpartyViewModel(Models.Counterparty c)
        {
            CounterpartyID = c.CounterpartyID;
            Name = c.Name;
            TaxID = c.TaxID;
            ActivityCategory = c.ActivityCategory;
            RiskLevel = c.RiskLevel;
            IsBlacklisted = c.IsBlacklisted;
        }
    }
}