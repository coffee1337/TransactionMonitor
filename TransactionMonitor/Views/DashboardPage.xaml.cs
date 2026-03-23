using Microsoft.UI.Xaml.Controls;
using System.Linq;
using TransactionMonitor.Services;

namespace TransactionMonitor.Views
{
    public sealed partial class DashboardPage : Page
    {
        private readonly DatabaseService _db = new DatabaseService();

        public DashboardPage()
        {
            this.InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            var stats = _db.GetDashboardStats();

            TotalClientsText.Text = stats.totalClients.ToString();
            BlockedClientsText.Text = "заблокировано: " + stats.blockedClients.ToString();
            TotalTransactionsText.Text = stats.totalTransactions.ToString();
            FraudCountText.Text = stats.fraudCount.ToString();
            BlockedText.Text = stats.blockedClients.ToString();

            var transactions = _db.GetTransactions().Take(10).ToList();
            RecentTransactionsList.ItemsSource = transactions;
        }
    }
}