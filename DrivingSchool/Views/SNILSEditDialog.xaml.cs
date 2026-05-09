using DrivingSchool.Models;
using DrivingSchool.Services;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DrivingSchool.Views
{
    public partial class SNILSEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        public StudentSNILS SNILSData { get; private set; }
        private readonly int _studentId;
        private readonly string _studentName;
        private bool _isDirty = false;

        public SNILSEditDialog(SqlDataService dataService, int studentId, string studentName, StudentSNILS snilsData = null)
        {
            InitializeComponent();
            _dataService = dataService;
            _studentId = studentId;
            _studentName = studentName;

            if (snilsData != null)
            {
                SNILSData = snilsData;
                Title = "Редактирование данных СНИЛС";
            }
            else
            {
                SNILSData = new StudentSNILS
                {
                    StudentId = studentId,
                    IssueDate = DateTime.Now
                };
                Title = "Добавление данных СНИЛС";
            }

            DataContext = SNILSData;
            StudentNameTextBox.Text = studentName;
            SubscribeToChanges();

            // Форматируем номер при загрузке
            if (!string.IsNullOrEmpty(SNILSData.Number))
            {
                NumberTextBox.Text = FormatSNILSNumber(SNILSData.Number);
            }
        }

        private void SubscribeToChanges()
        {
            NumberTextBox.TextChanged += (s, e) => _isDirty = true;
            IssueDatePicker.SelectedDateChanged += (s, e) => _isDirty = true;
            IssuedByTextBox.TextChanged += (s, e) => _isDirty = true;
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
                    if (SaveSNILS())
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

        // ==================== МАСКА ДЛЯ СНИЛС ====================

        private string FormatSNILSNumber(string number)
        {
            if (string.IsNullOrEmpty(number))
                return "";

            var digits = new string(number.Where(char.IsDigit).ToArray());

            if (digits.Length == 0)
                return "";

            if (digits.Length <= 3)
                return digits;

            if (digits.Length <= 6)
                return $"{digits.Substring(0, 3)}-{digits.Substring(3)}";

            if (digits.Length <= 9)
                return $"{digits.Substring(0, 3)}-{digits.Substring(3, 3)}-{digits.Substring(6)}";

            return $"{digits.Substring(0, 3)}-{digits.Substring(3, 3)}-{digits.Substring(6, 3)} {digits.Substring(9)}";
        }

        private string GetDigitsFromSNILS(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            return new string(text.Where(char.IsDigit).ToArray());
        }

        private void NumberTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null)
            {
                string digits = GetDigitsFromSNILS(tb.Text);
                tb.Text = digits;
                tb.CaretIndex = tb.Text.Length;
            }
        }

        private void NumberTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null)
            {
                string digits = GetDigitsFromSNILS(tb.Text);
                if (digits.Length == 11)
                {
                    tb.Text = FormatSNILSNumber(digits);
                }
                SNILSData.Number = digits;
            }
        }

        private void NumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }

            var tb = sender as TextBox;
            if (tb == null) return;

            string currentText = tb.Text;
            string digits = GetDigitsFromSNILS(currentText);
            int caretPos = tb.CaretIndex;

            int digitsBeforeCaret = 0;
            for (int i = 0; i < caretPos && i < currentText.Length; i++)
            {
                if (char.IsDigit(currentText[i]))
                    digitsBeforeCaret++;
            }

            string newDigits = digits.Insert(digitsBeforeCaret, e.Text);

            if (newDigits.Length > 11)
            {
                e.Handled = true;
                return;
            }

            string formatted = FormatSNILSNumber(newDigits);
            tb.Text = formatted;

            int newCaretPos = GetCaretPosition(formatted, digitsBeforeCaret + 1);
            tb.CaretIndex = newCaretPos;

            SNILSData.Number = newDigits;
            e.Handled = true;
        }

        private void NumberTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            string currentText = tb.Text;
            string digits = GetDigitsFromSNILS(currentText);
            int caretPos = tb.CaretIndex;

            int digitsBeforeCaret = 0;
            for (int i = 0; i < caretPos && i < currentText.Length; i++)
            {
                if (char.IsDigit(currentText[i]))
                    digitsBeforeCaret++;
            }

            if (e.Key == Key.Back && digitsBeforeCaret > 0 && digits.Length > 0)
            {
                string newDigits = digits.Remove(digitsBeforeCaret - 1, 1);
                string formatted = FormatSNILSNumber(newDigits);
                tb.Text = formatted;
                int newCaretPos = GetCaretPosition(formatted, digitsBeforeCaret - 1);
                tb.CaretIndex = newCaretPos;
                SNILSData.Number = newDigits;
                e.Handled = true;
            }
            else if (e.Key == Key.Delete && digitsBeforeCaret < digits.Length)
            {
                string newDigits = digits.Remove(digitsBeforeCaret, 1);
                string formatted = FormatSNILSNumber(newDigits);
                tb.Text = formatted;
                int newCaretPos = GetCaretPosition(formatted, digitsBeforeCaret);
                tb.CaretIndex = newCaretPos;
                SNILSData.Number = newDigits;
                e.Handled = true;
            }
        }

        private int GetCaretPosition(string text, int digitIndex)
        {
            if (string.IsNullOrEmpty(text) || digitIndex <= 0)
                return 0;

            int digitCount = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsDigit(text[i]))
                {
                    digitCount++;
                    if (digitCount == digitIndex)
                        return i + 1;
                }
            }
            return text.Length;
        }

        // ==================== ПРОВЕРКА КОНТРОЛЬНОЙ СУММЫ СНИЛС ====================

        private bool IsValidSNILS(string number)
        {
            if (string.IsNullOrEmpty(number)) return false;

            var digits = new string(number.Where(char.IsDigit).ToArray());

            if (digits.Length != 11) return false;

            // Проверка, что не все цифры одинаковые
            if (digits.All(c => c == digits[0])) return false;

            int sum = 0;
            for (int i = 0; i < 9; i++)
            {
                sum += (digits[i] - '0') * (9 - i);
            }

            int checkDigit;
            if (sum < 100)
            {
                checkDigit = sum;
            }
            else if (sum == 100 || sum == 101)
            {
                checkDigit = 0;
            }
            else
            {
                checkDigit = sum % 101;
                if (checkDigit == 100 || checkDigit == 101)
                    checkDigit = 0;
            }

            int actualCheckDigit = int.Parse(digits.Substring(9, 2));

            return checkDigit == actualCheckDigit;
        }

        // ==================== АВТОМАТИЧЕСКИЙ ВЕРХНИЙ РЕГИСТР ====================

        private void IssuedByTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null && tb.Text != tb.Text.ToUpper())
            {
                int caret = tb.CaretIndex;
                tb.Text = tb.Text.ToUpper();
                tb.CaretIndex = Math.Min(caret, tb.Text.Length);
                SNILSData.IssuedBy = tb.Text;
            }
        }

        // ==================== ВАЛИДАЦИЯ ====================

        private bool CheckDuplicateSNILS()
        {
            var existingSNILS = _dataService.LoadSNILSData();
            var duplicate = existingSNILS.SNILSList.Any(s =>
                s.Number == SNILSData.Number &&
                s.Id != SNILSData.Id);

            if (duplicate)
            {
                MessageBox.Show("СНИЛС с таким номером уже существует в базе данных!\n\n" +
                               "Каждый СНИЛС уникален и не может повторяться.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private bool ValidateAndSaveNumber()
        {
            string digits = GetDigitsFromSNILS(NumberTextBox.Text);

            if (digits.Length != 11)
            {
                MessageBox.Show("Номер СНИЛС должен содержать 11 цифр!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NumberTextBox.Focus();
                return false;
            }

            if (!IsValidSNILS(digits))
            {
                MessageBox.Show("Неверная контрольная сумма СНИЛС! Проверьте правильность ввода.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NumberTextBox.Focus();
                return false;
            }

            SNILSData.Number = digits;
            return true;
        }

        private bool ValidateDates()
        {
            if (SNILSData.IssueDate.HasValue && SNILSData.IssueDate.Value > DateTime.Now)
            {
                MessageBox.Show("Дата выдачи не может быть в будущем", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                IssueDatePicker.Focus();
                return false;
            }
            return true;
        }

        private bool SaveSNILS()
        {
            try
            {
                _dataService.SaveSNILSData(SNILSData);
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
            if (!ValidateAndSaveNumber()) return;
            if (!ValidateDates()) return;
            if (!CheckDuplicateSNILS()) return;

            if (SaveSNILS())
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