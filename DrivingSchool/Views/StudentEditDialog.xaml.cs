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
        private bool _isDirty = false; // Флаг наличия несохраненных изменений

        public event EventHandler<StudentSavedEventArgs> StudentSaved;

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

            CategoryComboBox.SelectionChanged += CategoryComboBox_SelectionChanged;
            TariffComboBox.SelectionChanged += TariffComboBox_SelectionChanged;
            BirthDatePicker.SelectedDateChanged += BirthDateChanged;

            UpdateFinalAmount();

            // Подписываемся на изменения во всех полях
            SubscribeToChanges();
        }

        // Подписка на изменения во всех полях
        private void SubscribeToChanges()
        {
            // Текстовые поля
            LastNameTextBox.TextChanged += (s, e) => _isDirty = true;
            FirstNameTextBox.TextChanged += (s, e) => _isDirty = true;
            MiddleNameTextBox.TextChanged += (s, e) => _isDirty = true;
            BirthPlaceTextBox.TextChanged += (s, e) => _isDirty = true;
            PhoneTextBox.TextChanged += (s, e) => _isDirty = true;
            EmailTextBox.TextChanged += (s, e) => _isDirty = true;
            CitizenshipTextBox.TextChanged += (s, e) => _isDirty = true;
            TuitionTextBox.TextChanged += (s, e) => _isDirty = true;
            DiscountTextBox.TextChanged += (s, e) => _isDirty = true;

            // ComboBox
            GroupComboBox.SelectionChanged += (s, e) => _isDirty = true;
            CategoryComboBox.SelectionChanged += (s, e) => _isDirty = true;
            TariffComboBox.SelectionChanged += (s, e) => _isDirty = true;
            InstructorComboBox.SelectionChanged += (s, e) => _isDirty = true;
            CarComboBox.SelectionChanged += (s, e) => _isDirty = true;
            GenderComboBox.SelectionChanged += (s, e) => _isDirty = true;

            // DatePicker
            BirthDatePicker.SelectedDateChanged += (s, e) => _isDirty = true;
        }

        // Обработка закрытия окна
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_isDirty)
            {
                var result = MessageBox.Show(
                    "Есть несохраненные изменения. Сохранить перед закрытием?",
                    "Подтверждение закрытия",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Сохраняем
                    if (SaveStudent())
                    {
                        DialogResult = true;
                        base.OnClosing(e);
                    }
                    else
                    {
                        e.Cancel = true; // Не закрываем, если ошибка сохранения
                    }
                }
                else if (result == MessageBoxResult.No)
                {
                    // Закрываем без сохранения
                    DialogResult = false;
                    base.OnClosing(e);
                }
                else // Cancel
                {
                    e.Cancel = true; // Не закрываем
                }
            }
            else
            {
                base.OnClosing(e);
            }
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
                TariffComboBox.SelectionChanged -= TariffComboBox_SelectionChanged;

                try
                {
                    var filteredTariffs = _tariffs.Tariffs
                        .Where(t => t.Category == selectedCategory.Code || string.IsNullOrEmpty(t.Category))
                        .ToList();

                    filteredTariffs.Insert(0, new Tariff
                    {
                        Id = 0,
                        Name = "Без тарифа",
                        BaseCost = 0,
                        Category = ""
                    });

                    var currentTariffId = StudentData.TariffId;

                    TariffComboBox.ItemsSource = filteredTariffs;

                    if (currentTariffId.HasValue && currentTariffId.Value > 0)
                    {
                        var exists = filteredTariffs.Any(t => t.Id == currentTariffId.Value);
                        if (exists)
                        {
                            TariffComboBox.SelectedValue = currentTariffId.Value;
                        }
                        else
                        {
                            StudentData.TariffId = null;
                            TariffComboBox.SelectedIndex = 0;
                        }
                    }
                    else
                    {
                        TariffComboBox.SelectedIndex = 0;
                    }
                }
                finally
                {
                    TariffComboBox.SelectionChanged += TariffComboBox_SelectionChanged;
                }
            }
        }

        private void TariffComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TariffComboBox.SelectedItem is Tariff selectedTariff)
            {
                if (selectedTariff.Id > 0)
                {
                    StudentData.TuitionAmount = selectedTariff.BaseCost;
                    StudentData.TariffId = selectedTariff.Id;
                    TuitionTextBox.Text = selectedTariff.BaseCost.ToString("F2", CultureInfo.InvariantCulture);
                }
                else
                {
                    StudentData.TariffId = null;
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

                if (!string.IsNullOrEmpty(TuitionTextBox?.Text))
                    decimal.TryParse(TuitionTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out tuition);

                if (!string.IsNullOrEmpty(DiscountTextBox?.Text))
                    decimal.TryParse(DiscountTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out discount);

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
            var binding = AgeTextBox?.GetBindingExpression(TextBox.TextProperty);
            binding?.UpdateTarget();
        }

        private bool ValidateStudent()
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(StudentData.LastName))
            {
                MessageBox.Show("Введите фамилию учащегося", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                LastNameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(StudentData.FirstName))
            {
                MessageBox.Show("Введите имя учащегося", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                FirstNameTextBox.Focus();
                return false;
            }

            // Валидация телефона
            string digitsOnly = new string(StudentData.Phone.Where(char.IsDigit).ToArray());

            if (digitsOnly.StartsWith("7") || digitsOnly.StartsWith("8"))
                digitsOnly = digitsOnly.Substring(1);

            if (digitsOnly.Length != 10 && !string.IsNullOrEmpty(StudentData.Phone))
            {
                MessageBox.Show("Введите корректный номер телефона (10 цифр)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PhoneTextBox.Focus();
                return false;
            }

            // Валидация email
            if (!string.IsNullOrEmpty(StudentData.Email))
            {
                try
                {
                    var addr = new System.Net.Mail.MailAddress(StudentData.Email);
                    if (addr.Address != StudentData.Email)
                        throw new Exception();
                }
                catch
                {
                    MessageBox.Show("Введите корректный email адрес", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    EmailTextBox.Focus();
                    return false;
                }
            }

            // Проверка возраста
            if (StudentData.BirthDate > DateTime.Now.AddYears(-16))
            {
                MessageBox.Show("Учащийся должен быть старше 16 лет", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                BirthDatePicker.Focus();
                return false;
            }

            // Проверка выбора категории
            if (StudentData.VehicleCategoryId == 0)
            {
                MessageBox.Show("Выберите категорию транспортного средства", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoryComboBox.Focus();
                return false;
            }

            // Обновляем значения из текстовых полей
            if (!string.IsNullOrEmpty(TuitionTextBox?.Text))
            {
                if (!decimal.TryParse(TuitionTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal tuition))
                {
                    MessageBox.Show("Некорректный формат суммы обучения", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    TuitionTextBox.Focus();
                    return false;
                }
                StudentData.TuitionAmount = tuition;
            }

            if (!string.IsNullOrEmpty(DiscountTextBox?.Text))
            {
                if (!decimal.TryParse(DiscountTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal discount))
                {
                    MessageBox.Show("Некорректный формат суммы скидки", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    DiscountTextBox.Focus();
                    return false;
                }
                StudentData.DiscountAmount = discount;
            }

            return true;
        }

        private bool SaveStudent()
        {
            try
            {
                int savedId = _dataService.SaveStudent(StudentData);
                StudentData.Id = savedId;

                StudentSaved?.Invoke(this, new StudentSavedEventArgs
                {
                    StudentId = savedId,
                    StudentName = StudentData.FullName
                });

                _isDirty = false; // Сбрасываем флаг после успешного сохранения
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateStudent())
                return;

            // Проверка, что скидка не превышает стоимость
            if (StudentData.DiscountAmount > StudentData.TuitionAmount)
            {
                var result = MessageBox.Show("Скидка превышает стоимость обучения. Продолжить сохранение?",
                    "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    DiscountTextBox.Focus();
                    return;
                }
            }

            if (SaveStudent())
            {
                // Используем существующий метод HasPayments
                bool hasPayments = _dataService.HasPayments(StudentData.Id);

                // Если платежей нет - предлагаем внести первый платеж
                if (!hasPayments)
                {
                    MessageBoxResult result = MessageBox.Show(
                        "Необходимо внести первый платеж. Перейти к оплате?",
                        "Первый платеж",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var paymentDialog = new PaymentEditDialog(_dataService, StudentData.Id, StudentData.FullName);
                        paymentDialog.Owner = this;
                        paymentDialog.ShowDialog();
                    }
                }

                DialogResult = true;
                Close();
            }
        }

        // Обработка нажатия Enter для сохранения
        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SaveButton_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                CancelButton_Click(sender, e);
                e.Handled = true;
            }
        }

        // ==================== PHONE MASK ====================

        private string GetDigits(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            var digits = new string(text.Where(char.IsDigit).ToArray());

            if (digits.Length >= 1 && (digits[0] == '7' || digits[0] == '8'))
                digits = digits.Substring(1);

            return digits;
        }

        private string FormatPhone(string digits)
        {
            if (string.IsNullOrEmpty(digits))
                return "";

            if (digits.Length > 10)
                digits = digits.Substring(0, 10);

            try
            {
                if (digits.Length == 0)
                    return "";

                if (digits.Length <= 3)
                    return $"+7 ({digits}";

                if (digits.Length <= 6)
                    return $"+7 ({digits.Substring(0, 3)}) {digits.Substring(3)}";

                if (digits.Length <= 8)
                    return $"+7 ({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6)}";

                return $"+7 ({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6, 2)}-{digits.Substring(8, 2)}";
            }
            catch
            {
                return "+7 (" + digits;
            }
        }

        private int GetCaretPosition(string text, int digitIndex)
        {
            if (string.IsNullOrEmpty(text) || digitIndex <= 0)
                return 4;

            int digitCount = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsDigit(text[i]))
                {
                    digitCount++;
                    if (digitCount == digitIndex)
                    {
                        if (i + 1 <= text.Length)
                            return i + 1;
                        else
                            return text.Length;
                    }
                }
            }

            return text.Length;
        }

        private void PhoneTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }

            var tb = sender as TextBox;
            if (tb == null) return;

            string currentText = tb.Text ?? "";
            string digits = GetDigits(currentText);
            int caret = tb.CaretIndex;

            if (caret < 0) caret = 0;
            if (caret > currentText.Length) caret = currentText.Length;

            int digitsBefore = currentText.Take(caret).Count(char.IsDigit);

            if (digitsBefore < 0) digitsBefore = 0;
            if (digitsBefore > digits.Length) digitsBefore = digits.Length;

            digits = digits.Insert(digitsBefore, e.Text);

            if (digits.Length > 10)
            {
                e.Handled = true;
                return;
            }

            string formatted = FormatPhone(digits);
            tb.Text = formatted;

            int newCaretPos = GetCaretPosition(formatted, digitsBefore + 1);
            if (newCaretPos >= 0 && newCaretPos <= formatted.Length)
                tb.CaretIndex = newCaretPos;

            StudentData.Phone = formatted;

            e.Handled = true;
        }

        private void PhoneTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            string currentText = tb.Text ?? "";
            string digits = GetDigits(currentText);
            int caret = tb.CaretIndex;

            if (caret < 0) caret = 0;
            if (caret > currentText.Length) caret = currentText.Length;

            int digitsBefore = currentText.Take(caret).Count(char.IsDigit);

            if (digitsBefore < 0) digitsBefore = 0;
            if (digitsBefore > digits.Length) digitsBefore = digits.Length;

            if (e.Key == System.Windows.Input.Key.Back)
            {
                if (digitsBefore > 0 && digits.Length > 0 && digitsBefore - 1 < digits.Length)
                {
                    digits = digits.Remove(digitsBefore - 1, 1);
                    UpdatePhone(tb, digits, digitsBefore - 1);
                }
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Delete)
            {
                if (digitsBefore < digits.Length)
                {
                    digits = digits.Remove(digitsBefore, 1);
                    UpdatePhone(tb, digits, digitsBefore);
                }
                e.Handled = true;
            }
        }

        private void UpdatePhone(TextBox tb, string digits, int digitIndex)
        {
            string formatted = FormatPhone(digits);
            tb.Text = formatted;

            int newCaretPos = GetCaretPosition(formatted, digitIndex);
            if (newCaretPos >= 0 && newCaretPos <= formatted.Length)
                tb.CaretIndex = newCaretPos;

            StudentData.Phone = formatted;
        }

        private void PhoneTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(typeof(string)))
                return;

            var text = (string)e.DataObject.GetData(typeof(string));
            if (string.IsNullOrEmpty(text)) return;

            var digits = new string(text.Where(char.IsDigit).ToArray());

            if (digits.Length >= 1 && (digits[0] == '7' || digits[0] == '8'))
                digits = digits.Substring(1);

            if (digits.Length > 10)
                digits = digits.Substring(0, 10);

            var tb = sender as TextBox;
            if (tb == null) return;

            string formatted = FormatPhone(digits);
            tb.Text = formatted;
            tb.CaretIndex = tb.Text.Length;

            StudentData.Phone = formatted;

            e.CancelCommand();
        }

        private void PhoneTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            // Показываем только цифры при фокусе для удобства редактирования
            string digits = GetDigits(tb.Text);
            if (!string.IsNullOrEmpty(digits))
            {
                tb.Text = digits;
                tb.CaretIndex = tb.Text.Length;
            }
            else
            {
                tb.Text = "";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDirty)
            {
                var result = MessageBox.Show(
                    "Есть несохраненные изменения. Закрыть без сохранения?",
                    "Подтверждение закрытия",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            DialogResult = false;
            Close();
        }
    }

    public class StudentSavedEventArgs : EventArgs
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
    }
}