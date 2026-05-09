using DrivingSchool.Models;
using DrivingSchool.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DrivingSchool.Views
{
    public partial class MedicalEditDialog : Window
    {
        private readonly SqlDataService _dataService;
        public StudentMedicalCertificate MedicalData { get; private set; }
        private readonly int _studentId;
        private readonly string _studentName;
        private bool _isDirty = false;

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
            SubscribeToChanges();

            // Форматируем серию и номер при загрузке
            if (!string.IsNullOrEmpty(MedicalData.Series))
            {
                SeriesTextBox.Text = FormatSeries(MedicalData.Series);
            }
            if (!string.IsNullOrEmpty(MedicalData.Number))
            {
                NumberTextBox.Text = MedicalData.Number;
            }
        }

        private void SubscribeToChanges()
        {
            SeriesTextBox.TextChanged += (s, e) => _isDirty = true;
            NumberTextBox.TextChanged += (s, e) => _isDirty = true;
            IssueDatePicker.SelectedDateChanged += (s, e) => _isDirty = true;
            ValidUntilPicker.SelectedDateChanged += (s, e) => _isDirty = true;
            InstitutionTextBox.TextChanged += (s, e) => _isDirty = true;
            CategoriesTextBox.TextChanged += (s, e) => _isDirty = true;
            RegionTextBox.TextChanged += (s, e) => _isDirty = true;
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
                    if (SaveMedical())
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

        // ==================== МАСКА ДЛЯ СЕРИИ ====================

        private string FormatSeries(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            var digits = new string(text.Where(char.IsDigit).ToArray());

            if (digits.Length == 0)
                return "";

            if (digits.Length <= 2)
                return digits;

            if (digits.Length <= 4)
                return $"{digits.Substring(0, 2)} {digits.Substring(2)}";

            return $"{digits.Substring(0, 2)} {digits.Substring(2, 2)}";
        }

        private string GetDigitsFromSeries(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            return new string(text.Where(char.IsDigit).ToArray());
        }

        private void SeriesTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null)
            {
                string digits = GetDigitsFromSeries(tb.Text);
                tb.Text = digits;
                tb.CaretIndex = tb.Text.Length;
            }
        }

        private void SeriesTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null)
            {
                string digits = GetDigitsFromSeries(tb.Text);
                if (digits.Length == 4)
                {
                    tb.Text = FormatSeries(digits);
                }
                MedicalData.Series = digits;
            }
        }

        private void SeriesTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }

            var tb = sender as TextBox;
            if (tb == null) return;

            string currentText = tb.Text;
            string digits = GetDigitsFromSeries(currentText);

            if (digits.Length >= 4)
            {
                e.Handled = true;
                return;
            }
        }

        // ==================== МАСКА ДЛЯ НОМЕРА ====================

        private void NumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
                return;
            }

            var tb = sender as TextBox;
            if (tb == null) return;

            if (tb.Text.Length >= 6)
            {
                e.Handled = true;
                return;
            }
        }

        // ==================== АВТОМАТИЧЕСКИЙ ВЕРХНИЙ РЕГИСТР ====================

        private void InstitutionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null && tb.Text != tb.Text.ToUpper())
            {
                int caret = tb.CaretIndex;
                tb.Text = tb.Text.ToUpper();
                tb.CaretIndex = Math.Min(caret, tb.Text.Length);
                MedicalData.MedicalInstitution = tb.Text;
            }
        }

        private void CategoriesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null && tb.Text != tb.Text.ToUpper())
            {
                int caret = tb.CaretIndex;
                tb.Text = tb.Text.ToUpper();
                tb.CaretIndex = Math.Min(caret, tb.Text.Length);
                MedicalData.Categories = tb.Text;

                // Автоматический пересчет срока действия при изменении категорий
                UpdateValidityPeriod();
            }
        }

        private void RegionTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb != null && tb.Text != tb.Text.ToUpper())
            {
                int caret = tb.CaretIndex;
                tb.Text = tb.Text.ToUpper();
                tb.CaretIndex = Math.Min(caret, tb.Text.Length);
                MedicalData.Region = tb.Text;
            }
        }

        // ==================== АВТОМАТИЧЕСКИЙ РАСЧЕТ СРОКА ДЕЙСТВИЯ ====================

        private void IssueDateChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateValidityPeriod();
        }

        private void UpdateValidityPeriod()
        {
            if (!IssueDatePicker.SelectedDate.HasValue) return;

            var issueDate = IssueDatePicker.SelectedDate.Value;
            int years = 1; // по умолчанию 1 год

            // Для категорий C, D, CE, DE - 2 года
            if (!string.IsNullOrEmpty(MedicalData.Categories))
            {
                var categories = MedicalData.Categories.ToUpper();
                if (categories.Contains("C") || categories.Contains("D") ||
                    categories.Contains("CE") || categories.Contains("DE"))
                {
                    years = 2;
                }
            }

            MedicalData.ValidUntil = issueDate.AddYears(years);
            ValidUntilPicker.SelectedDate = MedicalData.ValidUntil;

            ValidityInfoText.Text = years == 2
                ? "Для категорий C, D, CE, DE срок действия справки - 2 года"
                : "Срок действия справки - 1 год";
        }

        // ==================== ВАЛИДАЦИЯ ====================

        private bool ValidateMedical()
        {
            string seriesDigits = GetDigitsFromSeries(SeriesTextBox.Text);
            if (seriesDigits.Length != 4)
            {
                MessageBox.Show("Серия справки должна содержать 4 цифры", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SeriesTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(MedicalData.Number))
            {
                MessageBox.Show("Введите номер справки (6 цифр)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NumberTextBox.Focus();
                return false;
            }

            if (MedicalData.Number.Length != 6)
            {
                MessageBox.Show("Номер справки должен содержать 6 цифр", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NumberTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(MedicalData.MedicalInstitution))
            {
                MessageBox.Show("Введите название медицинского учреждения", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                InstitutionTextBox.Focus();
                return false;
            }

            if (MedicalData.IssueDate > DateTime.Now)
            {
                MessageBox.Show("Дата выдачи не может быть в будущем", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                IssueDatePicker.Focus();
                return false;
            }

            if (MedicalData.ValidUntil <= MedicalData.IssueDate)
            {
                MessageBox.Show("Дата окончания действия должна быть позже даты выдачи", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ValidUntilPicker.Focus();
                return false;
            }

            return true;
        }

        private bool CheckDuplicateMedical()
        {
            var existingCertificates = _dataService.LoadMedicalData();
            var duplicate = existingCertificates.Certificates.Any(c =>
                c.StudentId == _studentId &&
                c.Series == MedicalData.Series &&
                c.Number == MedicalData.Number &&
                c.Id != MedicalData.Id);

            if (duplicate)
            {
                MessageBox.Show("Справка с такой серией и номером уже существует для этого студента",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private bool SaveMedical()
        {
            try
            {
                _dataService.SaveMedicalData(MedicalData);
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
            if (!ValidateMedical()) return;
            if (!CheckDuplicateMedical()) return;

            if (SaveMedical())
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