using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class EmployeesPage : Page
    {
        private readonly SqlDataService _dataService;
        private EmployeeCollection _employees;
        private EmployeeCollection _filteredEmployees;

        public EmployeesPage(SqlDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            LoadEmployees();
        }

        private void LoadEmployees()
        {
            try
            {
                _employees = _dataService.LoadEmployees();
                ApplyFilter();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _employees = new EmployeeCollection { Employees = new System.Collections.Generic.List<Employee>() };
                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            if (_employees?.Employees == null)
            {
                _filteredEmployees = new EmployeeCollection { Employees = new System.Collections.Generic.List<Employee>() };
            }
            else
            {
                var searchText = SearchTextBox?.Text?.ToLower() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    _filteredEmployees = new EmployeeCollection { Employees = _employees.Employees.ToList() };
                }
                else
                {
                    _filteredEmployees = new EmployeeCollection
                    {
                        Employees = _employees.Employees
                            .Where(e => (e.LastName ?? "").ToLower().Contains(searchText) ||
                                       (e.FirstName ?? "").ToLower().Contains(searchText) ||
                                       (e.MiddleName ?? "").ToLower().Contains(searchText) ||
                                       (e.Position ?? "").ToLower().Contains(searchText) ||
                                       (e.Phone ?? "").Contains(searchText) ||
                                       (e.Email ?? "").ToLower().Contains(searchText))
                            .ToList()
                    };
                }
            }

            EmployeesGrid.ItemsSource = _filteredEmployees.Employees;
            UpdateButtonsAvailability();
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            var totalCount = _employees?.Employees?.Count ?? 0;
            var filteredCount = _filteredEmployees?.Employees?.Count ?? 0;

            if (filteredCount == totalCount)
            {
                StatusTextBlock.Text = $"Всего сотрудников: {totalCount}";
            }
            else
            {
                StatusTextBlock.Text = $"Найдено сотрудников: {filteredCount} из {totalCount}";
            }
        }

        private void UpdateButtonsAvailability()
        {
            var isSelected = EmployeesGrid.SelectedItem != null;
            EditEmployeeButton.IsEnabled = isSelected;
            DeleteEmployeeButton.IsEnabled = isSelected;
            ViewEmployeeButton.IsEnabled = isSelected;
        }

        private void EmployeesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsAvailability();
        }

        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new EmployeeEditDialog(_dataService);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    LoadEmployees();
                    MessageBox.Show("Сотрудник успешно добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (!(EmployeesGrid.SelectedItem is Employee selectedEmployee))
            {
                MessageBox.Show("Выберите сотрудника для редактирования", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dialog = new EmployeeEditDialog(_dataService, selectedEmployee);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    LoadEmployees();
                    MessageBox.Show("Данные сотрудника обновлены!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (!(EmployeesGrid.SelectedItem is Employee selectedEmployee))
            {
                MessageBox.Show("Выберите сотрудника для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Проверяем, можно ли удалить сотрудника
                if (MessageBox.Show($"Удалить сотрудника {selectedEmployee.FullName}?\n\n" +
                                   "При удалении сотрудника все связанные записи (автомобили, студенты) будут обновлены.",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    bool deleted = _dataService.DeleteEmployee(selectedEmployee.Id);

                    if (deleted)
                    {
                        LoadEmployees();
                        MessageBox.Show("Сотрудник удален.", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Не удалось удалить сотрудника. Возможно, он является инструктором и назначен на автомобили или студентов.",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (!(EmployeesGrid.SelectedItem is Employee selectedEmployee))
            {
                MessageBox.Show("Выберите сотрудника для просмотра", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var experience = CalculateExperience(selectedEmployee.HireDate);

            MessageBox.Show(
                $"Информация о сотруднике:\n\n" +
                $"ФИО: {selectedEmployee.FullName}\n" +
                $"Должность: {selectedEmployee.Position}\n" +
                $"Статус: {selectedEmployee.Status}\n" +
                $"Телефон: {selectedEmployee.Phone}\n" +
                $"Email: {selectedEmployee.Email ?? "не указан"}\n" +
                $"Дата приема: {selectedEmployee.HireDate:dd.MM.yyyy}\n" +
                $"Стаж: {experience}",
                $"Данные сотрудника: {selectedEmployee.FullName}",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private string CalculateExperience(DateTime hireDate)
        {
            var today = DateTime.Today;
            var years = today.Year - hireDate.Year;
            if (hireDate.Date > today.AddYears(-years)) years--;

            var months = today.Month - hireDate.Month;
            if (months < 0) months += 12;

            if (years > 0)
                return $"{years} г. {months} мес.";
            else
                return $"{months} мес.";
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            ApplyFilter();
        }

        private void EmployeesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (EmployeesGrid.SelectedItem != null)
            {
                EditEmployee_Click(sender, e);
            }
        }
    }
}