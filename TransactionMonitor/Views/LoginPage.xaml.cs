using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using TransactionMonitor.Services;

namespace TransactionMonitor.Views
{
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            this.InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            DoLogin();
        }

        private void Input_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
                DoLogin();
        }

        private void DoLogin()
        {
            var username = UsernameBox.Text.Trim();
            var password = PasswordBox.Password;

            if (SessionService.Login(username, password))
            {
                ErrorBorder.Visibility = Visibility.Collapsed;
                Frame.Navigate(typeof(MainShellPage));
            }
            else
            {
                ErrorBorder.Visibility = Visibility.Visible;
                ErrorText.Text = "Неверный логин или пароль";
                PasswordBox.Password = "";
            }
        }
    }
}