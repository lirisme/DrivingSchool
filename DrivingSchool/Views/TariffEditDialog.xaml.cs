using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class TariffEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        public Tariff TariffData { get; private set; }

        public TariffEditDialog(SqlDataService dataService, Tariff tariffData = null)
        {
            InitializeComponent();
            _dataService = dataService;

            if (tariffData != null)
            {
                TariffData = tariffData;
                Title = "Редактирование тарифа";
            }
            else
            {
                TariffData = new Tariff
                {
                    DurationMonths = 3,
                    PracticeHours = 56,
                    BaseCost = 35000
                };
                Title = "Добавление тарифа";
            }

            DataContext = TariffData;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9]+$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void DecimalValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9]*(?:\,[0-9]*)?$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(TariffData.Name))
            {
                MessageBox.Show("Введите название тарифа", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return;
            }

            if (TariffData.DurationMonths <= 0)
            {
                MessageBox.Show("Длительность обучения должна быть больше 0", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DurationTextBox.Focus();
                return;
            }

            if (TariffData.PracticeHours <= 0)
            {
                MessageBox.Show("Количество часов практики должно быть больше 0", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PracticeHoursTextBox.Focus();
                return;
            }

            if (TariffData.BaseCost <= 0)
            {
                MessageBox.Show("Стоимость обучения должна быть больше 0", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CostTextBox.Focus();
                return;
            }

            try
            {
                _dataService.SaveTariff(TariffData);
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