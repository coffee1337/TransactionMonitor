using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Linq;
using TransactionMonitor.Models;
using TransactionMonitor.Services;

namespace TransactionMonitor.Views
{
    public sealed partial class MainShellPage : Page
    {
        private readonly DatabaseService _db = new DatabaseService();
        private List<AlertItem> _alerts = new();

        public MainShellPage()
        {
            this.InitializeComponent();
            SetupUser();
            ContentFrame.Navigate(typeof(DashboardPage));
            NavView.SelectedItem = NavView.MenuItems[0];
            LoadAlerts();
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

        private void LoadAlerts()
        {
            _alerts = _db.GetAlerts();

            AlertsPanel.Children.Clear();
            AlertsPanel.Visibility = _alerts.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            if (_alerts.Count > 0)
            {
                AlertBadge.Value = _alerts.Count;
                AlertBadge.Visibility = Visibility.Visible;
                AlertIcon.Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 244, 67, 54));
            }
            else
            {
                AlertBadge.Visibility = Visibility.Collapsed;
            }

            int shown = 0;
            foreach (var alert in _alerts.Take(3))
            {
                var bar = new InfoBar
                {
                    IsOpen = true,
                    IsClosable = true,
                    Severity = alert.RiskLevel == "Critical" ? InfoBarSeverity.Error : InfoBarSeverity.Warning,
                    Title = $"Транзакция #{alert.TransactionID} — {alert.RiskLevel}",
                    Message = $"{alert.ClientName} | {alert.Amount:N0} RUB | Риск: {(alert.RiskScore * 100):F0}% | {alert.ScoredAt:dd.MM.yyyy HH:mm}"
                };

                var reviewBtn = new Button
                {
                    Content = "Проверено",
                    Tag = alert.ScoreID,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                reviewBtn.Click += ReviewAlert_Click;

                if (SessionService.CanCreate)
                    bar.ActionButton = reviewBtn;

                bar.Closed += (s, e) => { };

                AlertsPanel.Children.Add(bar);
                shown++;
            }

            if (_alerts.Count > 3)
            {
                var moreText = new TextBlock
                {
                    Text = $"... и ещё {_alerts.Count - 3} непроверенных",
                    Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                    FontSize = 12,
                    Margin = new Thickness(12, 4, 0, 4)
                };
                AlertsPanel.Children.Add(moreText);
            }
        }

        private void ReviewAlert_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int scoreId)
            {
                _db.MarkAsReviewed(scoreId, "Проверено аналитиком");
                LoadAlerts();
            }
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
                case "calculator":
                    ContentFrame.Navigate(typeof(RiskCalculatorPage));
                    PageTitle.Text = "ML-калькулятор";
                    break;
                case "charts":
                    ContentFrame.Navigate(typeof(ChartsPage));
                    PageTitle.Text = "Графики";
                    break;
                case "reports":
                    ContentFrame.Navigate(typeof(ReportsPage));
                    PageTitle.Text = "Отчёты";
                    break;
                case "alerts":
                    LoadAlerts();
                    ContentFrame.Navigate(typeof(RiskScoresPage));
                    PageTitle.Text = "Оценки риска";
                    break;
                case "logout":
                    SessionService.Logout();
                    Frame.Navigate(typeof(LoginPage));
                    break;
            }
        }
    }
}