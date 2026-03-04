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
    public partial class PassportDataPage : Page
    {
        private readonly SqlDataService _dataService;
        private StudentCollection _students;
        private StudentPassportDataCollection _passports;
        private Student _selectedStudent;

        public PassportDataPage(SqlDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                Debug.WriteLine("=== ЗАГРУЗКА ДАННЫХ ===");

                _students = _dataService.LoadStudents() ?? new StudentCollection { Students = new List<Student>() };
                Debug.WriteLine($"Загружено студентов: {_students.Students.Count}");

                _passports = _dataService.LoadPassportData() ?? new StudentPassportDataCollection { Passports = new List<StudentPassportData>() };
                Debug.WriteLine($"Загружено паспортов: {_passports.Passports.Count}");

                ApplyFilter();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА загрузки данных: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _students = new StudentCollection { Students = new List<Student>() };
                _passports = new StudentPassportDataCollection { Passports = new List<StudentPassportData>() };
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

                    var filtered = _passports.Passports
                        .Where(p => p.StudentId == _selectedStudent.Id)
                        .Select(p =>
                        {
                            p.StudentName = _selectedStudent.FullName;
                            return p;
                        })
                        .ToList();

                    Debug.WriteLine($"Найдено паспортов для студента: {filtered.Count}");

                    // Выводим все найденные паспорта в отладку
                    foreach (var p in filtered)
                    {
                        Debug.WriteLine($"  Паспорт ID={p.Id}, Серия={p.Series}, Номер={p.Number}");
                    }

                    PassportGrid.ItemsSource = filtered;

                    if (filtered.Any())
                    {
                        InfoTextBlock.Text = $"Паспортные данные студента {_selectedStudent.FullName}";
                    }
                    else
                    {
                        InfoTextBlock.Text = $"У студента {_selectedStudent.FullName} нет паспортных данных";
                    }
                }
                else
                {
                    Debug.WriteLine("Студент не выбран, показываем все паспорта");

                    var allPassports = _passports.Passports
                        .Select(p =>
                        {
                            p.StudentName = GetStudentName(p.StudentId);
                            return p;
                        })
                        .ToList();

                    PassportGrid.ItemsSource = allPassports;
                    InfoTextBlock.Text = $"Всего записей: {_passports.Passports.Count}. Выберите студента для добавления/редактирования";
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

                // Проверяем наличие паспортных данных для выбранного студента
                var hasPassportData = hasStudent && _passports.Passports.Any(p => p.StudentId == _selectedStudent.Id);
                var hasSelection = PassportGrid.SelectedItem != null;

                Debug.WriteLine($"UpdateButtonsAvailability: hasStudent={hasStudent}, hasPassportData={hasPassportData}, hasSelection={hasSelection}");

                AddPassportButton.IsEnabled = hasStudent && !hasPassportData;
                EditPassportButton.IsEnabled = hasPassportData && hasSelection;
                DeletePassportButton.IsEnabled = hasPassportData && hasSelection;
                ViewPassportButton.IsEnabled = hasPassportData && hasSelection;

                // Визуальная индикация
                AddPassportButton.Opacity = AddPassportButton.IsEnabled ? 1.0 : 0.5;
                EditPassportButton.Opacity = EditPassportButton.IsEnabled ? 1.0 : 0.5;
                DeletePassportButton.Opacity = DeletePassportButton.IsEnabled ? 1.0 : 0.5;
                ViewPassportButton.Opacity = ViewPassportButton.IsEnabled ? 1.0 : 0.5;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА в UpdateButtonsAvailability: {ex.Message}");
            }
        }

        private void PassportGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsAvailability();
        }

        private void AddPassport_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStudent == null)
            {
                MessageBox.Show("Выберите студента", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Debug.WriteLine($"Добавление паспорта для студента ID={_selectedStudent.Id}");

                var dialog = new PassportEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    Debug.WriteLine("Диалог закрыт с OK, перезагружаем данные");

                    // Принудительно перезагружаем паспортные данные
                    _passports = _dataService.LoadPassportData() ?? new StudentPassportDataCollection { Passports = new List<StudentPassportData>() };

                    Debug.WriteLine($"После перезагрузки паспортов: {_passports.Passports.Count}");

                    ApplyFilter();

                    MessageBox.Show("Паспортные данные успешно добавлены!", "Успех",
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

        private void EditPassport_Click(object sender, RoutedEventArgs e)
        {
            if (!(PassportGrid.SelectedItem is StudentPassportData selectedPassport))
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
                Debug.WriteLine($"Редактирование паспорта ID={selectedPassport.Id}");

                var dialog = new PassportEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName, selectedPassport);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    Debug.WriteLine("Диалог закрыт с OK, перезагружаем данные");

                    // Принудительно перезагружаем паспортные данные
                    _passports = _dataService.LoadPassportData() ?? new StudentPassportDataCollection { Passports = new List<StudentPassportData>() };

                    ApplyFilter();

                    MessageBox.Show("Паспортные данные успешно обновлены!", "Успех",
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

        private void DeletePassport_Click(object sender, RoutedEventArgs e)
        {
            if (!(PassportGrid.SelectedItem is StudentPassportData selectedPassport))
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

            if (MessageBox.Show($"Удалить паспортные данные студента {_selectedStudent.FullName}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    Debug.WriteLine($"Удаление паспорта ID={selectedPassport.Id}");

                    _dataService.DeletePassportData(selectedPassport.Id);

                    // Принудительно перезагружаем паспортные данные
                    _passports = _dataService.LoadPassportData() ?? new StudentPassportDataCollection { Passports = new List<StudentPassportData>() };

                    ApplyFilter();

                    MessageBox.Show("Паспортные данные удалены.", "Успех",
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

        private void ViewPassport_Click(object sender, RoutedEventArgs e)
        {
            if (!(PassportGrid.SelectedItem is StudentPassportData selectedPassport))
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

            MessageBox.Show(
                $"Паспортные данные:\n\n" +
                $"Студент: {_selectedStudent.FullName}\n" +
                $"Тип документа: {selectedPassport.DocumentType}\n" +
                $"Серия: {selectedPassport.Series}\n" +
                $"Номер: {selectedPassport.Number}\n" +
                $"Кем выдан: {selectedPassport.IssuedBy}\n" +
                $"Код подразделения: {selectedPassport.DivisionCode}\n" +
                $"Дата выдачи: {selectedPassport.IssueDate:dd.MM.yyyy}",
                "Просмотр паспортных данных",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            SearchResultsListBox.Visibility = Visibility.Collapsed;
        }

        private void ClearSelectedStudent_Click(object sender, RoutedEventArgs e)
        {
            _selectedStudent = null;
            SelectedStudentPanel.Visibility = Visibility.Collapsed;
            ApplyFilter();
        }

        private void PassportGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PassportGrid.SelectedItem != null && _selectedStudent != null)
            {
                EditPassport_Click(sender, e);
            }
        }
    }
}