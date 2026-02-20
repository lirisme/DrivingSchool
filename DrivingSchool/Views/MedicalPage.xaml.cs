using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;
using DrivingSchool.Services;

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
                _students = _dataService.LoadStudents();
                // TODO: LoadMedicalData нужно добавить в SqlDataService
                _medicalList = new StudentMedicalCertificateCollection { Certificates = new System.Collections.Generic.List<StudentMedicalCertificate>() };

                // Пока заглушка
                // _medicalList = _dataService.LoadMedicalData();

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _students = new StudentCollection { Students = new System.Collections.Generic.List<Student>() };
                _medicalList = new StudentMedicalCertificateCollection { Certificates = new System.Collections.Generic.List<StudentMedicalCertificate>() };
            }
        }

        private void ApplyFilter()
        {
            if (_selectedStudent != null)
            {
                var filtered = _medicalList.Certificates
                    .Where(m => m.StudentId == _selectedStudent.Id)
                    .Select(m =>
                    {
                        m.StudentName = _selectedStudent.FullName;
                        return m;
                    })
                    .ToList();

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
            var hasMedicalData = hasStudent && _medicalList.Certificates.Any(m => m.StudentId == _selectedStudent.Id);
            var hasSelection = MedicalGrid.SelectedItem != null;

            AddMedicalButton.IsEnabled = hasStudent && !hasMedicalData;
            EditMedicalButton.IsEnabled = hasMedicalData && hasSelection;
            DeleteMedicalButton.IsEnabled = hasMedicalData && hasSelection;
            ViewMedicalButton.IsEnabled = hasMedicalData && hasSelection;
        }

        private void MedicalGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsAvailability();
        }

        private void AddMedical_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new MedicalEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    // TODO: Обновить данные
                    // LoadData();
                    MessageBox.Show("Медицинская справка успешно добавлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
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

            try
            {
                var dialog = new MedicalEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName, selectedMedical);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    // TODO: Обновить данные
                    // LoadData();
                    MessageBox.Show("Медицинская справка успешно обновлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
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

            if (MessageBox.Show($"Удалить медицинскую справку студента {_selectedStudent.FullName}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    // TODO: Удалить через сервис
                    // _dataService.DeleteMedicalData(selectedMedical.Id);

                    MessageBox.Show("Медицинская справка удалена.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
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

        private void MedicalGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MedicalGrid.SelectedItem != null && _selectedStudent != null)
            {
                EditMedical_Click(sender, e);
            }
        }
    }
}