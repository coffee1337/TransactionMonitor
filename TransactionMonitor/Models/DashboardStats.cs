namespace TransactionMonitor.Models
{
    public class DashboardStats
    {
        public int TotalClients { get; set; }
        public int BlockedClients { get; set; }
        public int TotalTransactions { get; set; }
        public int FraudCount { get; set; }
        public int TotalAccounts { get; set; }
        public int ActiveAccounts { get; set; }
        public int FrozenAccounts { get; set; }
        public int TotalCounterparties { get; set; }
        public int BlacklistedCounterparties { get; set; }
        public decimal TotalAmount { get; set; }
        public double AvgRiskScore { get; set; }
        public int UnreviewedHighRisk { get; set; }
    }
}