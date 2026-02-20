using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class StudentEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        public Student StudentData { get; private set; }

        public StudentEditDialog(SqlDataService dataService, Student studentData = null)
        {
            InitializeComponent();
            _dataService = dataService;

            if (studentData != null)
            {
                StudentData = studentData;
                Title = "Редактирование учащегося";
            }
            else
            {
                StudentData = new Student
                {
                    BirthDate = DateTime.Now.AddYears(-18),
                    Citizenship = "Российская Федерация",
                    Gender = "Мужской"
                };
                Title = "Добавление учащегося";
            }

            DataContext = StudentData;
            LoadGroups();
            LoadCategories();
            LoadInstructors();
            LoadCars();
            BirthDatePicker.SelectedDateChanged += BirthDateChanged;
        }

        private void LoadGroups()
        {
            try
            {
                var groups = _dataService.LoadStudyGroups();
                var groupsList = groups.Groups.ToList();
                groupsList.Insert(0, new StudyGroup { Id = 0, Name = "Без группы" });

                GroupComboBox.ItemsSource = groupsList;
                GroupComboBox.DisplayMemberPath = "Name";
                GroupComboBox.SelectedValuePath = "Id";

                if (StudentData.GroupId > 0)
                {
                    GroupComboBox.SelectedValue = StudentData.GroupId;
                }
                else
                {
                    GroupComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки групп: {ex.Message}");
                GroupComboBox.ItemsSource = new[] { new { Id = 0, Name = "Ошибка загрузки" } };
            }
        }

        private void LoadCategories()
        {
            try
            {
                var categories = _dataService.LoadVehicleCategories();
                var categoriesList = categories.Categories.ToList();

                CategoryComboBox.ItemsSource = categoriesList;
                CategoryComboBox.DisplayMemberPath = "DisplayText";
                CategoryComboBox.SelectedValuePath = "Id";

                if (StudentData.VehicleCategoryId > 0)
                {
                    CategoryComboBox.SelectedValue = StudentData.VehicleCategoryId;
                }
                else if (categoriesList.Any())
                {
                    CategoryComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки категорий: {ex.Message}");
                CategoryComboBox.ItemsSource = new[] { new { Id = 0, DisplayText = "Ошибка загрузки" } };
            }
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

                instructors.Insert(0, new Employee
                {
                    Id = 0,
                    LastName = "Не",
                    FirstName = "назначен",
                    MiddleName = ""
                });

                InstructorComboBox.ItemsSource = instructors;
                InstructorComboBox.DisplayMemberPath = "FullName";
                InstructorComboBox.SelectedValuePath = "Id";

                if (StudentData.InstructorId > 0)
                {
                    InstructorComboBox.SelectedValue = StudentData.InstructorId;
                }
                else
                {
                    InstructorComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки инструкторов: {ex.Message}");
                InstructorComboBox.ItemsSource = new[] { new { Id = 0, FullName = "Ошибка загрузки" } };
            }
        }

        private void LoadCars()
        {
            try
            {
                var cars = _dataService.LoadCars();
                var activeCars = cars.Cars.Where(c => c.IsActive).ToList();
                activeCars.Insert(0, new Car { Id = 0, Brand = "Не", Model = "назначен" });

                CarComboBox.ItemsSource = activeCars;
                CarComboBox.DisplayMemberPath = "DisplayText";
                CarComboBox.SelectedValuePath = "Id";

                if (StudentData.CarId > 0)
                {
                    CarComboBox.SelectedValue = StudentData.CarId;
                }
                else
                {
                    CarComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки автомобилей: {ex.Message}");
                CarComboBox.ItemsSource = new[] { new { Id = 0, DisplayText = "Ошибка загрузки" } };
            }
        }

        private void BirthDateChanged(object sender, SelectionChangedEventArgs e)
        {
            // Обновляем возраст
            var binding = AgeTextBox?.GetBindingExpression(TextBox.TextProperty);
            binding?.UpdateTarget();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(StudentData.LastName))
            {
                MessageBox.Show("Введите фамилию учащегося", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                LastNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(StudentData.FirstName))
            {
                MessageBox.Show("Введите имя учащегося", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                FirstNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(StudentData.Phone))
            {
                MessageBox.Show("Введите телефон учащегося", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PhoneTextBox.Focus();
                return;
            }

            if (StudentData.BirthDate > DateTime.Now.AddYears(-16))
            {
                MessageBox.Show("Учащийся должен быть старше 16 лет", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                BirthDatePicker.Focus();
                return;
            }

            if (StudentData.VehicleCategoryId == 0)
            {
                MessageBox.Show("Выберите категорию транспортного средства", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoryComboBox.Focus();
                return;
            }

            try
            {
                _dataService.SaveStudent(StudentData);
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