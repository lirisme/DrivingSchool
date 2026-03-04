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
    public partial class MedicalPage : Page
    {
        private readonly SqlDataService _dataService;
        private StudentCollection _students;
        private StudentMedicalCertificateCollection _medicalList;
        private Student _selectedStudent;

        public MedicalPage(SqlDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                Debug.WriteLine("=== ЗАГРУЗКА МЕДИЦИНСКИХ СПРАВОК ===");

                _students = _dataService.LoadStudents() ?? new StudentCollection { Students = new List<Student>() };
                Debug.WriteLine($"Загружено студентов: {_students.Students.Count}");

                // ИСПРАВЛЕНИЕ: загружаем реальные данные!
                _medicalList = _dataService.LoadMedicalData() ?? new StudentMedicalCertificateCollection { Certificates = new List<StudentMedicalCertificate>() };
                Debug.WriteLine($"Загружено справок: {_medicalList.Certificates.Count}");

                // Отладка: выводим все загруженные справки
                foreach (var med in _medicalList.Certificates)
                {
                    Debug.WriteLine($"  Справка ID={med.Id}, Студент ID={med.StudentId}, Серия={med.Series}, Номер={med.Number}");
                }

                ApplyFilter();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА загрузки данных: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _students = new StudentCollection { Students = new List<Student>() };
                _medicalList = new StudentMedicalCertificateCollection { Certificates = new List<StudentMedicalCertificate>() };
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

                    var filtered = _medicalList.Certificates
                        .Where(m => m.StudentId == _selectedStudent.Id)
                        .Select(m =>
                        {
                            m.StudentName = _selectedStudent.FullName;
                            return m;
                        })
                        .ToList();

                    Debug.WriteLine($"Найдено справок для студента: {filtered.Count}");

                    MedicalGrid.ItemsSource = filtered;

                    if (filtered.Any())
                    {
                        var validCount = filtered.Count(m => m.IsValid);
                        InfoTextBlock.Text = $"Медицинские справки студента {_selectedStudent.FullName} (действительных: {validCount})";
                    }
                    else
                    {
                        InfoTextBlock.Text = $"У студента {_selectedStudent.FullName} нет медицинских справок";
                    }
                }
                else
                {
                    Debug.WriteLine("Студент не выбран, показываем все справки");

                    var allMedical = _medicalList.Certificates
                        .Select(m =>
                        {
                            m.StudentName = GetStudentName(m.StudentId);
                            return m;
                        })
                        .ToList();

                    MedicalGrid.ItemsSource = allMedical;

                    var totalCount = _medicalList.Certificates.Count;
                    var expiredCount = _medicalList.Certificates.Count(m => !m.IsValid);

                    InfoTextBlock.Text = $"Всего справок: {totalCount} (просрочено: {expiredCount}). Выберите студента для добавления/редактирования";
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
                var hasSelection = MedicalGrid.SelectedItem != null;

                Debug.WriteLine($"UpdateButtonsAvailability: hasStudent={hasStudent}, hasSelection={hasSelection}");

                // ИСПРАВЛЕНИЕ: можно добавлять несколько справок (каждый год новая)
                AddMedicalButton.IsEnabled = hasStudent;
                EditMedicalButton.IsEnabled = hasSelection;
                DeleteMedicalButton.IsEnabled = hasSelection;
                ViewMedicalButton.IsEnabled = hasSelection;

                // Визуальная индикация
                AddMedicalButton.Opacity = AddMedicalButton.IsEnabled ? 1.0 : 0.5;
                EditMedicalButton.Opacity = EditMedicalButton.IsEnabled ? 1.0 : 0.5;
                DeleteMedicalButton.Opacity = DeleteMedicalButton.IsEnabled ? 1.0 : 0.5;
                ViewMedicalButton.Opacity = ViewMedicalButton.IsEnabled ? 1.0 : 0.5;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА в UpdateButtonsAvailability: {ex.Message}");
            }
        }

        private void MedicalGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsAvailability();
        }

        private void AddMedical_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStudent == null)
            {
                MessageBox.Show("Выберите студента", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Debug.WriteLine($"Добавление справки для студента ID={_selectedStudent.Id}");

                var dialog = new MedicalEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    Debug.WriteLine("Диалог закрыт с OK, перезагружаем данные");

                    // ИСПРАВЛЕНИЕ: перезагружаем данные после сохранения
                    _medicalList = _dataService.LoadMedicalData() ?? new StudentMedicalCertificateCollection { Certificates = new List<StudentMedicalCertificate>() };

                    Debug.WriteLine($"После перезагрузки справок: {_medicalList.Certificates.Count}");

                    ApplyFilter();

                    MessageBox.Show("Медицинская справка успешно добавлена!", "Успех",
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

        private void EditMedical_Click(object sender, RoutedEventArgs e)
        {
            if (!(MedicalGrid.SelectedItem is StudentMedicalCertificate selectedMedical))
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
                Debug.WriteLine($"Редактирование справки ID={selectedMedical.Id}");

                var dialog = new MedicalEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName, selectedMedical);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    Debug.WriteLine("Диалог закрыт с OK, перезагружаем данные");

                    // ИСПРАВЛЕНИЕ: перезагружаем данные после сохранения
                    _medicalList = _dataService.LoadMedicalData() ?? new StudentMedicalCertificateCollection { Certificates = new List<StudentMedicalCertificate>() };

                    ApplyFilter();

                    MessageBox.Show("Медицинская справка успешно обновлена!", "Успех",
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

        private void DeleteMedical_Click(object sender, RoutedEventArgs e)
        {
            if (!(MedicalGrid.SelectedItem is StudentMedicalCertificate selectedMedical))
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

            if (MessageBox.Show($"Удалить медицинскую справку студента {_selectedStudent.FullName}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    Debug.WriteLine($"Удаление справки ID={selectedMedical.Id}");

                    _dataService.DeleteMedicalData(selectedMedical.Id);

                    // ИСПРАВЛЕНИЕ: перезагружаем данные после удаления
                    _medicalList = _dataService.LoadMedicalData() ?? new StudentMedicalCertificateCollection { Certificates = new List<StudentMedicalCertificate>() };

                    ApplyFilter();

                    MessageBox.Show("Медицинская справка удалена.", "Успех",
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

        private void ViewMedical_Click(object sender, RoutedEventArgs e)
        {
            if (!(MedicalGrid.SelectedItem is StudentMedicalCertificate selectedMedical))
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

            var status = selectedMedical.IsValid ? "✅ Действительна" : "❌ Просрочена";
            var daysLeft = (selectedMedical.ValidUntil - DateTime.Today).Days;

            MessageBox.Show(
                $"Медицинская справка:\n\n" +
                $"Студент: {_selectedStudent.FullName}\n" +
                $"Серия: {selectedMedical.Series}\n" +
                $"Номер: {selectedMedical.Number}\n" +
                $"Дата выдачи: {selectedMedical.IssueDate:dd.MM.yyyy}\n" +
                $"Действительна до: {selectedMedical.ValidUntil:dd.MM.yyyy}\n" +
                $"Статус: {status}\n" +
                $"Осталось дней: {daysLeft}\n" +
                $"Мед. учреждение: {selectedMedical.MedicalInstitution}\n" +
                $"Категории: {selectedMedical.Categories}\n" +
                $"Регион: {selectedMedical.Region}",
                "Просмотр медицинской справки",
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

        private void MedicalGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MedicalGrid.SelectedItem != null && _selectedStudent != null)
            {
                EditMedical_Click(sender, e);
            }
        }
    }
}