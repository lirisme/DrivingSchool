using DrivingSchool.Models;
using DrivingSchool.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DrivingSchool.Views
{
    public partial class DrivingLicenseEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        public StudentDrivingLicense LicenseData { get; private set; }
        private readonly int _studentId;
        private readonly string _studentName;
        private bool _isDirty = false;

        public DrivingLicenseEditDialog(SqlDataService dataService, int studentId, string studentName, StudentDrivingLicense licenseData = null)
        {
            InitializeComponent();
            _dataService = dataService;
            _studentId = studentId;
            _studentName = studentName;

            if (licenseData != null)
            {
                LicenseData = licenseData;
                Title = "Редактирование водительского удостоверения";
            }
            else
            {
                LicenseData = new StudentDrivingLicense
                {
                    StudentId = studentId,
                    IssueDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddYears(10),
                    Status = "Действительно"
                };
                Title = "Добавление водительского удостоверения";
            }

            DataContext = LicenseData;
            StudentNameTextBox.Text = studentName;
            SubscribeToChanges();

            IssueDatePicker.SelectedDateChanged += IssueDateChanged;
            UpdateExperience();
        }

        private void SubscribeToChanges()
        {
            NumberTextBox.TextChanged += (s, e) => _isDirty = true;
            CategoriesTextBox.TextChanged += (s, e) => _isDirty = true;
            IssuedByTextBox.TextChanged += (s, e) => _isDirty = true;
            DivisionCodeTextBox.TextChanged += (s, e) => _isDirty = true;
            IssueDatePicker.SelectedDateChanged += (s, e) => _isDirty = true;
            ExpiryDatePicker.SelectedDateChanged += (s, e) => _isDirty = true;
            ExperienceYearTextBox.TextChanged += (s, e) => _isDirty = true;
            StatusComboBox.SelectionChanged += (s, e) => _isDirty = true;
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
                    if (SaveLicense())
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

        private void IssueDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateExperience();

            // По умолчанию срок действия 10 лет
            if (IssueDatePicker.SelectedDate.HasValue)
            {
                LicenseData.ExpiryDate = IssueDatePicker.SelectedDate.Value.AddYears(10);
            }
        }

        private void UpdateExperience()
        {
            // Если указан год начала стажа
            if (!string.IsNullOrEmpty(LicenseData.ExperienceStartYear) && int.TryParse(LicenseData.ExperienceStartYear, out int startYear))
            {
                int currentYear = DateTime.Now.Year;
                LicenseData.ExperienceYears = Math.Max(0, currentYear - startYear);
            }
            else if (IssueDatePicker.SelectedDate.HasValue)
            {
                // Иначе считаем от даты выдачи
                var today = DateTime.Today;
                var issueDate = IssueDatePicker.SelectedDate.Value;
                var years = today.Year - issueDate.Year;
                if (issueDate.Date > today.AddYears(-years))
                {
                    years--;
                }
                LicenseData.ExperienceYears = Math.Max(0, years);
            }
        }

        // ==================== МАСКА ДЛЯ НОМЕРА ВУ (11 ЦИФР) ====================

        private void NumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }

            var tb = sender as TextBox;
            if (tb == null) return;

            if (tb.Text.Length >= 20)
            {
                e.Handled = true;
                return;
            }
        }

        // ==================== МАСКА ДЛЯ ГОДА ====================

        private void YearTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }

            var tb = sender as TextBox;
            if (tb == null) return;

            if (tb.Text.Length >= 4)
            {
                e.Handled = true;
                return;
            }
        }

        // ==================== АВТОМАТИЧЕСКИЙ ВЕРХНИЙ РЕГИСТР ====================

        private void IssuedByTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null && tb.Text != tb.Text.ToUpper())
            {
                int caret = tb.CaretIndex;
                tb.Text = tb.Text.ToUpper();
                tb.CaretIndex = Math.Min(caret, tb.Text.Length);
                LicenseData.IssuedBy = tb.Text;
            }
        }

        // ==================== ВАЛИДАЦИЯ ====================

        private bool ValidateLicense()
        {
            if (string.IsNullOrWhiteSpace(LicenseData.Number))
            {
                MessageBox.Show("Введите номер водительского удостоверения (11 цифр)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NumberTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(LicenseData.Number))
            {
                MessageBox.Show("Введите номер водительского удостоверения", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NumberTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(LicenseData.LicenseCateg))
            {
                MessageBox.Show("Введите категории", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CategoriesTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(LicenseData.IssuedBy))
            {
                MessageBox.Show("Введите кем выдано удостоверение", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                IssuedByTextBox.Focus();
                return false;
            }

            if (LicenseData.IssueDate > DateTime.Now)
            {
                MessageBox.Show("Дата выдачи не может быть в будущем", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                IssueDatePicker.Focus();
                return false;
            }

            if (LicenseData.ExpiryDate <= LicenseData.IssueDate)
            {
                MessageBox.Show("Дата окончания должна быть позже даты выдачи", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ExpiryDatePicker.Focus();
                return false;
            }

            return true;
        }

        private bool CheckDuplicateLicense()
        {
            var existingLicenses = _dataService.LoadDrivingLicenses();
            var duplicate = existingLicenses.Licenses.Any(l =>
                l.StudentId == _studentId &&
                l.Number == LicenseData.Number &&
                l.Id != LicenseData.Id);

            if (duplicate)
            {
                MessageBox.Show("Водительское удостоверение с таким номером уже существует для этого студента",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private bool SaveLicense()
        {
            try
            {
                _dataService.SaveDrivingLicense(LicenseData);
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
            if (!ValidateLicense()) return;
            if (!CheckDuplicateLicense()) return;

            if (SaveLicense())
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