using System;
using System.Windows;
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
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(AddressData.Region))
            {
                MessageBox.Show("Введите регион", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                RegionTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(AddressData.City))
            {
                MessageBox.Show("Введите город или населенный пункт", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CityTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(AddressData.Street))
            {
                MessageBox.Show("Введите улицу", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                StreetTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(AddressData.House))
            {
                MessageBox.Show("Введите номер дома", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                HouseTextBox.Focus();
                return;
            }

            try
            {
                // TODO: Сохранить через сервис
                // _dataService.SaveAddressData(AddressData);

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