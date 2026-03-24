using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using TransactionMonitor.Services;

namespace TransactionMonitor.Views
{
    public sealed partial class RiskCalculatorPage : Page
    {
        private readonly RiskCalculator _calculator = new RiskCalculator();

        public RiskCalculatorPage()
        {
            this.InitializeComponent();
        }

        private void Calculate_Click(object sender, RoutedEventArgs e)
        {
            var input = new RiskInput
            {
                Amount = (decimal)AmountBox.Value,
                TransactionHour = (int)HourBox.Value,
                TransactionsLast24h = (int)FreqBox.Value,
                CounterpartyRiskLevel = (CounterpartyRiskBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Low",
                ClientScoringScore = ScoringBox.Value,
                IsCounterpartyBlacklisted = BlacklistSwitch.IsOn,
                IsClientBlocked = BlockedSwitch.IsOn
            };

            var result = _calculator.Calculate(input);

            // Показываем результат
            PlaceholderText.Visibility = Visibility.Collapsed;
            ResultPanel.Visibility = Visibility.Visible;

            // Устанавливаем уровень
            RiskLevelText.Text = result.LevelEmoji;

            // Цвет бейджа
            RiskLevelBadge.Background = result.Level switch
            {
                "Low" => new SolidColorBrush(ColorHelper.FromArgb(60, 40, 167, 69)),
                "Medium" => new SolidColorBrush(ColorHelper.FromArgb(60, 255, 193, 7)),
                "High" => new SolidColorBrush(ColorHelper.FromArgb(60, 255, 140, 0)),
                "Critical" => new SolidColorBrush(ColorHelper.FromArgb(60, 220, 53, 69)),
                _ => new SolidColorBrush(ColorHelper.FromArgb(60, 128, 128, 128))
            };

            // Прогресс-бар
            RiskProgressBar.Value = result.Score * 100;
            RiskScoreText.Text = $"Скор: {(result.Score * 100):F1}% из 100%";

            // Факторы
            FactorsList.ItemsSource = result.Factors;

            // Рекомендация
            RecommendationText.Text = result.Level switch
            {
                "Low" => "Рекомендация: Транзакция безопасна. Можно пропустить автоматически.",
                "Medium" => "Рекомендация: Требует внимания. Рекомендуется мониторинг.",
                "High" => "Рекомендация: Высокий риск! Требуется проверка аналитика.",
                "Critical" => "Рекомендация: Критический риск! Заблокировать и передать в службу безопасности.",
                _ => ""
            };
        }
    }
}