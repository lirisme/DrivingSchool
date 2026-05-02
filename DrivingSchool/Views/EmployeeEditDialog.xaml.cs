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
        private bool _isDirty = false;

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
            SubscribeToChanges();
        }

        private void SubscribeToChanges()
        {
            LastNameTextBox.TextChanged += (s, e) => _isDirty = true;
            FirstNameTextBox.TextChanged += (s, e) => _isDirty = true;
            MiddleNameTextBox.TextChanged += (s, e) => _isDirty = true;
            PositionComboBox.SelectionChanged += (s, e) => _isDirty = true;
            StatusComboBox.SelectionChanged += (s, e) => _isDirty = true;
            PhoneTextBox.TextChanged += (s, e) => _isDirty = true;
            EmailTextBox.TextChanged += (s, e) => _isDirty = true;
            HireDatePicker.SelectedDateChanged += (s, e) => _isDirty = true;
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
                    if (SaveEmployee())
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

        private bool ValidateEmployee()
        {
            if (string.IsNullOrWhiteSpace(EmployeeData.LastName))
            {
                MessageBox.Show("Введите фамилию сотрудника", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                LastNameTextBox.Focus();
                return false;
            }

            // Берем значение из ComboBox
            if (PositionComboBox.SelectedItem is ComboBoxItem selectedPosition)
                EmployeeData.Position = selectedPosition.Content.ToString();

            if (StatusComboBox.SelectedItem is ComboBoxItem selectedStatus)
                EmployeeData.Status = selectedStatus.Content.ToString();

            if (string.IsNullOrWhiteSpace(EmployeeData.FirstName))
            {
                MessageBox.Show("Введите имя сотрудника", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                FirstNameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(EmployeeData.Position))
            {
                MessageBox.Show("Введите должность сотрудника", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PositionComboBox.Focus();
                return false;
            }

            // Валидация телефона
            string digitsOnly = new string(EmployeeData.Phone.Where(char.IsDigit).ToArray());

            if (digitsOnly.StartsWith("7") || digitsOnly.StartsWith("8"))
                digitsOnly = digitsOnly.Substring(1);

            if (digitsOnly.Length != 10 && !string.IsNullOrEmpty(EmployeeData.Phone))
            {
                MessageBox.Show("Введите корректный номер телефона (10 цифр)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PhoneTextBox.Focus();
                return false;
            }

            // Валидация email
            if (!string.IsNullOrEmpty(EmployeeData.Email))
            {
                try
                {
                    var addr = new System.Net.Mail.MailAddress(EmployeeData.Email);
                    if (addr.Address != EmployeeData.Email)
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

            if (EmployeeData.HireDate > DateTime.Now)
            {
                MessageBox.Show("Дата приема не может быть в будущем", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                HireDatePicker.Focus();
                return false;
            }

            return true;
        }

        private bool SaveEmployee()
        {
            try
            {
                _dataService.SaveEmployee(EmployeeData);
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
            if (!ValidateEmployee())
                return;

            if (SaveEmployee())
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

        // ==================== PHONE MASK (как у студентов) ====================

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

            EmployeeData.Phone = formatted;

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

            EmployeeData.Phone = formatted;
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

            EmployeeData.Phone = formatted;

            e.CancelCommand();
        }

        private void PhoneTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

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
    }
}