using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class PaymentEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        public Payment PaymentData { get; private set; }
        private readonly int _studentId;
        private readonly string _studentName;

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
        }

        private void DecimalValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9]*(?:\,[0-9]*)?$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (PaymentData.Amount <= 0)
            {
                MessageBox.Show("Введите сумму платежа (больше 0)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                AmountTextBox.Focus();
                return;
            }

            if (PaymentData.Amount > 1000000)
            {
                MessageBox.Show("Сумма платежа не может превышать 1 000 000 руб.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                AmountTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(PaymentData.PaymentType))
            {
                MessageBox.Show("Выберите тип платежа", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PaymentTypeComboBox.Focus();
                return;
            }

            try
            {
                if (PaymentData.Id == 0)
                {
                    // Новый платеж
                    _dataService.AddPayment(PaymentData);
                }
                else
                {
                    // Обновление существующего платежа
                    _dataService.UpdatePayment(PaymentData);
                }

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