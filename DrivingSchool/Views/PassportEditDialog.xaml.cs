using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        private readonly DateTime _studentBirthDate;
        private readonly int _studentAge;
        private bool _isDirty = false;

        public PassportEditDialog(SqlDataService dataService, int studentId, string studentName,
                          DateTime studentBirthDate, int studentAge, StudentPassportData passportData = null)
        {
            InitializeComponent();
            _dataService = dataService;
            _studentId = studentId;
            _studentName = studentName;
            _studentBirthDate = studentBirthDate;
            _studentAge = studentAge;

            if (passportData != null)
            {
                PassportData = passportData;
                Title = "Редактирование документа";
            }
            else
            {
                // Только для НОВОГО документа предлагаем дату по умолчанию
                DateTime defaultDate = DateTime.Now;

                if (studentAge < 20)
                    defaultDate = studentBirthDate.AddYears(14);
                else if (studentAge < 45)
                    defaultDate = studentBirthDate.AddYears(20);
                else
                    defaultDate = studentBirthDate.AddYears(45);

                PassportData = new StudentPassportData
                {
                    StudentId = studentId,
                    DocumentType = "Паспорт РФ",
                    IssueDate = defaultDate  // Предлагаем, но пользователь может изменить
                };
                Title = "Добавление документа";
            }

            DataContext = PassportData;
            StudentNameTextBox.Text = studentName;

            // Временно отключаем обработчик
            DocumentTypeComboBox.SelectionChanged -= DocumentType_SelectionChanged;

            // Устанавливаем выбранное значение в ComboBox
            foreach (ComboBoxItem item in DocumentTypeComboBox.Items)
            {
                if (item.Content.ToString() == PassportData.DocumentType)
                {
                    DocumentTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            // Включаем обработчик обратно
            DocumentTypeComboBox.SelectionChanged += DocumentType_SelectionChanged;

            SubscribeToChanges();
            UpdateFieldsVisibility();
            UpdateFormatHints();
        }

        private void SubscribeToChanges()
        {
            DocumentTypeComboBox.SelectionChanged += (s, e) => { _isDirty = true; UpdateFieldsVisibility(); UpdateFormatHints(); };
            SeriesTextBox.TextChanged += (s, e) => _isDirty = true;
            NumberTextBox.TextChanged += (s, e) => _isDirty = true;
            IssuedByTextBox.TextChanged += (s, e) => _isDirty = true;
            IssuedByTextBox.TextChanged += IssuedByTextBox_TextChanged; // Добавить эту строку
            DivisionCodeTextBox.TextChanged += (s, e) => _isDirty = true;
            IssueDatePicker.SelectedDateChanged += (s, e) => _isDirty = true;
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
                    if (SaveDocument())
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

        private DateTime GetDefaultPassportIssueDate()
        {
            // Проверка на случай вызова до инициализации
            if (PassportData == null)
            {
                // Возвращаем дату по умолчанию (14 лет от даты рождения студента)
                if (_studentAge < 20)
                    return _studentBirthDate.AddYears(14);
                else if (_studentAge < 45)
                    return _studentBirthDate.AddYears(20);
                else
                    return _studentBirthDate.AddYears(45);
            }

            if (PassportData.DocumentType != "Паспорт РФ")
                return DateTime.Now;

            DateTime issueDate;

            if (_studentAge < 20)
            {
                issueDate = _studentBirthDate.AddYears(14);
            }
            else if (_studentAge < 45)
            {
                issueDate = _studentBirthDate.AddYears(20);
            }
            else
            {
                issueDate = _studentBirthDate.AddYears(45);
            }

            return issueDate;
        }

        private void UpdateFieldsVisibility()
        {
            var docType = PassportData.DocumentType;

            // Серия (видна не для всех)
            if (docType == "Паспорт РФ" || docType == "Загранпаспорт" || docType == "Временное удостоверение" || docType == "Военный билет")
            {
                SeriesPanel.Visibility = Visibility.Visible;
            }
            else
            {
                SeriesPanel.Visibility = Visibility.Collapsed;
                PassportData.Series = "";
            }

            // Код подразделения (только для паспорта РФ)
            if (docType == "Паспорт РФ")
            {
                DivisionCodePanel.Visibility = Visibility.Visible;
            }
            else
            {
                DivisionCodePanel.Visibility = Visibility.Collapsed;
                PassportData.DivisionCode = "";
            }

            // Информационное сообщение (без принудительной установки даты)
            if (docType == "Паспорт РФ")
            {
                InfoTextBlock.Text = "Для паспорта РФ: серия 4 цифры, номер 6 цифр";
            }
            else if (docType == "Свидетельство о рождении")
            {
                InfoTextBlock.Text = "Для свидетельства о рождении: номер до 8 цифр";
                // НЕ устанавливаем дату принудительно!
            }
            else
            {
                InfoTextBlock.Text = "Укажите данные документа";
            }
        }

        private void UpdateFormatHints()
        {
            var docType = PassportData.DocumentType;

            if (docType == "Паспорт РФ")
            {
                SeriesFormatHint.Visibility = Visibility.Visible;
                SeriesFormatHint.Text = "(4 цифры)";
                // Подсказка для номера
                InfoTextBlock.Text = "Для паспорта РФ: серия 4 цифры, номер 6 цифр";
            }
            else if (docType == "Загранпаспорт")
            {
                SeriesFormatHint.Visibility = Visibility.Visible;
                SeriesFormatHint.Text = "(2 цифры + 2 буквы)";
                InfoTextBlock.Text = "Для загранпаспорта: номер 7 цифр";
            }
            else if (docType == "Свидетельство о рождении")
            {
                SeriesFormatHint.Visibility = Visibility.Collapsed;
                InfoTextBlock.Text = "Для свидетельства о рождении: номер до 8 цифр";
            }
            else
            {
                SeriesFormatHint.Visibility = Visibility.Collapsed;
                InfoTextBlock.Text = "Укажите данные документа";
            }
        }

        private void DocumentType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (PassportData == null) return;

                if (DocumentTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    string docType = selectedItem.Content.ToString();
                    PassportData.DocumentType = docType;

                    UpdateFieldsVisibility();
                    UpdateFormatHints();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в DocumentType_SelectionChanged: {ex.Message}");
            }
        }

        // ==================== МАСКА ДЛЯ СЕРИИ ПАСПОРТА ====================

        private void SeriesTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null && PassportData.DocumentType == "Паспорт РФ")
            {
                string digits = new string(tb.Text.Where(char.IsDigit).ToArray());
                tb.Text = digits;
                tb.CaretIndex = tb.Text.Length;
            }
        }

        private void SeriesTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null && PassportData.DocumentType == "Паспорт РФ")
            {
                string digits = new string(tb.Text.Where(char.IsDigit).ToArray());
                if (digits.Length == 4)
                {
                    tb.Text = $"{digits.Substring(0, 2)} {digits.Substring(2, 2)}";
                    PassportData.Series = digits;
                }
                else
                {
                    PassportData.Series = digits;
                }
            }
        }

        private void SeriesTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (PassportData.DocumentType == "Паспорт РФ" && !char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }

            var tb = sender as TextBox;
            if (tb == null) return;

            string currentText = tb.Text;
            string clean = new string(currentText.Where(char.IsDigit).ToArray());

            if (clean.Length >= 4 && PassportData.DocumentType == "Паспорт РФ")
            {
                e.Handled = true;
                return;
            }
        }

        private void SeriesTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                return;
            }
        }

        private void SeriesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null && PassportData.DocumentType == "Паспорт РФ")
            {
                string clean = new string(tb.Text.Where(char.IsDigit).ToArray());
                if (clean.Length == 4 && tb.Text.Length < 5)
                {
                    tb.TextChanged -= SeriesTextBox_TextChanged;
                    tb.Text = $"{clean.Substring(0, 2)} {clean.Substring(2, 2)}";
                    tb.CaretIndex = tb.Text.Length;
                    tb.TextChanged += SeriesTextBox_TextChanged;
                }
            }
        }

        // ==================== МАСКА ДЛЯ НОМЕРА ====================

        private void NumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }

            var tb = sender as TextBox;
            if (tb == null) return;

            // Получаем текущие цифры
            string digits = new string(tb.Text.Where(char.IsDigit).ToArray());

            // Для паспорта РФ ограничиваем 6 цифрами
            if (PassportData.DocumentType == "Паспорт РФ" && digits.Length >= 6)
            {
                e.Handled = true;
                return;
            }

            // Для загранпаспорта - 7 цифр
            if (PassportData.DocumentType == "Загранпаспорт" && digits.Length >= 7)
            {
                e.Handled = true;
                return;
            }

            // Для свидетельства о рождении - 8 цифр
            if (PassportData.DocumentType == "Свидетельство о рождении" && digits.Length >= 8)
            {
                e.Handled = true;
                return;
            }
        }

        private void NumberTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null)
            {
                string digits = new string(tb.Text.Where(char.IsDigit).ToArray());
                PassportData.Number = digits;
                tb.Text = digits;

                // Проверка количества цифр для паспорта РФ
                if (PassportData.DocumentType == "Паспорт РФ" && digits.Length != 6)
                {
                    WarningBorder.Visibility = Visibility.Visible;
                    WarningTextBlock.Text = "Номер паспорта должен содержать 6 цифр!";
                }
                else
                {
                    WarningBorder.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void IssuedByTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null && tb.Text != tb.Text.ToUpper())
            {
                int caret = tb.CaretIndex;
                tb.Text = tb.Text.ToUpper();
                tb.CaretIndex = Math.Min(caret, tb.Text.Length);
                PassportData.IssuedBy = tb.Text;
            }
        }

        // ==================== МАСКА ДЛЯ КОДА ПОДРАЗДЕЛЕНИЯ ====================

        private void DivisionCodeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }

            var tb = sender as TextBox;
            if (tb == null) return;

            string clean = new string(tb.Text.Where(char.IsDigit).ToArray());

            if (clean.Length >= 6)
            {
                e.Handled = true;
                return;
            }
        }

        private void DivisionCodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null)
            {
                string clean = new string(tb.Text.Where(char.IsDigit).ToArray());
                if (clean.Length == 6 && tb.Text.Length < 7)
                {
                    tb.TextChanged -= DivisionCodeTextBox_TextChanged;
                    tb.Text = $"{clean.Substring(0, 3)}-{clean.Substring(3, 3)}";
                    tb.CaretIndex = tb.Text.Length;
                    tb.TextChanged += DivisionCodeTextBox_TextChanged;
                }
                PassportData.DivisionCode = tb.Text;
            }
        }

        private void DivisionCodeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null)
            {
                string digits = new string(tb.Text.Where(char.IsDigit).ToArray());
                if (digits.Length == 6)
                {
                    tb.Text = $"{digits.Substring(0, 3)}-{digits.Substring(3, 3)}";
                    PassportData.DivisionCode = tb.Text;
                }
            }
        }

        // ==================== ВАЛИДАЦИЯ ====================

        private bool ValidateDocument()
        {
            var docType = PassportData.DocumentType;

            // Проверка номера
            if (string.IsNullOrWhiteSpace(PassportData.Number))
            {
                MessageBox.Show("Введите номер документа", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NumberTextBox.Focus();
                return false;
            }

            string numberDigits = new string(PassportData.Number.Where(char.IsDigit).ToArray());
            if (docType == "Паспорт РФ" && numberDigits.Length != 6)
            {
                MessageBox.Show("Номер паспорта должен содержать 6 цифр", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NumberTextBox.Focus();
                return false;
            }

            // Проверка кем выдан
            if (string.IsNullOrWhiteSpace(PassportData.IssuedBy))
            {
                MessageBox.Show("Введите кем выдан документ", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                IssuedByTextBox.Focus();
                return false;
            }

            // Проверка серии для паспорта РФ
            if (docType == "Паспорт РФ")
            {
                string seriesDigits = new string(PassportData.Series.Where(char.IsDigit).ToArray());
                if (seriesDigits.Length != 4)
                {
                    MessageBox.Show("Серия паспорта должна содержать 4 цифры", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    SeriesTextBox.Focus();
                    return false;
                }

                // Проверка кода подразделения
                string codeDigits = new string(PassportData.DivisionCode.Where(char.IsDigit).ToArray());
                if (codeDigits.Length != 6)
                {
                    MessageBox.Show("Код подразделения должен быть в формате 123-456 (6 цифр)", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    DivisionCodeTextBox.Focus();
                    return false;
                }
            }

            // Проверка даты
            if (PassportData.IssueDate > DateTime.Now)
            {
                MessageBox.Show("Дата выдачи не может быть в будущем", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                IssueDatePicker.Focus();
                return false;
            }

            return true;
        }

        private bool CheckDuplicatePassport()
        {
            var existingPassports = _dataService.LoadPassportData();
            var duplicate = existingPassports.Passports.Any(p =>
                p.StudentId == _studentId &&
                p.Id != PassportData.Id &&
                p.DocumentType == PassportData.DocumentType);

            if (duplicate)
            {
                MessageBox.Show($"У студента уже есть документ типа '{PassportData.DocumentType}'.\n\n" +
                               "У одного студента не может быть двух одинаковых типов документов.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private bool SaveDocument()
        {
            try
            {
                if (!CheckDuplicatePassport())
                    return false;

                _dataService.SavePassportData(PassportData);
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
            if (!ValidateDocument())
                return;

            if (SaveDocument())
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

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SaveButton_Click(sender, e);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                CancelButton_Click(sender, e);
                e.Handled = true;
            }
        }


    }
}