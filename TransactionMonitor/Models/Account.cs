namespace TransactionMonitor.Models
{
    public class Account
    {
        public int AccountID { get; set; }
        public int ClientID { get; set; }
        public string? AccountNumber { get; set; }
        public decimal Balance { get; set; }
        public string? Currency { get; set; }
        public string? Status { get; set; }
        public string? AccountType { get; set; }
        public string? OwnerName { get; set; }

        public string BalanceFormatted => Balance.ToString("N2") + " " + Currency;
        public string DisplayName => $"{OwnerName} | {AccountNumber}";
    }
}