using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class StudentsPage : Page
    {
        private readonly SqlDataService _dataService;
        private StudentCollection _students;
        private StudentCollection _filteredStudents;

        public StudentsPage(SqlDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            LoadStudents();
        }

        private void LoadStudents()
        {
            try
            {
                _students = _dataService.LoadStudents();
                ApplyFilter();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _students = new StudentCollection { Students = new System.Collections.Generic.List<Student>() };
                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            if (_students?.Students == null)
            {
                _filteredStudents = new StudentCollection { Students = new System.Collections.Generic.List<Student>() };
            }
            else
            {
                var searchText = SearchTextBox?.Text?.ToLower() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(searchText))
                {
                    _filteredStudents = new StudentCollection { Students = _students.Students.ToList() };
                }
                else
                {
                    _filteredStudents = new StudentCollection
                    {
                        Students = _students.Students
                            .Where(s => (s.LastName ?? "").ToLower().Contains(searchText) ||
                                       (s.FirstName ?? "").ToLower().Contains(searchText) ||
                                       (s.MiddleName ?? "").ToLower().Contains(searchText) ||
                                       (s.Phone ?? "").Contains(searchText) ||
                                       (s.Email ?? "").ToLower().Contains(searchText))
                            .ToList()
                    };
                }
            }

            StudentsGrid.ItemsSource = _filteredStudents.Students;
            UpdateButtonsAvailability();
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            var totalCount = _students?.Students?.Count ?? 0;
            var filteredCount = _filteredStudents?.Students?.Count ?? 0;

            if (filteredCount == totalCount)
            {
                StatusTextBlock.Text = $"Всего учащихся: {totalCount}";
            }
            else
            {
                StatusTextBlock.Text = $"Найдено учащихся: {filteredCount} из {totalCount}";
            }
        }

        private void UpdateButtonsAvailability()
        {
            var isSelected = StudentsGrid.SelectedItem != null;
            EditStudentButton.IsEnabled = isSelected;
            DeleteStudentButton.IsEnabled = isSelected;
            ViewStudentButton.IsEnabled = isSelected;
            DocumentsButton.IsEnabled = isSelected;
            PaymentsButton.IsEnabled = isSelected;

            if (isSelected && StudentsGrid.SelectedItem is Student selectedStudent)
            {
                InfoTextBlock.Text = $"Выбран: {selectedStudent.FullName} | Возраст: {selectedStudent.Age} | Категория: {selectedStudent.CategoryCode}";
            }
            else
            {
                InfoTextBlock.Text = "Выберите учащегося для просмотра документов";
            }
        }

        private void StudentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsAvailability();
        }

        private void AddStudent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Попытка открыть диалог добавления студента");

                var dialog = new StudentEditDialog(_dataService);
                dialog.Owner = Window.GetWindow(this);

                System.Diagnostics.Debug.WriteLine("Диалог создан, показываем...");

                if (dialog.ShowDialog() == true)
                {
                    System.Diagnostics.Debug.WriteLine("Диалог закрыт с результатом true, перезагружаем список");
                    LoadStudents();
                    MessageBox.Show("Учащийся успешно добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Диалог закрыт с результатом false");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ОШИБКА в AddStudent_Click: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                MessageBox.Show($"Ошибка при добавлении: {ex.Message}\n\n{ex.StackTrace}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditStudent_Click(object sender, RoutedEventArgs e)
        {
            if (!(StudentsGrid.SelectedItem is Student selectedStudent))
            {
                MessageBox.Show("Выберите учащегося для редактирования", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dialog = new StudentEditDialog(_dataService, selectedStudent);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    LoadStudents();
                    MessageBox.Show("Данные учащегося обновлены!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteStudent_Click(object sender, RoutedEventArgs e)
        {
            if (!(StudentsGrid.SelectedItem is Student selectedStudent))
            {
                MessageBox.Show("Выберите учащегося для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Удалить учащегося {selectedStudent.FullName}?\n\n" +
                               "Будут удалены все связанные данные (паспорт, СНИЛС, мед. справка, адрес, платежи).\n\n" +
                               "Это действие нельзя отменить!",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    _dataService.DeleteStudent(selectedStudent.Id);
                    LoadStudents();
                    MessageBox.Show("Учащийся удален.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewStudent_Click(object sender, RoutedEventArgs e)
        {
            if (!(StudentsGrid.SelectedItem is Student selectedStudent))
            {
                MessageBox.Show("Выберите учащегося для просмотра", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(
                $"Информация об учащемся:\n\n" +
                $"ФИО: {selectedStudent.FullName}\n" +
                $"Дата рождения: {selectedStudent.BirthDate:dd.MM.yyyy} (Возраст: {selectedStudent.Age})\n" +
                $"Место рождения: {selectedStudent.BirthPlace}\n" +
                $"Телефон: {selectedStudent.Phone}\n" +
                $"Email: {selectedStudent.Email ?? "не указан"}\n" +
                $"Гражданство: {selectedStudent.Citizenship}\n" +
                $"Пол: {selectedStudent.Gender}\n" +
                $"Группа: {selectedStudent.GroupName}\n" +
                $"Категория: {selectedStudent.CategoryCode}\n" +
                $"Инструктор: {selectedStudent.InstructorName}\n" +
                $"Автомобиль: {selectedStudent.CarInfo}",
                $"Данные учащегося: {selectedStudent.FullName}",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void DocumentsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(StudentsGrid.SelectedItem is Student selectedStudent))
            {
                MessageBox.Show("Выберите учащегося для работы с документами", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // TODO: Открыть окно с документами учащегося
            MessageBox.Show($"Документы учащегося {selectedStudent.FullName}\n\n" +
                           "• Паспортные данные\n" +
                           "• СНИЛС\n" +
                           "• Медицинская справка\n" +
                           "• Адрес регистрации\n" +
                           "• Свидетельство об окончании\n" +
                           "• Водительское удостоверение",
                "Документы учащегося", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PaymentsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(StudentsGrid.SelectedItem is Student selectedStudent))
            {
                MessageBox.Show("Выберите учащегося для просмотра оплат", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // TODO: Открыть страницу платежей для этого учащегося
            MessageBox.Show($"Платежи учащегося {selectedStudent.FullName}", "Платежи",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            ApplyFilter();
        }

        private void StudentsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (StudentsGrid.SelectedItem != null)
            {
                EditStudent_Click(sender, e);
            }
        }
    }
}