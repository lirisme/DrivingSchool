using DrivingSchool.Models;
using DrivingSchool.Services;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace DrivingSchool.Views
{
    public partial class SNILSEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        public StudentSNILS SNILSData { get; private set; }
        private readonly int _studentId;
        private readonly string _studentName;

        public SNILSEditDialog(SqlDataService dataService, int studentId, string studentName, StudentSNILS snilsData = null)
        {
            InitializeComponent();
            _dataService = dataService;
            _studentId = studentId;
            _studentName = studentName;

            if (snilsData != null)
            {
                SNILSData = snilsData;
                Title = "Редактирование данных СНИЛС";
            }
            else
            {
                SNILSData = new StudentSNILS
                {
                    StudentId = studentId,
                    IssueDate = DateTime.Now
                };
                Title = "Добавление данных СНИЛС";
            }

            DataContext = SNILSData;
            StudentNameTextBox.Text = studentName;
        }

        private string NormalizeSNILSNumber(string number)
        {
            if (string.IsNullOrEmpty(number)) return "";

            // Удаляем все нецифровые символы
            var digits = new string(number.Where(char.IsDigit).ToArray());

            if (digits.Length == 11)
            {
                return $"{digits.Substring(0, 3)}-{digits.Substring(3, 3)}-{digits.Substring(6, 3)} {digits.Substring(9, 2)}";
            }

            return number;
        }

        private bool IsValidSNILS(string number)
        {
            if (string.IsNullOrEmpty(number)) return false;

            // Удаляем все нецифровые символы
            var digits = new string(number.Where(char.IsDigit).ToArray());

            return digits.Length == 11;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(SNILSData.Number))
            {
                MessageBox.Show("Введите номер СНИЛС", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NumberTextBox.Focus();
                return;
            }

            // Нормализуем номер
            SNILSData.Number = NormalizeSNILSNumber(SNILSData.Number);

            if (!IsValidSNILS(SNILSData.Number))
            {
                MessageBox.Show("Номер СНИЛС должен содержать 11 цифр\n\nФормат: 123-456-789 00", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NumberTextBox.Focus();
                return;
            }

            try
            {
                // TODO: Сохранить через сервис
                // _dataService.SaveSNILSData(SNILSData);

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