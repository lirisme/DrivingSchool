using System;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public abstract class BasePage : Page
    {
        // Важно! Поле должно быть protected
        protected readonly SqlDataService _dataService;

        public BasePage(SqlDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        }

        protected void ShowError(string message)
        {
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        protected void ShowWarning(string message)
        {
            MessageBox.Show(message, "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        protected void ShowInfo(string message)
        {
            MessageBox.Show(message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        protected bool ShowConfirmation(string message)
        {
            return MessageBox.Show(message, "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        protected void UpdateStatus(string status)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null && mainWindow.StatusText != null)
            {
                mainWindow.StatusText.Text = status;
            }
        }
    }
}