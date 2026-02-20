using System;
using System.Windows;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class CategoryEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        public VehicleCategory CategoryData { get; private set; }

        public CategoryEditDialog(SqlDataService dataService, VehicleCategory categoryData = null)
        {
            InitializeComponent();
            _dataService = dataService;

            if (categoryData != null)
            {
                CategoryData = categoryData;
                Title = "Редактирование категории";
            }
            else
            {
                CategoryData = new VehicleCategory();
                Title = "Добавление категории";
            }

            DataContext = CategoryData;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CategoryData.Code))
            {
                MessageBox.Show("Введите код категории", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CodeTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(CategoryData.FullName))
            {
                MessageBox.Show("Введите название категории", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                FullNameTextBox.Focus();
                return;
            }

            try
            {
                _dataService.SaveVehicleCategory(CategoryData);
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