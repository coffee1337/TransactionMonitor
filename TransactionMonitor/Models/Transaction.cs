using System;

namespace TransactionMonitor.Models
{
    public class Transaction
    {
        public int TransactionID { get; set; }
        public int SenderAccountID { get; set; }
        public int? ReceiverAcooundID { get; set; }
        public int? CounterpartyID { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? TransactionType { get; set; }
        public string? Status { get; set; }
        public string? Description { get; set; }
        public string? IPAddress { get; set; }

        public string? SenderName { get; set; }
        public string? CounterpartyName { get; set; }
        public string? RiskLevel { get; set; }
        public double? RiskScore { get; set; }

        public string AmountFormatted => Amount.ToString("N2") + " ₽";

    }
}