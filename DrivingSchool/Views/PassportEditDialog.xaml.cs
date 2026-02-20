using System;
using System.Windows;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class PassportEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        public StudentPassportData PassportData { get; private set; }
        private readonly int _studentId;
        private readonly string _studentName;

        public PassportEditDialog(SqlDataService dataService, int studentId, string studentName, StudentPassportData passportData = null)
        {
            InitializeComponent();
            _dataService = dataService;
            _studentId = studentId;
            _studentName = studentName;

            if (passportData != null)
            {
                PassportData = passportData;
                Title = "Редактирование паспортных данных";
            }
            else
            {
                PassportData = new StudentPassportData
                {
                    StudentId = studentId,
                    DocumentType = "Паспорт РФ",
                    IssueDate = DateTime.Now
                };
                Title = "Добавление паспортных данных";
            }

            DataContext = PassportData;
            StudentNameTextBox.Text = studentName;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(PassportData.Series))
            {
                MessageBox.Show("Введите серию паспорта", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SeriesTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(PassportData.Number))
            {
                MessageBox.Show("Введите номер паспорта", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NumberTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(PassportData.IssuedBy))
            {
                MessageBox.Show("Введите кем выдан паспорт", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                IssuedByTextBox.Focus();
                return;
            }

            if (PassportData.IssueDate > DateTime.Now)
            {
                MessageBox.Show("Дата выдачи не может быть в будущем", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                IssueDatePicker.Focus();
                return;
            }

            try
            {
                // Сохраняем через сервис (нужно добавить метод в SqlDataService)
                // _dataService.SavePassportData(PassportData);

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