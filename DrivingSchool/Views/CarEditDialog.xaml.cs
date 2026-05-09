using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class CarEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        public Car CarData { get; private set; }
        private bool _isDirty = false;
        private VehicleCategoryCollection _vehicleCategories;

        public CarEditDialog(SqlDataService dataService, Car carData = null)
        {
            InitializeComponent();
            _dataService = dataService;

            if (carData != null)
            {
                CarData = carData;
                Title = "Редактирование транспортного средства";
            }
            else
            {
                CarData = new Car
                {
                    IsActive = true,
                    Year = DateTime.Now.Year,
                    Category = "B"
                };
                Title = "Добавление транспортного средства";
            }

            DataContext = CarData;
            LoadVehicleCategories();
            LoadInstructors();
            SubscribeToChanges();
        }

        private void LoadVehicleCategories()
        {
            try
            {
                _vehicleCategories = _dataService.LoadVehicleCategories();
                CategoryComboBox.ItemsSource = _vehicleCategories.Categories;
                CategoryComboBox.DisplayMemberPath = "DisplayText";
                CategoryComboBox.SelectedValuePath = "Code";

                if (!string.IsNullOrEmpty(CarData.Category))
                {
                    CategoryComboBox.SelectedValue = CarData.Category;
                }
                else if (_vehicleCategories.Categories.Any())
                {
                    CategoryComboBox.SelectedIndex = 0;
                    CarData.Category = _vehicleCategories.Categories[0].Code;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки категорий: {ex.Message}");
                CategoryComboBox.ItemsSource = new[] { new { Code = "B", DisplayText = "B - Легковые автомобили" } };
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

                if (CarData.InstructorId > 0)
                {
                    InstructorComboBox.SelectedValue = CarData.InstructorId;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки инструкторов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SubscribeToChanges()
        {
            BrandTextBox.TextChanged += (s, e) => { CarData.Brand = BrandTextBox.Text; _isDirty = true; };
            ModelTextBox.TextChanged += (s, e) => { CarData.Model = ModelTextBox.Text; _isDirty = true; };

            LicensePlateTextBox.TextChanged += (s, e) => { _isDirty = true; };

            YearTextBox.TextChanged += (s, e) =>
            {
                if (int.TryParse(YearTextBox.Text, out int year)) CarData.Year = year;
                _isDirty = true;
            };

            ColorTextBox.TextChanged += (s, e) => { CarData.Color = ColorTextBox.Text; _isDirty = true; };
            VINTextBox.TextChanged += (s, e) => { CarData.VIN = VINTextBox.Text.ToUpper(); _isDirty = true; };

            CategoryComboBox.SelectionChanged += (s, e) =>
            {
                if (CategoryComboBox.SelectedItem is VehicleCategory selected)
                {
                    CarData.Category = selected.Code;
                    // Обновляем формат госномера при смене категории
                    string currentClean = new string(LicensePlateTextBox.Text.Where(c => char.IsLetterOrDigit(c)).ToArray());
                    string formatted = FormatLicensePlate(currentClean, CarData.Category);
                    LicensePlateTextBox.Text = formatted;
                    _isDirty = true;
                }
            };

            InstructorComboBox.SelectionChanged += (s, e) =>
            {
                if (InstructorComboBox.SelectedItem is Employee selected)
                {
                    CarData.InstructorId = selected.Id;
                    _isDirty = true;
                }
            };

            IsActiveCheckBox.Checked += (s, e) => { CarData.IsActive = true; _isDirty = true; };
            IsActiveCheckBox.Unchecked += (s, e) => { CarData.IsActive = false; _isDirty = true; };
        }

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
                    if (SaveCar())
                    {
                        DialogResult = true;
                        base.OnClosing(e);
                    }
                    else
                    {
                        e.Cancel = true;
                    }
                }
                else if (result == MessageBoxResult.No)
                {
                    DialogResult = false;
                    base.OnClosing(e);
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else
            {
                base.OnClosing(e);
            }
        }

        // ==================== LICENSE PLATE MASK ====================

        private string FormatLicensePlate(string text, string categoryCode)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            // Убираем все лишние символы
            string clean = new string(text.Where(c => char.IsLetterOrDigit(c)).ToArray()).ToUpper();

            if (clean.Length == 0)
                return "";

            // Формат в зависимости от категории (БЕЗ ПРОБЕЛОВ)
            if (categoryCode == "A" || categoryCode == "A1" || categoryCode == "M")
            {
                // Мотоциклы, мопеды - формат: 1234AB (без пробела)
                if (clean.Length <= 4)
                    return clean;
                else
                    return clean; // просто возвращаем как есть
            }
            else if (categoryCode == "B" || categoryCode == "C" || categoryCode == "D")
            {
                // Легковые, грузовые, автобусы - формат: A123BC77 (без пробелов)
                return clean;
            }
            else if (categoryCode == "E")
            {
                // Прицепы - формат: AB123477 (без пробелов)
                return clean;
            }
            else if (categoryCode == "Tm" || categoryCode == "Tb")
            {
                // Трамваи, троллейбусы - только цифры
                return new string(clean.Where(char.IsDigit).ToArray());
            }
            else
            {
                return clean;
            }
        }

        private void LicensePlateTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null)
            {
                // Показываем только буквы и цифры без форматирования
                string clean = new string(tb.Text.Where(c => char.IsLetterOrDigit(c)).ToArray());
                tb.Text = clean;
                tb.CaretIndex = tb.Text.Length;
            }
        }

        private void LicensePlateTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null && !string.IsNullOrEmpty(tb.Text))
            {
                string clean = new string(tb.Text.Where(c => char.IsLetterOrDigit(c)).ToArray());
                string formatted = FormatLicensePlate(clean, CarData.Category);
                tb.Text = formatted;
                CarData.LicensePlate = formatted;
            }
        }

        private void LicensePlateTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            char c = e.Text[0];

            // Для трамваев и троллейбусов - только цифры
            if (CarData.Category == "Tm" || CarData.Category == "Tb")
            {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    return;
                }
            }
            else
            {
                // Для остальных - буквы и цифры
                if (!char.IsLetterOrDigit(c))
                {
                    e.Handled = true;
                    return;
                }
            }

            var tb = sender as TextBox;
            if (tb == null) return;

            string currentText = tb.Text;
            string clean = new string(currentText.Where(ch => char.IsLetterOrDigit(ch)).ToArray());

            int caretPos = tb.CaretIndex;
            int letterDigitPos = 0;
            for (int i = 0; i < caretPos && i < currentText.Length; i++)
            {
                if (char.IsLetterOrDigit(currentText[i]))
                    letterDigitPos++;
            }

            string newClean = clean.Insert(letterDigitPos, e.Text.ToUpper());

            // Ограничиваем максимальную длину в зависимости от категории
            int maxLength = 9; // по умолчанию для B, C, D
            if (CarData.Category == "A" || CarData.Category == "A1" || CarData.Category == "M")
                maxLength = 6;
            else if (CarData.Category == "E")
                maxLength = 8;
            else if (CarData.Category == "Tm" || CarData.Category == "Tb")
                maxLength = 6;
            else
                maxLength = 9;

            if (newClean.Length > maxLength)
            {
                e.Handled = true;
                return;
            }

            string formatted = FormatLicensePlate(newClean, CarData.Category);
            tb.Text = formatted;

            // Вычисляем новую позицию курсора
            int newCaretPos = caretPos + 1;
            if (newCaretPos > tb.Text.Length) newCaretPos = tb.Text.Length;
            tb.CaretIndex = newCaretPos;

            CarData.LicensePlate = formatted;
            e.Handled = true;
        }

        private void LicensePlateTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            string currentText = tb.Text;
            string clean = new string(currentText.Where(c => char.IsLetterOrDigit(c)).ToArray());
            int caretPos = tb.CaretIndex;

            int letterDigitPos = 0;
            for (int i = 0; i < caretPos && i < currentText.Length; i++)
            {
                if (char.IsLetterOrDigit(currentText[i]))
                    letterDigitPos++;
            }

            if (e.Key == System.Windows.Input.Key.Back)
            {
                if (letterDigitPos > 0 && clean.Length > 0)
                {
                    clean = clean.Remove(letterDigitPos - 1, 1);
                    string formatted = FormatLicensePlate(clean, CarData.Category);
                    tb.Text = formatted;

                    int newCaretPos = caretPos - 1;
                    if (newCaretPos < 0) newCaretPos = 0;
                    if (newCaretPos > tb.Text.Length) newCaretPos = tb.Text.Length;
                    tb.CaretIndex = newCaretPos;

                    CarData.LicensePlate = formatted;
                }
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Delete)
            {
                if (letterDigitPos < clean.Length)
                {
                    clean = clean.Remove(letterDigitPos, 1);
                    string formatted = FormatLicensePlate(clean, CarData.Category);
                    tb.Text = formatted;

                    int newCaretPos = caretPos;
                    if (newCaretPos > tb.Text.Length) newCaretPos = tb.Text.Length;
                    tb.CaretIndex = newCaretPos;

                    CarData.LicensePlate = formatted;
                }
                e.Handled = true;
            }
        }

        private void LicensePlateTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(typeof(string)))
                return;

            var text = (string)e.DataObject.GetData(typeof(string));
            if (string.IsNullOrEmpty(text)) return;

            var clean = new string(text.Where(c => char.IsLetterOrDigit(c)).ToArray()).ToUpper();

            int maxLength = 9; // по умолчанию для B, C, D
            if (CarData.Category == "A" || CarData.Category == "A1" || CarData.Category == "M")
                maxLength = 6;
            else if (CarData.Category == "E")
                maxLength = 8;
            else if (CarData.Category == "Tm" || CarData.Category == "Tb")
                maxLength = 6;
            else
                maxLength = 9;

            if (clean.Length > maxLength)
                clean = clean.Substring(0, maxLength);

            var tb = sender as TextBox;
            if (tb == null) return;

            string formatted = FormatLicensePlate(clean, CarData.Category);
            tb.Text = formatted;
            tb.CaretIndex = tb.Text.Length;
            CarData.LicensePlate = formatted;

            e.CancelCommand();
        }

        private void YearTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
            }
        }

        private void VINTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            string text = e.Text.ToUpper();
            if (!char.IsLetterOrDigit(text, 0) || (char.IsLetter(text, 0) && text[0] > 'Z'))
            {
                e.Handled = true;
            }
        }

        private bool ValidateCar()
        {
            if (string.IsNullOrWhiteSpace(CarData.Brand))
            {
                MessageBox.Show("Введите марку ТС", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                BrandTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(CarData.Model))
            {
                MessageBox.Show("Введите модель ТС", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ModelTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(CarData.LicensePlate))
            {
                MessageBox.Show("Введите государственный номер", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                LicensePlateTextBox.Focus();
                return false;
            }

            if (CarData.Year < 1970 || CarData.Year > DateTime.Now.Year + 1)
            {
                MessageBox.Show($"Год выпуска должен быть от 1970 до {DateTime.Now.Year + 1}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                YearTextBox.Focus();
                return false;
            }

            if (string.IsNullOrEmpty(CarData.Category))
            {
                MessageBox.Show("Выберите категорию ТС", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoryComboBox.Focus();
                return false;
            }

            return true;
        }

        private bool SaveCar()
        {
            try
            {
                _dataService.SaveCar(CarData);
                _isDirty = false;
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
            if (!ValidateCar())
                return;

            if (SaveCar())
            {
                DialogResult = true;
                Close();
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
    }
}