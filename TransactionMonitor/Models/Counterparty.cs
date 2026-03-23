using System;

namespace TransactionMonitor.Models
{
    public class Counterparty
    {
        public int CounterpartyID { get; set; }
        public string? Name { get; set; }
        public string? TaxID { get; set; }
        public string? ActivityCategory { get; set; }
        public string? RiskLevel { get; set; }
        public bool IsBlacklisted { get; set; }
        public string? CountryOfRegistry { get; set; }
        public DateTime AddedDate { get; set; }
    }
}