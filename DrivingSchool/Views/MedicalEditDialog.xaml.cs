using DrivingSchool.Models;
using DrivingSchool.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace DrivingSchool.Views
{
    public partial class MedicalEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        public StudentMedicalCertificate MedicalData { get; private set; }
        private readonly int _studentId;
        private readonly string _studentName;

        public MedicalEditDialog(SqlDataService dataService, int studentId, string studentName, StudentMedicalCertificate medicalData = null)
        {
            InitializeComponent();
            _dataService = dataService;
            _studentId = studentId;
            _studentName = studentName;

            if (medicalData != null)
            {
                MedicalData = medicalData;
                Title = "Редактирование медицинской справки";
            }
            else
            {
                MedicalData = new StudentMedicalCertificate
                {
                    StudentId = studentId,
                    IssueDate = DateTime.Now,
                    ValidUntil = DateTime.Now.AddYears(1),
                    Region = "Оренбургская область"
                };
                Title = "Добавление медицинской справки";
            }

            DataContext = MedicalData;
            StudentNameTextBox.Text = studentName;

            IssueDatePicker.SelectedDateChanged += IssueDateChanged;
        }

        private void IssueDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IssueDatePicker.SelectedDate.HasValue)
            {
                // По умолчанию справка действительна 1 год
                MedicalData.ValidUntil = IssueDatePicker.SelectedDate.Value.AddYears(1);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверка обязательных полей
            if (string.IsNullOrWhiteSpace(MedicalData.Series))
            {
                MessageBox.Show("Введите серию справки", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SeriesTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(MedicalData.Number))
            {
                MessageBox.Show("Введите номер справки", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NumberTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(MedicalData.MedicalInstitution))
            {
                MessageBox.Show("Введите название медицинского учреждения", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                InstitutionTextBox.Focus();
                return;
            }

            if (MedicalData.IssueDate > DateTime.Now)
            {
                MessageBox.Show("Дата выдачи не может быть в будущем", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                IssueDatePicker.Focus();
                return;
            }

            if (MedicalData.ValidUntil <= MedicalData.IssueDate)
            {
                MessageBox.Show("Дата окончания действия должна быть позже даты выдачи", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ValidUntilPicker.Focus();
                return;
            }

            try
            {
                // TODO: Сохранить через сервис
                // _dataService.SaveMedicalData(MedicalData);

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