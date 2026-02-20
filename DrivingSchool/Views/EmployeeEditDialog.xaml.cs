using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class EmployeeEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        public Employee EmployeeData { get; private set; }

        public EmployeeEditDialog(SqlDataService dataService, Employee employeeData = null)
        {
            InitializeComponent();
            _dataService = dataService;

            if (employeeData != null)
            {
                EmployeeData = employeeData;
                Title = "Редактирование сотрудника";
            }
            else
            {
                EmployeeData = new Employee
                {
                    HireDate = DateTime.Now,
                    Status = "Активен"
                };
                Title = "Добавление сотрудника";
            }

            DataContext = EmployeeData;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(EmployeeData.LastName))
            {
                MessageBox.Show("Введите фамилию сотрудника", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                LastNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(EmployeeData.FirstName))
            {
                MessageBox.Show("Введите имя сотрудника", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                FirstNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(EmployeeData.Position))
            {
                MessageBox.Show("Введите должность сотрудника", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PositionComboBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(EmployeeData.Phone))
            {
                MessageBox.Show("Введите телефон сотрудника", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PhoneTextBox.Focus();
                return;
            }

            if (EmployeeData.HireDate > DateTime.Now)
            {
                MessageBox.Show("Дата приема не может быть в будущем", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                HireDatePicker.Focus();
                return;
            }

            try
            {
                _dataService.SaveEmployee(EmployeeData);
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