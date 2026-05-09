using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class AddressEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        public StudentRegistrationAddress AddressData { get; private set; }
        private readonly int _studentId;
        private readonly string _studentName;
        private bool _isDirty = false;

        public AddressEditDialog(SqlDataService dataService, int studentId, string studentName, StudentRegistrationAddress addressData = null)
        {
            InitializeComponent();
            _dataService = dataService;
            _studentId = studentId;
            _studentName = studentName;

            if (addressData != null)
            {
                AddressData = addressData;
                Title = "Редактирование адреса регистрации";
            }
            else
            {
                AddressData = new StudentRegistrationAddress
                {
                    StudentId = studentId,
                    Region = "Оренбургская область"
                };
                Title = "Добавление адреса регистрации";
            }

            DataContext = AddressData;
            StudentNameTextBox.Text = studentName;
            SubscribeToChanges();
            UpdatePreview();
        }

        private void SubscribeToChanges()
        {
            RegionTextBox.TextChanged += (s, e) => { _isDirty = true; UpdatePreview(); };
            CityTextBox.TextChanged += (s, e) => { _isDirty = true; UpdatePreview(); };
            StreetTextBox.TextChanged += (s, e) => { _isDirty = true; UpdatePreview(); };
            HouseTextBox.TextChanged += (s, e) => { _isDirty = true; UpdatePreview(); };
            BuildingTextBox.TextChanged += (s, e) => { _isDirty = true; UpdatePreview(); };
            ApartmentTextBox.TextChanged += (s, e) => { _isDirty = true; UpdatePreview(); };
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
                    if (SaveAddress())
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

        private void UpdatePreview()
        {
            var address = $"{AddressData.City}, {AddressData.Street} {AddressData.House}";
            if (!string.IsNullOrWhiteSpace(AddressData.Building))
                address += $", корп.{AddressData.Building}";
            if (!string.IsNullOrWhiteSpace(AddressData.Apartment))
                address += $", кв.{AddressData.Apartment}";

            PreviewTextBlock.Text = address;
        }

        // ==================== ВАЛИДАЦИЯ ====================

        private bool ValidateAddress()
        {
            if (string.IsNullOrWhiteSpace(AddressData.Region))
            {
                MessageBox.Show("Введите регион", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                RegionTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(AddressData.City))
            {
                MessageBox.Show("Введите город", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CityTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(AddressData.Street))
            {
                MessageBox.Show("Введите улицу", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                StreetTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(AddressData.House))
            {
                MessageBox.Show("Введите номер дома", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                HouseTextBox.Focus();
                return false;
            }

            return true;
        }

        private bool SaveAddress()
        {
            try
            {
                _dataService.SaveAddressData(AddressData);
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
            if (!ValidateAddress())
                return;

            if (SaveAddress())
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