using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;
using DrivingSchool.Services;
using System.Collections.Generic;
using System.Globalization;

namespace DrivingSchool.Views
{
    public partial class StudentEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        private TariffCollection _tariffs;
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
                    Gender = "Мужской",
                    TuitionAmount = 0,
                    DiscountAmount = 0
                };
                Title = "Добавление учащегося";
            }

            DataContext = StudentData;
            LoadGroups();
            LoadCategories();
            LoadTariffs();
            LoadInstructors();
            LoadCars();

            // Подписываемся на событие выбора категории
            CategoryComboBox.SelectionChanged += CategoryComboBox_SelectionChanged;
            // Подписываемся на событие выбора тарифа для обновления стоимости
            TariffComboBox.SelectionChanged += TariffComboBox_SelectionChanged;

            BirthDatePicker.SelectedDateChanged += BirthDateChanged;

            // Обновляем отображение итоговой суммы
            UpdateFinalAmount();
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

        private void LoadTariffs()
        {
            try
            {
                _tariffs = _dataService.LoadTariffs();
                var tariffsList = _tariffs.Tariffs.ToList();

                // Добавляем пустой тариф для возможности выбора "Без тарифа"
                tariffsList.Insert(0, new Tariff
                {
                    Id = 0,
                    Name = "Без тарифа",
                    BaseCost = 0,
                    Category = ""
                });

                TariffComboBox.ItemsSource = tariffsList;
                TariffComboBox.DisplayMemberPath = "DisplayText";
                TariffComboBox.SelectedValuePath = "Id";

                if (StudentData.TariffId > 0)
                {
                    // Ищем тариф с таким ID
                    var selectedTariff = tariffsList.FirstOrDefault(t => t.Id == StudentData.TariffId);
                    if (selectedTariff != null)
                    {
                        TariffComboBox.SelectedValue = StudentData.TariffId;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки тарифов: {ex.Message}");
                TariffComboBox.ItemsSource = new[] { new { Id = 0, DisplayText = "Ошибка загрузки" } };
            }
        }

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryComboBox.SelectedItem is VehicleCategory selectedCategory && _tariffs != null)
            {
                // Фильтруем тарифы по выбранной категории
                var filteredTariffs = _tariffs.Tariffs
                    .Where(t => t.Category == selectedCategory.Code || string.IsNullOrEmpty(t.Category))
                    .ToList();

                // Добавляем "Без тарифа" в начало
                filteredTariffs.Insert(0, new Tariff
                {
                    Id = 0,
                    Name = "Без тарифа",
                    BaseCost = 0,
                    Category = ""
                });

                // Сохраняем текущее выделение
                var currentTariffId = StudentData.TariffId;

                TariffComboBox.ItemsSource = filteredTariffs;

                // Пытаемся восстановить выделение, если подходит
                if (currentTariffId > 0)
                {
                    var exists = filteredTariffs.Any(t => t.Id == currentTariffId);
                    if (exists)
                    {
                        TariffComboBox.SelectedValue = currentTariffId;
                    }
                    else
                    {
                        // Если текущий тариф не подходит под категорию, сбрасываем
                        StudentData.TariffId = null;
                        TariffComboBox.SelectedIndex = 0;
                    }
                }
                else
                {
                    TariffComboBox.SelectedIndex = 0;
                }
            }
        }

        private void TariffComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TariffComboBox.SelectedItem is Tariff selectedTariff)
            {
                if (selectedTariff.Id > 0)
                {
                    // Автоматически подставляем стоимость из тарифа
                    StudentData.TuitionAmount = selectedTariff.BaseCost;
                    StudentData.TariffId = selectedTariff.Id;

                    // Обновляем отображение
                    TuitionTextBox.Text = selectedTariff.BaseCost.ToString("F2");
                }
                else
                {
                    // Если выбран "Без тарифа"
                    StudentData.TariffId = null;
                    // Не сбрасываем стоимость, чтобы можно было ввести вручную
                }

                UpdateFinalAmount();
            }
        }

        private void DiscountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFinalAmount();
        }

        private void TuitionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFinalAmount();
        }

        private void UpdateFinalAmount()
        {
            try
            {
                decimal tuition = 0;
                decimal discount = 0;

                decimal.TryParse(TuitionTextBox?.Text, out tuition);
                decimal.TryParse(DiscountTextBox?.Text, out discount);

                StudentData.TuitionAmount = tuition;
                StudentData.DiscountAmount = discount;

                var finalAmount = tuition - discount;
                FinalAmountTextBlock.Text = $"Итого: {finalAmount:N2} руб.";

                if (discount > tuition)
                {
                    FinalAmountTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    WarningTextBlock.Text = "Внимание: Скидка больше стоимости!";
                    WarningTextBlock.Visibility = Visibility.Visible;
                }
                else
                {
                    FinalAmountTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                    WarningTextBlock.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
                FinalAmountTextBlock.Text = "Итого: 0 руб.";
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

            // Обновляем значения из текстовых полей
            if (decimal.TryParse(TuitionTextBox?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal tuition))
                StudentData.TuitionAmount = tuition;

            if (decimal.TryParse(DiscountTextBox?.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal discount))
                StudentData.DiscountAmount = discount;

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