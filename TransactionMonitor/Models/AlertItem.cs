using System;

namespace TransactionMonitor.Models
{
    public class AlertItem
    {
        public int ScoreID { get; set; }
        public int TransactionID { get; set; }
        public double RiskScore { get; set; }
        public string? RiskLevel { get; set; }
        public DateTime ScoredAt { get; set; }
        public decimal Amount { get; set; }
        public string? ClientName { get; set; }
    }
}