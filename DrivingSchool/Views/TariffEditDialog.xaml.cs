using System;
using System.Linq;
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
        private VehicleCategoryCollection _categories;
        private bool _isDirty = false;

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
            LoadCategories();
            SubscribeToChanges();
        }

        private void LoadCategories()
        {
            try
            {
                _categories = _dataService.LoadVehicleCategories();
                CategoryComboBox.ItemsSource = _categories.Categories;
                CategoryComboBox.DisplayMemberPath = "DisplayText";
                CategoryComboBox.SelectedValuePath = "Code";

                if (!string.IsNullOrEmpty(TariffData.Category))
                {
                    CategoryComboBox.SelectedValue = TariffData.Category;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки категорий: {ex.Message}");
            }
        }

        private void SubscribeToChanges()
        {
            NameTextBox.TextChanged += (s, e) => _isDirty = true;
            CategoryComboBox.SelectionChanged += (s, e) => _isDirty = true;
            DurationTextBox.TextChanged += (s, e) => _isDirty = true;
            PracticeHoursTextBox.TextChanged += (s, e) => _isDirty = true;
            CostTextBox.TextChanged += (s, e) => _isDirty = true;
            DescriptionTextBox.TextChanged += (s, e) => _isDirty = true;
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
                    if (SaveTariff())
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

        private bool ValidateTariff()
        {
            if (string.IsNullOrWhiteSpace(TariffData.Name))
            {
                MessageBox.Show("Введите название тарифа", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TariffData.Category))
            {
                MessageBox.Show("Выберите категорию ТС", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoryComboBox.Focus();
                return false;
            }

            if (TariffData.DurationMonths <= 0)
            {
                MessageBox.Show("Длительность обучения должна быть больше 0", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                DurationTextBox.Focus();
                return false;
            }

            if (TariffData.PracticeHours <= 0)
            {
                MessageBox.Show("Количество часов практики должно быть больше 0", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PracticeHoursTextBox.Focus();
                return false;
            }

            if (TariffData.BaseCost <= 0)
            {
                MessageBox.Show("Стоимость обучения должна быть больше 0", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CostTextBox.Focus();
                return false;
            }

            return true;
        }

        private bool SaveTariff()
        {
            try
            {
                _dataService.SaveTariff(TariffData);
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
            if (!ValidateTariff())
                return;

            if (SaveTariff())
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