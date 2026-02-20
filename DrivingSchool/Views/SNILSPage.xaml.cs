using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;
using DrivingSchool.Services;

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
                _students = _dataService.LoadStudents();
                // TODO: LoadSNILSData нужно добавить в SqlDataService
                _snilsList = new StudentSNILSCollection { SNILSList = new System.Collections.Generic.List<StudentSNILS>() };

                // Пока заглушка
                // _snilsList = _dataService.LoadSNILSData();

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _students = new StudentCollection { Students = new System.Collections.Generic.List<Student>() };
                _snilsList = new StudentSNILSCollection { SNILSList = new System.Collections.Generic.List<StudentSNILS>() };
            }
        }

        private void ApplyFilter()
        {
            if (_selectedStudent != null)
            {
                var filtered = _snilsList.SNILSList
                    .Where(s => s.StudentId == _selectedStudent.Id)
                    .Select(s =>
                    {
                        s.StudentName = _selectedStudent.FullName;
                        return s;
                    })
                    .ToList();

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
            var hasSNILSData = hasStudent && _snilsList.SNILSList.Any(s => s.StudentId == _selectedStudent.Id);
            var hasSelection = SNILSGrid.SelectedItem != null;

            AddSNILSButton.IsEnabled = hasStudent && !hasSNILSData;
            EditSNILSButton.IsEnabled = hasSNILSData && hasSelection;
            DeleteSNILSButton.IsEnabled = hasSNILSData && hasSelection;
            ViewSNILSButton.IsEnabled = hasSNILSData && hasSelection;
        }

        private void SNILSGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsAvailability();
        }

        private void AddSNILS_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SNILSEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    // TODO: Обновить данные
                    // LoadData();
                    MessageBox.Show("Данные СНИЛС успешно добавлены!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
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

            try
            {
                var dialog = new SNILSEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName, selectedSNILS);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    // TODO: Обновить данные
                    // LoadData();
                    MessageBox.Show("Данные СНИЛС успешно обновлены!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
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

            if (MessageBox.Show($"Удалить данные СНИЛС студента {_selectedStudent.FullName}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    // TODO: Удалить через сервис
                    // _dataService.DeleteSNILSData(selectedSNILS.Id);

                    MessageBox.Show("Данные СНИЛС удалены.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
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

            MessageBox.Show(
                $"Данные СНИЛС:\n\n" +
                $"Студент: {_selectedStudent.FullName}\n" +
                $"Номер СНИЛС: {selectedSNILS.FormattedNumber}\n" +
                $"Дата выдачи: {selectedSNILS.IssueDate:dd.MM.yyyy}\n" +
                $"Кем выдан: {selectedSNILS.IssuedBy}",
                "Просмотр данных СНИЛС",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            SearchResultsListBox.Visibility = Visibility.Collapsed;
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