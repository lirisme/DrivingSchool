using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class CarsPage : Page
    {
        private readonly SqlDataService _dataService;
        private CarCollection _cars;
        private EmployeeCollection _employees;
        private List<Car> _filteredCars;
        private bool _showActiveOnly = true;

        public CarsPage(SqlDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            LoadCars();
        }

        private void LoadCars()
        {
            try
            {
                _cars = _dataService.LoadCars();
                _employees = _dataService.LoadEmployees();
                ApplyFilter();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _cars = new CarCollection { Cars = new List<Car>() };
                _employees = new EmployeeCollection { Employees = new List<Employee>() };
                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            if (_cars?.Cars == null)
            {
                _filteredCars = new List<Car>();
            }
            else
            {
                var searchText = SearchTextBox?.Text?.ToLower() ?? string.Empty;

                var filtered = _cars.Cars.ToList();

                if (_showActiveOnly)
                {
                    filtered = filtered.Where(c => c.IsActive).ToList();
                }

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    filtered = filtered
                        .Where(c => (c.Brand ?? "").ToLower().Contains(searchText) ||
                                   (c.Model ?? "").ToLower().Contains(searchText) ||
                                   (c.LicensePlate ?? "").ToLower().Contains(searchText) ||
                                   (c.VIN ?? "").ToLower().Contains(searchText))
                        .ToList();
                }

                foreach (var car in filtered)
                {
                    var instructor = _employees.Employees.FirstOrDefault(e => e.Id == car.InstructorId);
                    car.InstructorName = instructor != null ? instructor.FullName : "Не назначен";
                }

                _filteredCars = filtered;
            }

            CarsGrid.ItemsSource = _filteredCars;
            UpdateButtons();
        }

        private void UpdateStatus()
        {
            var totalCount = _cars?.Cars?.Count ?? 0;
            var filteredCount = _filteredCars?.Count ?? 0;
            var activeCount = _cars?.Cars?.Count(c => c.IsActive) ?? 0;

            if (_showActiveOnly)
            {
                StatusTextBlock.Text = $"Активных ТС: {filteredCount} из {totalCount}";
            }
            else
            {
                StatusTextBlock.Text = $"Всего ТС: {totalCount} (активных: {activeCount})";
            }
        }

        private void UpdateButtons()
        {
            if (_showActiveOnly)
            {
                ActiveButton.Background = System.Windows.Media.Brushes.Green;
                InactiveButton.Background = System.Windows.Media.Brushes.Gray;
            }
            else
            {
                ActiveButton.Background = System.Windows.Media.Brushes.Gray;
                InactiveButton.Background = System.Windows.Media.Brushes.Blue;
            }

            var isSelected = CarsGrid.SelectedItem != null;
            EditCarButton.IsEnabled = isSelected;
            DeleteCarButton.IsEnabled = isSelected;
        }

        private void CarsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtons();
        }

        private void AddCar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new CarEditDialog(_dataService);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    LoadCars();
                    MessageBox.Show("ТС успешно добавлено!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditCar_Click(object sender, RoutedEventArgs e)
        {
            if (!(CarsGrid.SelectedItem is Car selectedCar))
            {
                MessageBox.Show("Выберите ТС для редактирования", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dialog = new CarEditDialog(_dataService, selectedCar);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    LoadCars();
                    MessageBox.Show("ТС успешно обновлено!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteCar_Click(object sender, RoutedEventArgs e)
        {
            if (!(CarsGrid.SelectedItem is Car selectedCar))
            {
                MessageBox.Show("Выберите ТС для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var students = _dataService.LoadStudents();
                var studentsWithCar = students.Students.Where(s => s.CarId == selectedCar.Id).ToList();

                if (studentsWithCar.Any())
                {
                    var studentNames = string.Join("\n", studentsWithCar.Take(3).Select(s => s.FullName));
                    if (studentsWithCar.Count > 3)
                    {
                        studentNames += $"\nи ещё {studentsWithCar.Count - 3} студентов";
                    }

                    MessageBox.Show($"Нельзя удалить ТС, так как оно назначено студентам:\n\n{studentNames}\n\n" +
                                   "Сначала измените ТС у этих студентов.", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (MessageBox.Show($"Удалить ТС {selectedCar.Brand} {selectedCar.Model} ({selectedCar.LicensePlate})?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    bool deleted = _dataService.DeleteCar(selectedCar.Id);

                    if (deleted)
                    {
                        LoadCars();
                        MessageBox.Show("ТС удалено.", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Не удалось удалить ТС. Возможно, оно используется.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowActive_Click(object sender, RoutedEventArgs e)
        {
            _showActiveOnly = true;
            ApplyFilter();
            UpdateStatus();
            UpdateButtons();
        }

        private void ShowInactive_Click(object sender, RoutedEventArgs e)
        {
            _showActiveOnly = false;
            ApplyFilter();
            UpdateStatus();
            UpdateButtons();
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

        private void CarsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CarsGrid.SelectedItem != null)
            {
                EditCar_Click(sender, e);
            }
        }
    }
}