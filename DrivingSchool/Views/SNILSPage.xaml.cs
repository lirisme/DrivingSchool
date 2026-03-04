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
    public partial class SNILSPage : Page
    {
        private readonly SqlDataService _dataService;
        private StudentCollection _students;
        private StudentSNILSCollection _snilsList;
        private Student _selectedStudent;

        public SNILSPage(SqlDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                Debug.WriteLine("=== ЗАГРУЗКА ДАННЫХ СНИЛС ===");

                _students = _dataService.LoadStudents() ?? new StudentCollection { Students = new List<Student>() };
                Debug.WriteLine($"Загружено студентов: {_students.Students.Count}");

                // ИСПРАВЛЕНИЕ: загружаем реальные данные!
                _snilsList = _dataService.LoadSNILSData() ?? new StudentSNILSCollection { SNILSList = new List<StudentSNILS>() };
                Debug.WriteLine($"Загружено СНИЛС: {_snilsList.SNILSList.Count}");

                // Отладка: выводим все загруженные СНИЛС
                foreach (var snils in _snilsList.SNILSList)
                {
                    Debug.WriteLine($"  СНИЛС ID={snils.Id}, Студент ID={snils.StudentId}, Номер={snils.Number}");
                }

                ApplyFilter();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА загрузки данных: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _students = new StudentCollection { Students = new List<Student>() };
                _snilsList = new StudentSNILSCollection { SNILSList = new List<StudentSNILS>() };
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

                    var filtered = _snilsList.SNILSList
                        .Where(s => s.StudentId == _selectedStudent.Id)
                        .Select(s =>
                        {
                            s.StudentName = _selectedStudent.FullName;
                            return s;
                        })
                        .ToList();

                    Debug.WriteLine($"Найдено СНИЛС для студента: {filtered.Count}");

                    SNILSGrid.ItemsSource = filtered;

                    if (filtered.Any())
                    {
                        InfoTextBlock.Text = $"Данные СНИЛС студента {_selectedStudent.FullName}";
                    }
                    else
                    {
                        InfoTextBlock.Text = $"У студента {_selectedStudent.FullName} нет данных СНИЛС";
                    }
                }
                else
                {
                    Debug.WriteLine("Студент не выбран, показываем все СНИЛС");

                    var allSNILS = _snilsList.SNILSList
                        .Select(s =>
                        {
                            s.StudentName = GetStudentName(s.StudentId);
                            return s;
                        })
                        .ToList();

                    SNILSGrid.ItemsSource = allSNILS;
                    InfoTextBlock.Text = $"Всего записей: {_snilsList.SNILSList.Count}. Выберите студента для добавления/редактирования";
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

                // Проверяем наличие СНИЛС для выбранного студента
                var hasSNILSData = hasStudent && _snilsList.SNILSList.Any(s => s.StudentId == _selectedStudent.Id);
                var hasSelection = SNILSGrid.SelectedItem != null;

                Debug.WriteLine($"UpdateButtonsAvailability: hasStudent={hasStudent}, hasSNILSData={hasSNILSData}, hasSelection={hasSelection}");

                // ИСПРАВЛЕНИЕ: как в паспортах - один СНИЛС на студента!
                AddSNILSButton.IsEnabled = hasStudent && !hasSNILSData;
                EditSNILSButton.IsEnabled = hasSNILSData && hasSelection;
                DeleteSNILSButton.IsEnabled = hasSNILSData && hasSelection;
                ViewSNILSButton.IsEnabled = hasSNILSData && hasSelection;

                // Визуальная индикация
                AddSNILSButton.Opacity = AddSNILSButton.IsEnabled ? 1.0 : 0.5;
                EditSNILSButton.Opacity = EditSNILSButton.IsEnabled ? 1.0 : 0.5;
                DeleteSNILSButton.Opacity = DeleteSNILSButton.IsEnabled ? 1.0 : 0.5;
                ViewSNILSButton.Opacity = ViewSNILSButton.IsEnabled ? 1.0 : 0.5;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА в UpdateButtonsAvailability: {ex.Message}");
            }
        }

        private void SNILSGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsAvailability();
        }

        private void AddSNILS_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStudent == null)
            {
                MessageBox.Show("Выберите студента", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Debug.WriteLine($"Добавление СНИЛС для студента ID={_selectedStudent.Id}");

                var dialog = new SNILSEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    Debug.WriteLine("Диалог закрыт с OK, перезагружаем данные");

                    // ИСПРАВЛЕНИЕ: перезагружаем данные после сохранения
                    _snilsList = _dataService.LoadSNILSData() ?? new StudentSNILSCollection { SNILSList = new List<StudentSNILS>() };

                    Debug.WriteLine($"После перезагрузки СНИЛС: {_snilsList.SNILSList.Count}");

                    ApplyFilter();

                    MessageBox.Show("Данные СНИЛС успешно добавлены!", "Успех",
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

        private void EditSNILS_Click(object sender, RoutedEventArgs e)
        {
            if (!(SNILSGrid.SelectedItem is StudentSNILS selectedSNILS))
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
                Debug.WriteLine($"Редактирование СНИЛС ID={selectedSNILS.Id}");

                var dialog = new SNILSEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName, selectedSNILS);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    Debug.WriteLine("Диалог закрыт с OK, перезагружаем данные");

                    // ИСПРАВЛЕНИЕ: перезагружаем данные после сохранения
                    _snilsList = _dataService.LoadSNILSData() ?? new StudentSNILSCollection { SNILSList = new List<StudentSNILS>() };

                    ApplyFilter();

                    MessageBox.Show("Данные СНИЛС успешно обновлены!", "Успех",
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

        private void DeleteSNILS_Click(object sender, RoutedEventArgs e)
        {
            if (!(SNILSGrid.SelectedItem is StudentSNILS selectedSNILS))
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

            if (MessageBox.Show($"Удалить данные СНИЛС студента {_selectedStudent.FullName}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    Debug.WriteLine($"Удаление СНИЛС ID={selectedSNILS.Id}");

                    _dataService.DeleteSNILSData(selectedSNILS.Id);

                    // ИСПРАВЛЕНИЕ: перезагружаем данные после удаления
                    _snilsList = _dataService.LoadSNILSData() ?? new StudentSNILSCollection { SNILSList = new List<StudentSNILS>() };

                    ApplyFilter();

                    MessageBox.Show("Данные СНИЛС удалены.", "Успех",
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

        private void ViewSNILS_Click(object sender, RoutedEventArgs e)
        {
            if (!(SNILSGrid.SelectedItem is StudentSNILS selectedSNILS))
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
                $"Данные СНИЛС:\n\n" +
                $"Студент: {_selectedStudent.FullName}\n" +
                $"Номер СНИЛС: {selectedSNILS.FormattedNumber}\n" +
                $"Дата выдачи: {(selectedSNILS.IssueDate.HasValue ? selectedSNILS.IssueDate.Value.ToString("dd.MM.yyyy") : "не указана")}\n" +
                $"Кем выдан: {selectedSNILS.IssuedBy ?? "не указано"}",
                "Просмотр данных СНИЛС",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
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

        private void SNILSGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (SNILSGrid.SelectedItem != null && _selectedStudent != null)
            {
                EditSNILS_Click(sender, e);
            }
        }
    }
}