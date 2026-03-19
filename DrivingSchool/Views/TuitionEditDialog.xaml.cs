using System;
using System.Windows;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class TuitionEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        private readonly int _studentId;

        public decimal TuitionAmount { get; private set; }
        public decimal DiscountAmount { get; private set; }

        public TuitionEditDialog(SqlDataService dataService, int studentId, string studentName,
                                decimal currentTuition, decimal currentDiscount)
        {
            InitializeComponent();

            _dataService = dataService;
            _studentId = studentId;

            StudentNameText.Text = studentName;
            TuitionTextBox.Text = currentTuition.ToString("F2");
            DiscountTextBox.Text = currentDiscount.ToString("F2");
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!decimal.TryParse(TuitionTextBox.Text, out decimal tuition) || tuition < 0)
                {
                    MessageBox.Show("Введите корректную стоимость обучения (положительное число)",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TuitionTextBox.Focus();
                    return;
                }

                if (!decimal.TryParse(DiscountTextBox.Text, out decimal discount) || discount < 0)
                {
                    MessageBox.Show("Введите корректную сумму скидки (положительное число)",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    DiscountTextBox.Focus();
                    return;
                }

                if (discount > tuition)
                {
                    MessageBox.Show("Скидка не может быть больше стоимости обучения",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    DiscountTextBox.Focus();
                    return;
                }

                TuitionAmount = tuition;
                DiscountAmount = discount;

                // Сохраняем в базу данных
                _dataService.UpdateStudentTuition(_studentId, tuition, discount);

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