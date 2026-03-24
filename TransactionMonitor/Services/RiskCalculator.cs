using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;

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
        public int SenderAccountId { get; set; }
        public int? CounterpartyId { get; set; }
        public string TransactionType { get; set; } = "Payment";
        public string? IpAddress { get; set; }
    }

    public class RiskFactor
    {
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public double Weight { get; set; }
        public string Severity { get; set; } = "Low";
    }

    public class RiskResult
    {
        public double Score { get; set; }
        public string Level { get; set; } = "";
        public List<RiskFactor> Factors { get; set; } = new();
        public string ModelVersion { get; set; } = "RuleEngine_v2.0";
        public string FeatureVectorJson { get; set; } = "";

        public string LevelDisplay => Level switch
        {
            "Low" => "Low",
            "Medium" => "Medium",
            "High" => "High",
            "Critical" => "Critical",
            _ => Level
        };

        public List<string> FactorDescriptions =>
            Factors.Select(f => $"[{f.Severity}] {f.Category}: {f.Description} (+{f.Weight:F2})").ToList();
    }

    public class RiskCalculator
    {
        private readonly string _connectionString;

        public RiskCalculator()
        {
            _connectionString = DatabaseService.ConnectionString;
        }

        public RiskResult Calculate(RiskInput input)
        {
            var factors = new List<RiskFactor>();
            var features = new Dictionary<string, object>();

            // === ФАКТОР 1: Сумма транзакции (многоуровневая оценка) ===
            features["amount"] = input.Amount;
            if (input.Amount > 1_000_000)
            {
                factors.Add(new RiskFactor
                {
                    Category = "Сумма",
                    Description = $"Экстремально крупная сумма: {input.Amount:N0} руб.",
                    Weight = 0.30,
                    Severity = "Critical"
                });
            }
            else if (input.Amount > 500_000)
            {
                factors.Add(new RiskFactor
                {
                    Category = "Сумма",
                    Description = $"Очень крупная сумма: {input.Amount:N0} руб.",
                    Weight = 0.22,
                    Severity = "High"
                });
            }
            else if (input.Amount > 100_000)
            {
                factors.Add(new RiskFactor
                {
                    Category = "Сумма",
                    Description = $"Крупная сумма: {input.Amount:N0} руб.",
                    Weight = 0.12,
                    Severity = "Medium"
                });
            }
            else if (input.Amount > 50_000)
            {
                factors.Add(new RiskFactor
                {
                    Category = "Сумма",
                    Description = $"Средняя сумма: {input.Amount:N0} руб.",
                    Weight = 0.05,
                    Severity = "Low"
                });
            }

            // === ФАКТОР 2: Время суток ===
            features["hour"] = input.TransactionHour;
            if (input.TransactionHour >= 0 && input.TransactionHour < 4)
            {
                factors.Add(new RiskFactor
                {
                    Category = "Время",
                    Description = "Глубокая ночь (00:00 — 04:00)",
                    Weight = 0.18,
                    Severity = "High"
                });
            }
            else if (input.TransactionHour >= 4 && input.TransactionHour < 6)
            {
                factors.Add(new RiskFactor
                {
                    Category = "Время",
                    Description = "Раннее утро (04:00 — 06:00)",
                    Weight = 0.08,
                    Severity = "Medium"
                });
            }

            // === ФАКТОР 3: Контрагент в чёрном списке ===
            features["counterparty_blacklisted"] = input.IsCounterpartyBlacklisted;
            if (input.IsCounterpartyBlacklisted)
            {
                factors.Add(new RiskFactor
                {
                    Category = "Контрагент",
                    Description = "Контрагент находится в чёрном списке",
                    Weight = 0.35,
                    Severity = "Critical"
                });
            }

            // === ФАКТОР 4: Уровень риска контрагента ===
            features["counterparty_risk"] = input.CounterpartyRiskLevel;
            if (input.CounterpartyRiskLevel == "High")
            {
                factors.Add(new RiskFactor
                {
                    Category = "Контрагент",
                    Description = "Контрагент с высоким уровнем риска",
                    Weight = 0.15,
                    Severity = "High"
                });
            }
            else if (input.CounterpartyRiskLevel == "Medium")
            {
                factors.Add(new RiskFactor
                {
                    Category = "Контрагент",
                    Description = "Контрагент со средним уровнем риска",
                    Weight = 0.07,
                    Severity = "Medium"
                });
            }

            // === ФАКТОР 5: Клиент заблокирован ===
            features["client_blocked"] = input.IsClientBlocked;
            if (input.IsClientBlocked)
            {
                factors.Add(new RiskFactor
                {
                    Category = "Клиент",
                    Description = "Клиент заблокирован в системе",
                    Weight = 0.40,
                    Severity = "Critical"
                });
            }

            // === ФАКТОР 6: Скоринг клиента ===
            features["client_scoring"] = input.ClientScoringScore;
            if (input.ClientScoringScore > 0.8)
            {
                factors.Add(new RiskFactor
                {
                    Category = "Клиент",
                    Description = $"Очень высокий скоринг клиента: {(input.ClientScoringScore * 100):F0}%",
                    Weight = 0.20,
                    Severity = "High"
                });
            }
            else if (input.ClientScoringScore > 0.6)
            {
                factors.Add(new RiskFactor
                {
                    Category = "Клиент",
                    Description = $"Повышенный скоринг клиента: {(input.ClientScoringScore * 100):F0}%",
                    Weight = 0.10,
                    Severity = "Medium"
                });
            }

            // === ФАКТОР 7: Частота транзакций за 24 часа ===
            features["tx_last_24h"] = input.TransactionsLast24h;
            if (input.TransactionsLast24h > 20)
            {
                factors.Add(new RiskFactor
                {
                    Category = "Частота",
                    Description = $"Аномальная частота: {input.TransactionsLast24h} тр./24ч",
                    Weight = 0.22,
                    Severity = "High"
                });
            }
            else if (input.TransactionsLast24h > 10)
            {
                factors.Add(new RiskFactor
                {
                    Category = "Частота",
                    Description = $"Высокая частота: {input.TransactionsLast24h} тр./24ч",
                    Weight = 0.12,
                    Severity = "Medium"
                });
            }
            else if (input.TransactionsLast24h > 5)
            {
                factors.Add(new RiskFactor
                {
                    Category = "Частота",
                    Description = $"Повышенная частота: {input.TransactionsLast24h} тр./24ч",
                    Weight = 0.05,
                    Severity = "Low"
                });
            }

            // === ФАКТОР 8: Тип транзакции ===
            features["tx_type"] = input.TransactionType;
            if (input.TransactionType == "Withdrawal" && input.Amount > 100_000)
            {
                factors.Add(new RiskFactor
                {
                    Category = "Тип операции",
                    Description = "Крупное снятие наличных",
                    Weight = 0.15,
                    Severity = "High"
                });
            }
            else if (input.TransactionType == "Transfer" && input.Amount > 200_000)
            {
                factors.Add(new RiskFactor
                {
                    Category = "Тип операции",
                    Description = "Крупный перевод",
                    Weight = 0.08,
                    Severity = "Medium"
                });
            }

            // === ФАКТОР 9: Аномалия суммы (отклонение от среднего) ===
            if (input.SenderAccountId > 0)
            {
                var avgAmount = GetAverageTransactionAmount(input.SenderAccountId);
                features["avg_amount"] = avgAmount;
                if (avgAmount > 0 && (double)input.Amount > avgAmount * 5)
                {
                    factors.Add(new RiskFactor
                    {
                        Category = "Аномалия",
                        Description = $"Сумма в {((double)input.Amount / avgAmount):F1}x больше средней ({avgAmount:N0} руб.)",
                        Weight = 0.20,
                        Severity = "High"
                    });
                }
                else if (avgAmount > 0 && (double)input.Amount > avgAmount * 3)
                {
                    factors.Add(new RiskFactor
                    {
                        Category = "Аномалия",
                        Description = $"Сумма в {((double)input.Amount / avgAmount):F1}x больше средней",
                        Weight = 0.10,
                        Severity = "Medium"
                    });
                }
            }

            // === ФАКТОР 10: Частота смены контрагентов ===
            if (input.SenderAccountId > 0)
            {
                int uniqueCounterparties = GetUniqueCounterpartiesLast7Days(input.SenderAccountId);
                features["unique_counterparties_7d"] = uniqueCounterparties;
                if (uniqueCounterparties > 10)
                {
                    factors.Add(new RiskFactor
                    {
                        Category = "Поведение",
                        Description = $"Много разных контрагентов за 7 дней: {uniqueCounterparties}",
                        Weight = 0.12,
                        Severity = "Medium"
                    });
                }
            }

            // === ФАКТОР 11: Круглая сумма ===
            features["round_amount"] = IsRoundAmount(input.Amount);
            if (input.Amount > 50_000 && IsRoundAmount(input.Amount))
            {
                factors.Add(new RiskFactor
                {
                    Category = "Паттерн",
                    Description = "Подозрительно круглая крупная сумма",
                    Weight = 0.06,
                    Severity = "Low"
                });
            }

            // === ФАКТОР 12: Комбинированный риск (ночь + крупная сумма) ===
            if (input.TransactionHour >= 0 && input.TransactionHour < 5 && input.Amount > 100_000)
            {
                factors.Add(new RiskFactor
                {
                    Category = "Комбинация",
                    Description = "Крупная сумма в ночное время — повышенный риск",
                    Weight = 0.15,
                    Severity = "High"
                });
            }

            // === РАСЧЁТ ИТОГОВОГО СКОРА ===
            double rawScore = factors.Sum(f => f.Weight);
            double normalizedScore = 1.0 - (1.0 / (1.0 + rawScore));
            normalizedScore = Math.Min(normalizedScore, 0.99);

            if (factors.Count == 0)
            {
                factors.Add(new RiskFactor
                {
                    Category = "Результат",
                    Description = "Факторов риска не обнаружено",
                    Weight = 0,
                    Severity = "Low"
                });
            }

            var featureJson = Newtonsoft.Json.JsonConvert.SerializeObject(features);

            return new RiskResult
            {
                Score = normalizedScore,
                Level = GetLevel(normalizedScore),
                Factors = factors.OrderByDescending(f => f.Weight).ToList(),
                ModelVersion = "RuleEngine_v2.0",
                FeatureVectorJson = featureJson
            };
        }

        private string GetLevel(double score)
        {
            if (score < 0.20) return "Low";
            if (score < 0.45) return "Medium";
            if (score < 0.70) return "High";
            return "Critical";
        }

        private bool IsRoundAmount(decimal amount)
        {
            return amount % 10000 == 0 || amount % 50000 == 0;
        }

        private double GetAverageTransactionAmount(int accountId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();
                var cmd = new SqlCommand(
                    "SELECT AVG(CAST(Amount AS FLOAT)) FROM Transactions WHERE SenderAccountID = @id",
                    conn);
                cmd.Parameters.AddWithValue("@id", accountId);
                var result = cmd.ExecuteScalar();
                return result == DBNull.Value ? 0 : Convert.ToDouble(result);
            }
            catch { return 0; }
        }

        private int GetUniqueCounterpartiesLast7Days(int accountId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();
                var cmd = new SqlCommand(@"
                    SELECT COUNT(DISTINCT CounterpartyID) FROM Transactions
                    WHERE SenderAccountID = @id
                      AND CounterpartyID IS NOT NULL
                      AND TransactionDate >= DATEADD(DAY, -7, GETDATE())", conn);
                cmd.Parameters.AddWithValue("@id", accountId);
                return (int)cmd.ExecuteScalar();
            }
            catch { return 0; }
        }
    }
}