using System;
using System.Linq;
using System.Windows;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class CertificateEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        public StudentCertificate CertificateData { get; private set; }
        private readonly int _studentId;
        private readonly string _studentName;
        private VehicleCategoryCollection _categories;

        public CertificateEditDialog(SqlDataService dataService, int studentId, string studentName, StudentCertificate certificateData = null)
        {
            InitializeComponent();
            _dataService = dataService;
            _studentId = studentId;
            _studentName = studentName;
            _categories = _dataService.LoadVehicleCategories();

            if (certificateData != null)
            {
                CertificateData = certificateData;
                Title = "Редактирование свидетельства";
            }
            else
            {
                CertificateData = new StudentCertificate
                {
                    StudentId = studentId,
                    IssueDate = DateTime.Now
                };
                Title = "Добавление свидетельства";
            }

            DataContext = CertificateData;
            StudentNameTextBox.Text = studentName;
            LoadCategories();
        }

        private void LoadCategories()
        {
            CategoryComboBox.ItemsSource = _categories.Categories;

            // Если категория не выбрана, выбираем первую
            if (CertificateData.VehicleCategoryId == 0 && _categories.Categories.Any())
            {
                CertificateData.VehicleCategoryId = _categories.Categories.First().Id;
                CertificateData.CategoryCode = _categories.Categories.First().Code;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(CertificateData.CertificateSeries))
            {
                MessageBox.Show("Введите серию свидетельства", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SeriesTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(CertificateData.CertificateNumber))
            {
                MessageBox.Show("Введите номер свидетельства", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NumberTextBox.Focus();
                return;
            }

            if (CertificateData.IssueDate > DateTime.Now)
            {
                MessageBox.Show("Дата выдачи не может быть в будущем", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                IssueDatePicker.Focus();
                return;
            }

            if (CertificateData.VehicleCategoryId == 0)
            {
                MessageBox.Show("Выберите категорию", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoryComboBox.Focus();
                return;
            }

            // Устанавливаем код категории
            var selectedCategory = _categories.Categories.FirstOrDefault(c => c.Id == CertificateData.VehicleCategoryId);
            if (selectedCategory != null)
            {
                CertificateData.CategoryCode = selectedCategory.Code;
            }

            try
            {
                // TODO: Сохранить через сервис
                // _dataService.SaveCertificateData(CertificateData);

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