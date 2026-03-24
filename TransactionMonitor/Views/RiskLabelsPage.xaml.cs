using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using TransactionMonitor.Models;
using TransactionMonitor.Services;

namespace TransactionMonitor.Views
{
    public sealed partial class RiskLabelsPage : Page
    {
        private readonly DatabaseService _db = new DatabaseService();
        private List<RiskLabelViewModel> _all = new();

        public RiskLabelsPage()
        {
            this.InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            _all = _db.GetRiskLabels().Select(l => new RiskLabelViewModel(l)).ToList();
            LabelsList.ItemsSource = _all;
            if (!SessionService.CanDelete)
            {
                // Кнопки удаления скрываются через ViewModel
            }

            TotalText.Text = _all.Count.ToString();
            HighText.Text = _all.Count(l => l.Severity == "High").ToString();
            MediumText.Text = _all.Count(l => l.Severity == "Medium").ToString();
            LowText.Text = _all.Count(l => l.Severity == "Low").ToString();
            AddLabelButton.Visibility = SessionService.CanCreate
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool _dialogOpen = false;

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            if (_dialogOpen) return;
            _dialogOpen = true;
            await ShowEditDialog(null);
            _dialogOpen = false;
        }

        private async void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (_dialogOpen) return;
            if ((sender as Button)?.Tag is not RiskLabelViewModel label) return;
            _dialogOpen = true;
            await ShowEditDialog(label);
            _dialogOpen = false;
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (_dialogOpen) return;
            if ((sender as Button)?.Tag is not RiskLabelViewModel label) return;
            _dialogOpen = true;

            var dialog = new ContentDialog
            {
                Title = "Удалить метку?",
                Content = $"Вы уверены, что хотите удалить метку \"{label.LabelName}\"?",
                PrimaryButtonText = "Удалить",
                CloseButtonText = "Отмена",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                _db.DeleteRiskLabel(label.LabelID);
                LoadData();
            }
            _dialogOpen = false;
        }

        private async System.Threading.Tasks.Task ShowEditDialog(RiskLabelViewModel? existing)
        {
            var nameBox = new TextBox
            {
                PlaceholderText = "Название метки",
                Text = existing?.LabelName ?? "",
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var descBox = new TextBox
            {
                PlaceholderText = "Описание",
                Text = existing?.Description ?? "",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 80
            };

            var severityCombo = new ComboBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            severityCombo.Items.Add("Low");
            severityCombo.Items.Add("Medium");
            severityCombo.Items.Add("High");
            severityCombo.SelectedItem = existing?.Severity ?? "Medium";

            var form = new StackPanel { Spacing = 12, MinWidth = 360 };
            form.Children.Add(new TextBlock { Text = "Название" });
            form.Children.Add(nameBox);
            form.Children.Add(new TextBlock { Text = "Описание" });
            form.Children.Add(descBox);
            form.Children.Add(new TextBlock { Text = "Серьёзность" });
            form.Children.Add(severityCombo);

            var dialog = new ContentDialog
            {
                Title = existing == null ? "Новая метка" : "Редактировать метку",
                Content = form,
                PrimaryButtonText = existing == null ? "Создать" : "Сохранить",
                CloseButtonText = "Отмена",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var name = nameBox.Text.Trim();
                var desc = descBox.Text.Trim();
                var severity = severityCombo.SelectedItem?.ToString() ?? "Medium";

                if (string.IsNullOrEmpty(name)) return;

                if (existing == null)
                    _db.CreateRiskLabel(name, desc, severity);
                else
                    _db.UpdateRiskLabel(existing.LabelID, name, desc, severity);

                LoadData();
            }
        }
    }

    public class RiskLabelViewModel
    {
        public int LabelID { get; set; }
        public string? LabelName { get; set; }
        public string? Description { get; set; }
        public string? Severity { get; set; }

        public string IdText => $"#{LabelID}";

        public SolidColorBrush SeverityBackground => Severity switch
        {
            "High" => new SolidColorBrush(ColorHelper.FromArgb(30, 244, 67, 54)),
            "Medium" => new SolidColorBrush(ColorHelper.FromArgb(30, 255, 152, 0)),
            "Low" => new SolidColorBrush(ColorHelper.FromArgb(30, 76, 175, 80)),
            _ => new SolidColorBrush(ColorHelper.FromArgb(30, 128, 128, 128))
        };

        public SolidColorBrush SeverityForeground => Severity switch
        {
            "High" => new SolidColorBrush(ColorHelper.FromArgb(255, 244, 67, 54)),
            "Medium" => new SolidColorBrush(ColorHelper.FromArgb(255, 255, 152, 0)),
            "Low" => new SolidColorBrush(ColorHelper.FromArgb(255, 76, 175, 80)),
            _ => new SolidColorBrush(ColorHelper.FromArgb(255, 180, 180, 180))
        };

        public RiskLabelViewModel(RiskLabel l)
        {
            LabelID = l.LabelID;
            LabelName = l.LabelName;
            Description = l.Description;
            Severity = l.Severity;
        }
        public Visibility DeleteVisible => SessionService.CanDelete
            ? Visibility.Visible : Visibility.Collapsed;
    }
}