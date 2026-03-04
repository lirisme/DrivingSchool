using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;
using DrivingSchool.Services;
using System.Collections.Generic;
using System.Diagnostics;

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
                Debug.WriteLine("=== ЗАГРУЗКА ВОДИТЕЛЬСКИХ УДОСТОВЕРЕНИЙ ===");

                _students = _dataService.LoadStudents() ?? new StudentCollection { Students = new List<Student>() };
                Debug.WriteLine($"Загружено студентов: {_students.Students.Count}");

                // ГЛАВНОЕ ИСПРАВЛЕНИЕ: УБРАЛИ ЗАГЛУШКУ, ЗАГРУЖАЕМ РЕАЛЬНЫЕ ДАННЫЕ!
                _licenses = _dataService.LoadDrivingLicenses() ?? new StudentDrivingLicenseCollection { Licenses = new List<StudentDrivingLicense>() };
                Debug.WriteLine($"Загружено удостоверений: {_licenses.Licenses.Count}");

                // Отладка: выводим все загруженные удостоверения
                foreach (var license in _licenses.Licenses)
                {
                    Debug.WriteLine($"  Удостоверение ID={license.Id}, Студент ID={license.StudentId}, Серия={license.Series}, Номер={license.Number}");
                }

                ApplyFilter();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА загрузки данных: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _students = new StudentCollection { Students = new List<Student>() };
                _licenses = new StudentDrivingLicenseCollection { Licenses = new List<StudentDrivingLicense>() };
            }
        }

        private void ApplyFilter()
        {
            try
            {
                Debug.WriteLine("=== ПРИМЕНЕНИЕ ФИЛЬТРА ===");

                if (_selectedStudent != null)
                {
                    Debug.WriteLine($"Выбран студент ID={_selectedStudent.Id}, Name={_selectedStudent.FullName}");

                    var filtered = _licenses.Licenses
                        .Where(l => l.StudentId == _selectedStudent.Id)
                        .Select(l =>
                        {
                            l.StudentName = _selectedStudent.FullName;
                            return l;
                        })
                        .ToList();

                    Debug.WriteLine($"Найдено удостоверений для студента: {filtered.Count}");

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
                    Debug.WriteLine("Студент не выбран, показываем все удостоверения");

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
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА в ApplyFilter: {ex.Message}");
            }
        }

        private string GetStudentName(int studentId)
        {
            var student = _students?.Students?.FirstOrDefault(s => s.Id == studentId);
            return student?.FullName ?? "Неизвестный студент";
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                var searchText = SearchTextBox.Text?.ToLower() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    SearchResultsListBox.Visibility = Visibility.Collapsed;
                    return;
                }

                var results = _students?.Students?
                    .Where(s => (s.LastName ?? "").ToLower().Contains(searchText) ||
                               (s.FirstName ?? "").ToLower().Contains(searchText) ||
                               (s.Phone ?? "").Contains(searchText))
                    .Take(10)
                    .ToList() ?? new List<Student>();

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
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка поиска: {ex.Message}");
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
            try
            {
                var hasStudent = _selectedStudent != null;

                // Проверяем наличие удостоверения для выбранного студента
                var hasLicense = hasStudent && _licenses.Licenses.Any(l => l.StudentId == _selectedStudent.Id);
                var hasSelection = LicenseGrid.SelectedItem != null;

                Debug.WriteLine($"UpdateButtonsAvailability: hasStudent={hasStudent}, hasLicense={hasLicense}, hasSelection={hasSelection}");

                // ИСПРАВЛЕНИЕ: как в паспортах - только одно удостоверение на студента!
                AddLicenseButton.IsEnabled = hasStudent && !hasLicense;  // Можно добавить только если нет
                EditLicenseButton.IsEnabled = hasLicense && hasSelection;
                DeleteLicenseButton.IsEnabled = hasLicense && hasSelection;
                ViewLicenseButton.IsEnabled = hasLicense && hasSelection;
                PrintLicenseButton.IsEnabled = hasLicense && hasSelection;

                // Визуальная индикация
                AddLicenseButton.Opacity = AddLicenseButton.IsEnabled ? 1.0 : 0.5;
                EditLicenseButton.Opacity = EditLicenseButton.IsEnabled ? 1.0 : 0.5;
                DeleteLicenseButton.Opacity = DeleteLicenseButton.IsEnabled ? 1.0 : 0.5;
                ViewLicenseButton.Opacity = ViewLicenseButton.IsEnabled ? 1.0 : 0.5;
                PrintLicenseButton.Opacity = PrintLicenseButton.IsEnabled ? 1.0 : 0.5;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА в UpdateButtonsAvailability: {ex.Message}");
            }
        }

        private void LicenseGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsAvailability();
        }

        private void AddLicense_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStudent == null)
            {
                MessageBox.Show("Выберите студента", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Debug.WriteLine($"Добавление удостоверения для студента ID={_selectedStudent.Id}");

                var dialog = new DrivingLicenseEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    Debug.WriteLine("Диалог закрыт с OK, перезагружаем данные");

                    // ИСПРАВЛЕНИЕ: перезагружаем данные после сохранения
                    _licenses = _dataService.LoadDrivingLicenses() ?? new StudentDrivingLicenseCollection { Licenses = new List<StudentDrivingLicense>() };

                    Debug.WriteLine($"После перезагрузки удостоверений: {_licenses.Licenses.Count}");

                    ApplyFilter();

                    MessageBox.Show("Водительское удостоверение успешно добавлено!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    Debug.WriteLine("Диалог закрыт с Cancel");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА при добавлении: {ex.Message}");
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

            if (_selectedStudent == null)
            {
                MessageBox.Show("Выберите студента", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Debug.WriteLine($"Редактирование удостоверения ID={selectedLicense.Id}");

                var dialog = new DrivingLicenseEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName, selectedLicense);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    Debug.WriteLine("Диалог закрыт с OK, перезагружаем данные");

                    // ИСПРАВЛЕНИЕ: перезагружаем данные после сохранения
                    _licenses = _dataService.LoadDrivingLicenses() ?? new StudentDrivingLicenseCollection { Licenses = new List<StudentDrivingLicense>() };

                    ApplyFilter();

                    MessageBox.Show("Водительское удостоверение успешно обновлено!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА при редактировании: {ex.Message}");
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

            if (_selectedStudent == null)
            {
                MessageBox.Show("Выберите студента", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Удалить водительское удостоверение студента {_selectedStudent.FullName}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    Debug.WriteLine($"Удаление удостоверения ID={selectedLicense.Id}");

                    _dataService.DeleteDrivingLicense(selectedLicense.Id);

                    // ИСПРАВЛЕНИЕ: перезагружаем данные после удаления
                    _licenses = _dataService.LoadDrivingLicenses() ?? new StudentDrivingLicenseCollection { Licenses = new List<StudentDrivingLicense>() };

                    ApplyFilter();

                    MessageBox.Show("Водительское удостоверение удалено.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ОШИБКА при удалении: {ex.Message}");
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

            if (_selectedStudent == null)
            {
                MessageBox.Show("Выберите студента", "Предупреждение",
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

            if (_selectedStudent == null)
            {
                MessageBox.Show("Выберите студента", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"Печать водительского удостоверения:\n\n{selectedLicense.Series} {selectedLicense.Number}\nСтудент: {_selectedStudent.FullName}", "Печать",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            SearchResultsListBox.Visibility = Visibility.Collapsed;
        }

        // НОВЫЙ МЕТОД: очистка выбранного студента
        private void ClearSelectedStudent_Click(object sender, RoutedEventArgs e)
        {
            _selectedStudent = null;
            SelectedStudentPanel.Visibility = Visibility.Collapsed;
            ApplyFilter();
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