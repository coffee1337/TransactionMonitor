using System;

namespace TransactionMonitor.Models
{
    public class RiskScore
    {
        public int ScoreID { get; set; }
        public int TransactionID { get; set; }
        public string? SenderName { get; set; }
        public double Score { get; set; }
        public string? RiskLevel { get; set; }
        public string? ModelVersion { get; set; }
        public DateTime ScoredAt { get; set; }
        public bool IsFraud { get; set; }
        public bool ReviewedByAnalyst { get; set; }
        public string? AnalystComment { get; set; }
    }
}