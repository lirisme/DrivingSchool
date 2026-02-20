using System;
using System.Linq;
using System.Windows;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class CarEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        public Car CarData { get; private set; }

        public CarEditDialog(SqlDataService dataService, Car carData = null)
        {
            InitializeComponent();
            _dataService = dataService;

            if (carData != null)
            {
                CarData = carData;
                Title = "Редактирование автомобиля";
            }
            else
            {
                CarData = new Car
                {
                    IsActive = true,
                    Year = DateTime.Now.Year
                };
                Title = "Добавление автомобиля";
            }

            DataContext = CarData;
            LoadInstructors();
        }

        private void LoadInstructors()
        {
            try
            {
                var employees = _dataService.LoadEmployees();
                var instructors = employees.Employees
                    .Where(e => e.Position != null &&
                               (e.Position.Contains("инструктор") ||
                                e.Position.Contains("Инструктор") ||
                                e.Position.Contains("водитель") ||
                                e.Position.Contains("Водитель")))
                    .ToList();

                // Добавляем пустого инструктора
                instructors.Insert(0, new Employee
                {
                    Id = 0,
                    LastName = "Не",
                    FirstName = "назначен",
                    MiddleName = ""
                });

                InstructorComboBox.ItemsSource = instructors;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки инструкторов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(CarData.Brand))
            {
                MessageBox.Show("Введите марку автомобиля", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                BrandTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(CarData.Model))
            {
                MessageBox.Show("Введите модель автомобиля", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ModelTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(CarData.LicensePlate))
            {
                MessageBox.Show("Введите государственный номер", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                LicensePlateTextBox.Focus();
                return;
            }

            // Проверка года
            if (CarData.Year < 1990 || CarData.Year > DateTime.Now.Year + 1)
            {
                MessageBox.Show($"Год выпуска должен быть от 1990 до {DateTime.Now.Year + 1}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                YearTextBox.Focus();
                return;
            }

            try
            {
                _dataService.SaveCar(CarData);
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