using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class CertificateEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        private readonly ExamService _examService;
        public StudentCertificate CertificateData { get; private set; }
        private readonly int _studentId;
        private readonly string _studentName;
        private VehicleCategoryCollection _categories;
        private bool _isDirty = false;
        private StudentExamStatus _examStatus;
        private string _seriesBeforeFormat = "";
        private string _numberBeforeFormat = "";

        public CertificateEditDialog(SqlDataService dataService, ExamService examService, int studentId, string studentName, StudentCertificate certificateData = null)
        {
            InitializeComponent();
            _dataService = dataService;
            _examService = examService;
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
            SubscribeToChanges();
            LoadExamStatus();
        }

        private void LoadCategories()
        {
            CategoryComboBox.ItemsSource = _categories.Categories;

            if (CertificateData.VehicleCategoryId == 0 && _categories.Categories.Any())
            {
                CertificateData.VehicleCategoryId = _categories.Categories.First().Id;
                LoadExamStatus();
            }
            else if (CertificateData.VehicleCategoryId != 0)
            {
                CategoryComboBox.SelectedValue = CertificateData.VehicleCategoryId;
            }
        }

        private async void LoadExamStatus()
        {
            try
            {
                var category = _categories.Categories.FirstOrDefault(c => c.Id == CertificateData.VehicleCategoryId);
                _examStatus = await _examService.GetStudentExamStatusWithCategoryAsync(_studentId, category?.Code ?? "B");

                if (_examStatus != null)
                {
                    if (_examStatus.InternalTheoryPassed && _examStatus.InternalPracticePassed)
                    {
                        ExamStatusText.Text = $"✅ Внутренние экзамены сданы! Можно выдать свидетельство.";
                        ExamStatusText.Foreground = System.Windows.Media.Brushes.Green;
                    }
                    else
                    {
                        ExamStatusText.Text = $"❌ Внутренние экзамены НЕ сданы!\n" +
                                             $"Теория: {(_examStatus.InternalTheoryPassed ? "✅" : "❌")}\n" +
                                             $"Практика: {(_examStatus.InternalPracticePassed ? "✅" : "❌")}";
                        ExamStatusText.Foreground = System.Windows.Media.Brushes.Red;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки статуса экзаменов: {ex.Message}");
                ExamStatusText.Text = "Не удалось загрузить статус экзаменов";
            }
        }

        private void SubscribeToChanges()
        {
            CategoryComboBox.SelectionChanged += (s, e) => { _isDirty = true; LoadExamStatus(); };
            SeriesTextBox.TextChanged += (s, e) => _isDirty = true;
            NumberTextBox.TextChanged += (s, e) => _isDirty = true;
            IssueDatePicker.SelectedDateChanged += (s, e) => _isDirty = true;
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
                    if (SaveCertificate())
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

        // ==================== ОБРАБОТКА ПОЛЕЙ БЕЗ ЖЕСТКОЙ МАСКИ ====================

        private void SeriesTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            _seriesBeforeFormat = tb.Text;
            tb.CaretIndex = tb.Text.Length;
        }

        private void SeriesTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            CertificateData.CertificateSeries = tb.Text.Trim();
        }

        private void NumberTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            _numberBeforeFormat = tb.Text;
            tb.CaretIndex = tb.Text.Length;
        }

        private void NumberTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            CertificateData.CertificateNumber = tb.Text.Trim();
        }

        private void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadExamStatus();
        }

        // ==================== ВАЛИДАЦИЯ ====================

        private bool CanIssueCertificate()
        {
            if (_examStatus == null)
            {
                var result = MessageBox.Show(
                    "Не удалось проверить статус экзаменов. Продолжить сохранение?",
                    "Предупреждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                return result == MessageBoxResult.Yes;
            }

            if (!_examStatus.InternalTheoryPassed || !_examStatus.InternalPracticePassed)
            {
                MessageBox.Show($"Студент не сдал внутренние экзамены!\n\n" +
                               "Свидетельство об окончании выдается только после успешной сдачи:\n" +
                               $"• Теория: {(_examStatus.InternalTheoryPassed ? "✅" : "❌")}\n" +
                               $"• Практика: {(_examStatus.InternalPracticePassed ? "✅" : "❌")}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private bool CheckDuplicateCertificate()
        {
            var existingCertificates = _dataService.LoadCertificates();
            var duplicate = existingCertificates.Certificates.Any(c =>
                c.StudentId == _studentId &&
                c.CertificateSeries == CertificateData.CertificateSeries &&
                c.CertificateNumber == CertificateData.CertificateNumber &&
                c.VehicleCategoryId == CertificateData.VehicleCategoryId &&
                c.Id != CertificateData.Id);

            if (duplicate)
            {
                MessageBox.Show("Свидетельство с такой серией, номером и категорией уже существует для этого студента",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private bool ValidateCertificate()
        {
            if (string.IsNullOrWhiteSpace(CertificateData.CertificateSeries))
            {
                MessageBox.Show("Введите серию свидетельства", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SeriesTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(CertificateData.CertificateNumber))
            {
                MessageBox.Show("Введите номер свидетельства", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NumberTextBox.Focus();
                return false;
            }

            if (CertificateData.IssueDate > DateTime.Now)
            {
                MessageBox.Show("Дата выдачи не может быть в будущем", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                IssueDatePicker.Focus();
                return false;
            }

            if (CertificateData.VehicleCategoryId == 0)
            {
                MessageBox.Show("Выберите категорию", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoryComboBox.Focus();
                return false;
            }

            return true;
        }

        private bool SaveCertificate()
        {
            try
            {
                int newId = _dataService.SaveCertificateData(CertificateData);

                if (CertificateData.Id == 0)
                {
                    CertificateData.Id = newId;
                    // Обновляем статус студента - свидетельство выдано
                    Task.Run(() => _dataService.UpdateStudentCertificateStatusAsync(_studentId, true));
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
            if (!ValidateCertificate()) return;
            if (!CanIssueCertificate()) return;
            if (!CheckDuplicateCertificate()) return;

            if (SaveCertificate())
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

        private void Window_PreviewKфeyDown(object sender, KeyEventArgs e)
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