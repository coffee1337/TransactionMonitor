namespace TransactionMonitor.Models
{
    public class RiskLabel
    {
        public int LabelID { get; set; }
        public string? LabelName { get; set; }
        public string? Description { get; set; }
        public string? Severity { get; set; }
    }
}