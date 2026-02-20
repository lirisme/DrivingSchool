using System;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class DrivingLicenseEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        public StudentDrivingLicense LicenseData { get; private set; }
        private readonly int _studentId;
        private readonly string _studentName;

        public DrivingLicenseEditDialog(SqlDataService dataService, int studentId, string studentName, StudentDrivingLicense licenseData = null)
        {
            InitializeComponent();
            _dataService = dataService;
            _studentId = studentId;
            _studentName = studentName;

            if (licenseData != null)
            {
                LicenseData = licenseData;
                Title = "Редактирование водительского удостоверения";
            }
            else
            {
                LicenseData = new StudentDrivingLicense
                {
                    StudentId = studentId,
                    IssueDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddYears(10),
                    Status = "Действительно"
                };
                Title = "Добавление водительского удостоверения";
            }

            DataContext = LicenseData;
            StudentNameTextBox.Text = studentName;

            IssueDatePicker.SelectedDateChanged += IssueDateChanged;
            UpdateExperience();
        }

        private void IssueDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateExperience();

            // По умолчанию срок действия 10 лет
            if (IssueDatePicker.SelectedDate.HasValue)
            {
                LicenseData.ExpiryDate = IssueDatePicker.SelectedDate.Value.AddYears(10);
            }
        }

        private void UpdateExperience()
        {
            if (IssueDatePicker.SelectedDate.HasValue)
            {
                var today = DateTime.Today;
                var issueDate = IssueDatePicker.SelectedDate.Value;

                var years = today.Year - issueDate.Year;
                if (issueDate.Date > today.AddYears(-years))
                {
                    years--;
                }

                LicenseData.ExperienceYears = Math.Max(0, years);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(LicenseData.Series))
            {
                MessageBox.Show("Введите серию удостоверения", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SeriesTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(LicenseData.Number))
            {
                MessageBox.Show("Введите номер удостоверения", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NumberTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(LicenseData.LicenseCateg))
            {
                MessageBox.Show("Введите категории", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoriesTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(LicenseData.IssuedBy))
            {
                MessageBox.Show("Введите кем выдано удостоверение", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                IssuedByTextBox.Focus();
                return;
            }

            if (LicenseData.IssueDate > DateTime.Now)
            {
                MessageBox.Show("Дата выдачи не может быть в будущем", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                IssueDatePicker.Focus();
                return;
            }

            if (LicenseData.ExpiryDate <= LicenseData.IssueDate)
            {
                MessageBox.Show("Дата окончания должна быть позже даты выдачи", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ExpiryDatePicker.Focus();
                return;
            }

            try
            {
                // TODO: Сохранить через сервис
                // _dataService.SaveDrivingLicense(LicenseData);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}