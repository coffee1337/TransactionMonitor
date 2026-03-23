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

        public string ScoreFormatted => (Score * 100).ToString("F0") + "%";
        public string FraudText => IsFraud ? "Да" : "Нет";
        public string ReviewedText => ReviewedByAnalyst ? "✓ Да" : "— Нет";
        public string DateFormatted => ScoredAt.ToString("dd.MM.yyyy");

        public SolidColorBrush ReviewedForeground => ReviewedByAnalyst
            ? new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 100, 220, 100))
            : new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 180, 180, 180));

        public SolidColorBrush FraudBackground => IsFraud
            ? new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 220, 53, 69))
            : new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(40, 40, 167, 69));

        public SolidColorBrush FraudForeground => IsFraud
            ? new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 255, 100, 100))
            : new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 100, 220, 100));

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