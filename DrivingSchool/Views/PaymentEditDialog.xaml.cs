using DrivingSchool.Models;
using DrivingSchool.Services;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DrivingSchool.Views
{
    public partial class PaymentEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        public Payment PaymentData { get; private set; }
        private readonly int _studentId;
        private readonly string _studentName;
        private bool _isDirty = false;

        public PaymentEditDialog(SqlDataService dataService, int studentId, string studentName, Payment paymentData = null)
        {
            InitializeComponent();
            _dataService = dataService;
            _studentId = studentId;
            _studentName = studentName;

            if (paymentData != null)
            {
                PaymentData = paymentData;
                Title = "Редактирование платежа";
            }
            else
            {
                PaymentData = new Payment
                {
                    StudentId = studentId,
                    PaymentDate = DateTime.Now,
                    PaymentType = "Наличные"
                };
                Title = "Добавление платежа";
            }

            DataContext = PaymentData;
            StudentNameTextBox.Text = studentName;
            SubscribeToChanges();
        }

        private void SubscribeToChanges()
        {
            PaymentDatePicker.SelectedDateChanged += (s, e) => _isDirty = true;
            AmountTextBox.TextChanged += (s, e) => _isDirty = true;
            PaymentTypeComboBox.SelectionChanged += (s, e) => _isDirty = true;
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
                    if (SavePayment())
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

        // ==================== МАСКА ДЛЯ СУММЫ ====================

        private void AmountTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0) && e.Text != "," && e.Text != "." && e.Text != "-")
            {
                e.Handled = true;
                return;
            }

            var tb = sender as TextBox;
            if (tb == null) return;

            // Минус только в начале
            if (e.Text == "-" && tb.Text.Length > 0)
            {
                e.Handled = true;
                return;
            }

            // Запрещаем больше одной запятой/точки
            if ((e.Text == "," || e.Text == ".") && (tb.Text.Contains(",") || tb.Text.Contains(".")))
            {
                e.Handled = true;
                return;
            }
        }

        private void AmountTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            string text = tb.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            // Заменяем точку на запятую
            text = text.Replace(".", ",");

            // Проверяем на отрицательное число
            bool isNegative = text.StartsWith("-");
            if (isNegative)
            {
                text = text.Substring(1);
            }

            // Добавляем ,00 если нет копеек
            if (!text.Contains(","))
            {
                text += ",00";
            }

            // Ограничиваем 2 знака после запятой
            if (text.Contains(","))
            {
                var parts = text.Split(',');
                if (parts[1].Length > 2)
                {
                    parts[1] = parts[1].Substring(0, 2);
                    text = string.Join(",", parts);
                }
            }

            // Добавляем минус обратно
            if (isNegative)
            {
                text = "-" + text;
            }

            if (decimal.TryParse(text, out decimal amount))
            {
                PaymentData.Amount = amount;
                tb.Text = text;
            }
        }

        // ==================== ВАЛИДАЦИЯ ====================

        private bool ValidatePayment()
        {
            if (PaymentData.Amount == 0)
            {
                MessageBox.Show("Введите сумму платежа (не 0)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                AmountTextBox.Focus();
                return false;
            }

            if (Math.Abs(PaymentData.Amount) > 1000000)
            {
                MessageBox.Show("Сумма платежа не может превышать 1 000 000 руб.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                AmountTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(PaymentData.PaymentType))
            {
                MessageBox.Show("Выберите тип платежа", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PaymentTypeComboBox.Focus();
                return false;
            }

            return true;
        }

        private bool SavePayment()
        {
            try
            {
                if (PaymentData.Id == 0)
                {
                    _dataService.AddPayment(PaymentData);
                }
                else
                {
                    _dataService.UpdatePayment(PaymentData);
                }

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
            if (!ValidatePayment()) return;

            // Предупреждение при отрицательной сумме (возврат)
            if (PaymentData.Amount < 0)
            {
                var result = MessageBox.Show(
                    "Вы вводите ОТРИЦАТЕЛЬНУЮ сумму (возврат).\n\n" +
                    "Убедитесь, что это корректная операция.\n\n" +
                    "Продолжить?",
                    "Подтверждение возврата",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            if (SavePayment())
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

        public void SetPaymentData(decimal amount, string paymentType)
        {
            if (amount > 0)
            {
                AmountTextBox.Text = amount.ToString("F2");
            }
            PaymentTypeComboBox.Text = paymentType;
        }
    }
}