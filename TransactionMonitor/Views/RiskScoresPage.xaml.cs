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
    public sealed partial class RiskScoresPage : Page
    {
        private readonly DatabaseService _db = new DatabaseService();
        private List<RiskScoreViewModel> _all = new();

        public RiskScoresPage()
        {
            this.InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            _all = _db.GetRiskScores()
                      .Select(r => new RiskScoreViewModel(r))
                      .ToList();

            RiskScoresList.ItemsSource = _all;
            TotalText.Text = _all.Count.ToString();
            FraudText.Text = _all.Count(r => r.IsFraud).ToString();
            ReviewedText.Text = _all.Count(r => r.ReviewedByAnalyst).ToString();
            CriticalText.Text = _all.Count(r => r.RiskLevel == "Critical").ToString();
            CountText.Text = $"Всего: {_all.Count}";

            RescanButton.Visibility = SessionService.CanCreate
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ApplyFilters()
        {
            if (_all == null || _all.Count == 0) return;

            var risk = (RiskFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Все уровни";
            var fraud = (FraudFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Все";

            var filtered = _all.Where(r =>
            {
                bool matchRisk = risk == "Все уровни" || r.RiskLevel == risk;
                bool matchFraud = fraud == "Все" ||
                    (fraud == "Мошенничество" && r.IsFraud) ||
                    (fraud == "Норма" && !r.IsFraud);
                return matchRisk && matchFraud;
            }).ToList();

            RiskScoresList.ItemsSource = filtered;
            CountText.Text = $"Показано: {filtered.Count} из {_all.Count}";
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        private async void Rescan_Click(object sender, RoutedEventArgs e)
        {
            RescanButton.IsEnabled = false;
            ScanProgress.IsActive = true;
            ScanProgress.Visibility = Visibility.Visible;
            ScanStatusText.Text = "Сканирование...";

            int count = 0;
            await System.Threading.Tasks.Task.Run(() =>
            {
                count = _db.RescanAllTransactions();
            });

            ScanProgress.IsActive = false;
            ScanProgress.Visibility = Visibility.Collapsed;
            ScanStatusText.Text = $"Обновлено: {count}";
            RescanButton.IsEnabled = true;
            LoadData();
        }
    }

    public class RiskScoreViewModel
    {
        public int ScoreID { get; set; }
        public int TransactionID { get; set; }
        public string? SenderName { get; set; }
        public double Score { get; set; }
        public string? RiskLevel { get; set; }
        public bool IsFraud { get; set; }
        public bool ReviewedByAnalyst { get; set; }
        public DateTime ScoredAt { get; set; }

        public string IdText => $"#{ScoreID}";
        public string TxIdText => $"#{TransactionID}";
        public string ScoreFormatted => (Score * 100).ToString("F0") + "%";
        public double ScoreValue => Score * 100;
        public string FraudText => IsFraud ? "Да" : "Нет";
        public string ReviewedText => ReviewedByAnalyst ? "Да" : "Нет";
        public string DateFormatted => ScoredAt.ToString("dd.MM.yyyy");

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

        public SolidColorBrush FraudBg => IsFraud
            ? new SolidColorBrush(ColorHelper.FromArgb(30, 244, 67, 54))
            : new SolidColorBrush(ColorHelper.FromArgb(30, 76, 175, 80));

        public SolidColorBrush FraudFg => IsFraud
            ? new SolidColorBrush(ColorHelper.FromArgb(255, 244, 67, 54))
            : new SolidColorBrush(ColorHelper.FromArgb(255, 76, 175, 80));

        public SolidColorBrush ReviewedForeground => ReviewedByAnalyst
            ? new SolidColorBrush(ColorHelper.FromArgb(255, 76, 175, 80))
            : new SolidColorBrush(ColorHelper.FromArgb(255, 180, 180, 180));

        public RiskScoreViewModel(Models.RiskScore r)
        {
            ScoreID = r.ScoreID;
            TransactionID = r.TransactionID;
            SenderName = r.SenderName;
            Score = r.Score;
            RiskLevel = r.RiskLevel;
            IsFraud = r.IsFraud;
            ReviewedByAnalyst = r.ReviewedByAnalyst;
            ScoredAt = r.ScoredAt;
        }
    }
}