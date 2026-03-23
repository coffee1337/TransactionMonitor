using Microsoft.UI.Xaml;
using TransactionMonitor.Views;

namespace TransactionMonitor
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            RootFrame.Navigate(typeof(LoginPage));
        }
    }
}