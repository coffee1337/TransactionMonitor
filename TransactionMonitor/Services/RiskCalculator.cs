using System;
using System.Collections.Generic;

namespace TransactionMonitor.Services
{
    public class RiskInput
    {
        public decimal Amount { get; set; }
        public int TransactionHour { get; set; }
        public bool IsCounterpartyBlacklisted { get; set; }
        public string CounterpartyRiskLevel { get; set; } = "Low";
        public bool IsClientBlocked { get; set; }
        public double ClientScoringScore { get; set; }
        public int TransactionsLast24h { get; set; }
    }

    public class RiskResult
    {
        public double Score { get; set; }
        public string Level { get; set; } = "";
        public List<string> Factors { get; set; } = new();

        public string LevelEmoji => Level switch
        {
            "Low" => "Low",
            "Medium" => "Medium",
            "High" => "High",
            "Critical" => "Critical",
            _ => Level
        };
    }

    public class RiskCalculator
    {
        public RiskResult Calculate(RiskInput input)
        {
            double score = 0.0;
            var factors = new List<string>();

            // Фактор 1: Сумма транзакции
            if (input.Amount > 500000)
            {
                score += 0.35;
                factors.Add("Очень крупная сумма (>500 000 руб.)");
            }
            else if (input.Amount > 100000)
            {
                score += 0.20;
                factors.Add("Крупная сумма (>100 000 руб.)");
            }
            else if (input.Amount > 50000)
            {
                score += 0.10;
                factors.Add("Средняя сумма (>50 000 руб.)");
            }

            // Фактор 2: Ночное время (00:00 - 05:00)
            if (input.TransactionHour >= 0 && input.TransactionHour < 5)
            {
                score += 0.20;
                factors.Add("Ночная транзакция (00:00 — 05:00)");
            }

            // Фактор 3: Контрагент в чёрном списке
            if (input.IsCounterpartyBlacklisted)
            {
                score += 0.40;
                factors.Add("Контрагент в чёрном списке");
            }

            // Фактор 4: Уровень риска контрагента
            if (input.CounterpartyRiskLevel == "High")
            {
                score += 0.20;
                factors.Add("Контрагент высокого риска");
            }
            else if (input.CounterpartyRiskLevel == "Medium")
            {
                score += 0.10;
                factors.Add("Контрагент среднего риска");
            }

            // Фактор 5: Клиент заблокирован
            if (input.IsClientBlocked)
            {
                score += 0.50;
                factors.Add("Клиент заблокирован");
            }

            // Фактор 6: Риск-скор клиента
            if (input.ClientScoringScore > 0.7)
            {
                score += 0.20;
                factors.Add("Высокий скоринг клиента");
            }
            else if (input.ClientScoringScore > 0.4)
            {
                score += 0.10;
                factors.Add("Средний скоринг клиента");
            }

            // Фактор 7: Много транзакций за 24 часа
            if (input.TransactionsLast24h > 10)
            {
                score += 0.15;
                factors.Add("Высокая частота (>10 тр. за 24ч)");
            }

            // Ограничиваем максимум
            score = Math.Min(score, 0.99);

            // Если нет факторов риска
            if (factors.Count == 0)
                factors.Add("Факторов риска не обнаружено");

            return new RiskResult
            {
                Score = score,
                Level = GetLevel(score),
                Factors = factors
            };
        }

        private string GetLevel(double score)
        {
            if (score < 0.20) return "Low";
            if (score < 0.50) return "Medium";
            if (score < 0.80) return "High";
            return "Critical";
        }
    }
}