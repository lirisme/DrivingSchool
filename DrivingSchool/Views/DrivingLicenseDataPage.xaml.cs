using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class DrivingLicenseDataPage : Page
    {
        private readonly SqlDataService _dataService;
        private StudentCollection _students;
        private StudentDrivingLicenseCollection _licenses;
        private Student _selectedStudent;

        public DrivingLicenseDataPage(SqlDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                _students = _dataService.LoadStudents();
                // TODO: LoadDrivingLicenses нужно добавить в SqlDataService
                _licenses = new StudentDrivingLicenseCollection { Licenses = new System.Collections.Generic.List<StudentDrivingLicense>() };

                // Пока заглушка
                // _licenses = _dataService.LoadDrivingLicenses();

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _students = new StudentCollection { Students = new System.Collections.Generic.List<Student>() };
                _licenses = new StudentDrivingLicenseCollection { Licenses = new System.Collections.Generic.List<StudentDrivingLicense>() };
            }
        }

        private void ApplyFilter()
        {
            if (_selectedStudent != null)
            {
                var filtered = _licenses.Licenses
                    .Where(l => l.StudentId == _selectedStudent.Id)
                    .Select(l =>
                    {
                        l.StudentName = _selectedStudent.FullName;
                        return l;
                    })
                    .ToList();

                LicenseGrid.ItemsSource = filtered;

                if (filtered.Any())
                {
                    var validCount = filtered.Count(l => l.IsValid);
                    InfoTextBlock.Text = $"Водительские удостоверения студента {_selectedStudent.FullName} (действительных: {validCount})";
                }
                else
                {
                    InfoTextBlock.Text = $"У студента {_selectedStudent.FullName} нет водительских удостоверений";
                }
            }
            else
            {
                var allLicenses = _licenses.Licenses
                    .Select(l =>
                    {
                        l.StudentName = GetStudentName(l.StudentId);
                        return l;
                    })
                    .ToList();

                LicenseGrid.ItemsSource = allLicenses;

                var totalCount = _licenses.Licenses.Count;
                var expiredCount = _licenses.Licenses.Count(l => !l.IsValid);

                InfoTextBlock.Text = $"Всего записей: {totalCount} (просрочено: {expiredCount}). Выберите студента для добавления/редактирования";
            }

            UpdateButtonsAvailability();
        }

        private string GetStudentName(int studentId)
        {
            var student = _students.Students.FirstOrDefault(s => s.Id == studentId);
            return student?.FullName ?? "Неизвестный студент";
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text?.ToLower() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                SearchResultsListBox.Visibility = Visibility.Collapsed;
                return;
            }

            var results = _students.Students
                .Where(s => (s.LastName ?? "").ToLower().Contains(searchText) ||
                           (s.FirstName ?? "").ToLower().Contains(searchText) ||
                           (s.Phone ?? "").Contains(searchText))
                .Take(10)
                .ToList();

            if (results.Any())
            {
                SearchResultsListBox.ItemsSource = results;
                SearchResultsListBox.Visibility = Visibility.Visible;
            }
            else
            {
                SearchResultsListBox.Visibility = Visibility.Collapsed;
            }
        }

        private void SearchResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SearchResultsListBox.SelectedItem is Student selectedStudent)
            {
                _selectedStudent = selectedStudent;
                UpdateSelectedStudentPanel();
                SearchResultsListBox.Visibility = Visibility.Collapsed;
                SearchTextBox.Text = string.Empty;
                ApplyFilter();
            }
        }

        private void UpdateSelectedStudentPanel()
        {
            if (_selectedStudent != null)
            {
                SelectedStudentPanel.Visibility = Visibility.Visible;
                SelectedStudentText.Text = _selectedStudent.FullName;
                SelectedStudentDetails.Text = $"Телефон: {_selectedStudent.Phone} | ID: {_selectedStudent.Id}";
            }
            else
            {
                SelectedStudentPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateButtonsAvailability()
        {
            var hasStudent = _selectedStudent != null;
            var hasLicense = hasStudent && _licenses.Licenses.Any(l => l.StudentId == _selectedStudent.Id);
            var hasSelection = LicenseGrid.SelectedItem != null;

            AddLicenseButton.IsEnabled = hasStudent && !hasLicense;
            EditLicenseButton.IsEnabled = hasLicense && hasSelection;
            DeleteLicenseButton.IsEnabled = hasLicense && hasSelection;
            ViewLicenseButton.IsEnabled = hasLicense && hasSelection;
            PrintLicenseButton.IsEnabled = hasLicense && hasSelection;
        }

        private void LicenseGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsAvailability();
        }

        private void AddLicense_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new DrivingLicenseEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    // TODO: Обновить данные
                    // LoadData();
                    MessageBox.Show("Водительское удостоверение успешно добавлено!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditLicense_Click(object sender, RoutedEventArgs e)
        {
            if (!(LicenseGrid.SelectedItem is StudentDrivingLicense selectedLicense))
            {
                MessageBox.Show("Выберите запись для редактирования", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dialog = new DrivingLicenseEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName, selectedLicense);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    // TODO: Обновить данные
                    // LoadData();
                    MessageBox.Show("Водительское удостоверение успешно обновлено!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteLicense_Click(object sender, RoutedEventArgs e)
        {
            if (!(LicenseGrid.SelectedItem is StudentDrivingLicense selectedLicense))
            {
                MessageBox.Show("Выберите запись для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Удалить водительское удостоверение студента {_selectedStudent.FullName}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    // TODO: Удалить через сервис
                    // _dataService.DeleteDrivingLicense(selectedLicense.Id);

                    MessageBox.Show("Водительское удостоверение удалено.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewLicense_Click(object sender, RoutedEventArgs e)
        {
            if (!(LicenseGrid.SelectedItem is StudentDrivingLicense selectedLicense))
            {
                MessageBox.Show("Выберите запись для просмотра", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var status = selectedLicense.IsValid ? "✅ Действительно" : "❌ Просрочено";
            var daysLeft = (selectedLicense.ExpiryDate - DateTime.Today).Days;

            MessageBox.Show(
                $"Водительское удостоверение:\n\n" +
                $"Студент: {_selectedStudent.FullName}\n" +
                $"Серия: {selectedLicense.Series}\n" +
                $"Номер: {selectedLicense.Number}\n" +
                $"Категории: {selectedLicense.CategoriesDisplay}\n" +
                $"Кем выдано: {selectedLicense.IssuedBy}\n" +
                $"Код подразделения: {selectedLicense.DivisionCode}\n" +
                $"Дата выдачи: {selectedLicense.IssueDate:dd.MM.yyyy}\n" +
                $"Действительно до: {selectedLicense.ExpiryDate:dd.MM.yyyy}\n" +
                $"Статус: {status}\n" +
                $"Осталось дней: {daysLeft}\n" +
                $"Стаж: {selectedLicense.ExperienceYears} лет",
                "Просмотр водительского удостоверения",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void PrintLicense_Click(object sender, RoutedEventArgs e)
        {
            if (!(LicenseGrid.SelectedItem is StudentDrivingLicense selectedLicense))
            {
                MessageBox.Show("Выберите запись для печати", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // TODO: Реализовать печать
            MessageBox.Show($"Печать водительского удостоверения:\n\n{selectedLicense.Series} {selectedLicense.Number}", "Печать",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            SearchResultsListBox.Visibility = Visibility.Collapsed;
        }

        private void LicenseGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (LicenseGrid.SelectedItem != null && _selectedStudent != null)
            {
                EditLicense_Click(sender, e);
            }
        }
    }
}