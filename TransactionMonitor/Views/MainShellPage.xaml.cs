using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using TransactionMonitor.Models;
using TransactionMonitor.Services;

namespace TransactionMonitor.Views
{
    public sealed partial class MainShellPage : Page
    {
        public MainShellPage()
        {
            this.InitializeComponent();
            SetupUser();
            ContentFrame.Navigate(typeof(DashboardPage));
            NavView.SelectedItem = NavView.MenuItems[0];
        }

        private void SetupUser()
        {
            var user = SessionService.CurrentUser;
            if (user == null) return;

            UserNameText.Text = user.FullName;
            RoleText.Text = user.RoleDisplayName;

            RoleBadge.Background = user.Role switch
            {
                UserRole.Admin => new SolidColorBrush(ColorHelper.FromArgb(40, 220, 53, 69)),
                UserRole.Analyst => new SolidColorBrush(ColorHelper.FromArgb(40, 255, 193, 7)),
                UserRole.Operator => new SolidColorBrush(ColorHelper.FromArgb(40, 40, 167, 69)),
                _ => new SolidColorBrush(ColorHelper.FromArgb(40, 128, 128, 128))
            };

            RoleText.Foreground = user.Role switch
            {
                UserRole.Admin => new SolidColorBrush(ColorHelper.FromArgb(255, 255, 100, 100)),
                UserRole.Analyst => new SolidColorBrush(ColorHelper.FromArgb(255, 255, 200, 0)),
                UserRole.Operator => new SolidColorBrush(ColorHelper.FromArgb(255, 100, 220, 100)),
                _ => new SolidColorBrush(ColorHelper.FromArgb(255, 180, 180, 180))
            };
        }

        private void NavView_SelectionChanged(NavigationView sender,
            NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is not NavigationViewItem item) return;

            switch (item.Tag?.ToString())
            {
                case "dashboard":
                    ContentFrame.Navigate(typeof(DashboardPage));
                    PageTitle.Text = "Дашборд";
                    break;
                case "clients":
                    ContentFrame.Navigate(typeof(ClientsPage));
                    PageTitle.Text = "Клиенты";
                    break;
                case "accounts":
                    ContentFrame.Navigate(typeof(AccountsPage));
                    PageTitle.Text = "Счета";
                    break;
                case "transactions":
                    ContentFrame.Navigate(typeof(TransactionsPage));
                    PageTitle.Text = "Транзакции";
                    break;
                case "counterparties":
                    ContentFrame.Navigate(typeof(CounterpartiesPage));
                    PageTitle.Text = "Контрагенты";
                    break;
                case "risks":
                    ContentFrame.Navigate(typeof(RiskScoresPage));
                    PageTitle.Text = "Оценки риска";
                    break;
                case "labels":
                    ContentFrame.Navigate(typeof(RiskLabelsPage));
                    PageTitle.Text = "Метки риска";
                    break;
                case "reports":
                    ContentFrame.Navigate(typeof(ReportsPage));
                    PageTitle.Text = "Отчёты";
                    break;
                case "calculator":
                    ContentFrame.Navigate(typeof(RiskCalculatorPage));
                    PageTitle.Text = "ML-калькулятор";
                    break;
                case "charts":
                    ContentFrame.Navigate(typeof(ChartsPage));
                    PageTitle.Text = "Графики";
                    break;
                case "logout":
                    SessionService.Logout();
                    Frame.Navigate(typeof(LoginPage));
                    break;
            }
        }
    }
}