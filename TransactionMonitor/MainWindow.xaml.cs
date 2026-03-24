using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using TransactionMonitor.Views;

namespace TransactionMonitor
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "Transaction Monitor — Мониторинг транзакций";
            this.ExtendsContentIntoTitleBar = false;
            TrySetMicaBackdrop();
            RootFrame.Navigate(typeof(LoginPage));
        }

        private void TrySetMicaBackdrop()
        {
            if (MicaController.IsSupported())
            {
                this.SystemBackdrop = new MicaBackdrop { Kind = MicaKind.Base };
            }
            else if (DesktopAcrylicController.IsSupported())
            {
                this.SystemBackdrop = new DesktopAcrylicBackdrop();
            }
        }
    }
}