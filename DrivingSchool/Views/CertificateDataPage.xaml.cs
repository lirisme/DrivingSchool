using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class CertificateDataPage : Page
    {
        private readonly SqlDataService _dataService;
        private StudentCollection _students;
        private StudentCertificateCollection _certificates;
        private VehicleCategoryCollection _categories;
        private Student _selectedStudent;

        public CertificateDataPage(SqlDataService dataService)
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
                _categories = _dataService.LoadVehicleCategories();
                // TODO: LoadCertificates нужно добавить в SqlDataService
                _certificates = new StudentCertificateCollection { Certificates = new System.Collections.Generic.List<StudentCertificate>() };

                // Пока заглушка
                // _certificates = _dataService.LoadCertificates();

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _students = new StudentCollection { Students = new System.Collections.Generic.List<Student>() };
                _categories = new VehicleCategoryCollection { Categories = new System.Collections.Generic.List<VehicleCategory>() };
                _certificates = new StudentCertificateCollection { Certificates = new System.Collections.Generic.List<StudentCertificate>() };
            }
        }

        private void ApplyFilter()
        {
            if (_selectedStudent != null)
            {
                var filtered = _certificates.Certificates
                    .Where(c => c.StudentId == _selectedStudent.Id)
                    .Select(c =>
                    {
                        c.StudentName = _selectedStudent.FullName;
                        var category = _categories.Categories.FirstOrDefault(cat => cat.Id == c.VehicleCategoryId);
                        c.CategoryName = category?.FullName ?? "";
                        return c;
                    })
                    .ToList();

                CertificateGrid.ItemsSource = filtered;

                if (filtered.Any())
                {
                    InfoTextBlock.Text = $"Свидетельства студента {_selectedStudent.FullName}";
                }
                else
                {
                    InfoTextBlock.Text = $"У студента {_selectedStudent.FullName} нет свидетельств";
                }
            }
            else
            {
                var allCertificates = _certificates.Certificates
                    .Select(c =>
                    {
                        c.StudentName = GetStudentName(c.StudentId);
                        var category = _categories.Categories.FirstOrDefault(cat => cat.Id == c.VehicleCategoryId);
                        c.CategoryName = category?.FullName ?? "";
                        return c;
                    })
                    .ToList();

                CertificateGrid.ItemsSource = allCertificates;
                InfoTextBlock.Text = $"Всего записей: {_certificates.Certificates.Count}. Выберите студента для добавления/редактирования";
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
            var hasCertificate = hasStudent && _certificates.Certificates.Any(c => c.StudentId == _selectedStudent.Id);
            var hasSelection = CertificateGrid.SelectedItem != null;

            AddCertificateButton.IsEnabled = hasStudent && !hasCertificate;
            EditCertificateButton.IsEnabled = hasCertificate && hasSelection;
            DeleteCertificateButton.IsEnabled = hasCertificate && hasSelection;
            ViewCertificateButton.IsEnabled = hasCertificate && hasSelection;
            PrintCertificateButton.IsEnabled = hasCertificate && hasSelection;
        }

        private void CertificateGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsAvailability();
        }

        private void AddCertificate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new CertificateEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    // TODO: Обновить данные
                    // LoadData();
                    MessageBox.Show("Свидетельство успешно добавлено!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditCertificate_Click(object sender, RoutedEventArgs e)
        {
            if (!(CertificateGrid.SelectedItem is StudentCertificate selectedCertificate))
            {
                MessageBox.Show("Выберите запись для редактирования", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dialog = new CertificateEditDialog(_dataService, _selectedStudent.Id, _selectedStudent.FullName, selectedCertificate);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    // TODO: Обновить данные
                    // LoadData();
                    MessageBox.Show("Свидетельство успешно обновлено!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteCertificate_Click(object sender, RoutedEventArgs e)
        {
            if (!(CertificateGrid.SelectedItem is StudentCertificate selectedCertificate))
            {
                MessageBox.Show("Выберите запись для удаления", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Удалить свидетельство студента {_selectedStudent.FullName}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    // TODO: Удалить через сервис
                    // _dataService.DeleteCertificateData(selectedCertificate.Id);

                    MessageBox.Show("Свидетельство удалено.", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewCertificate_Click(object sender, RoutedEventArgs e)
        {
            if (!(CertificateGrid.SelectedItem is StudentCertificate selectedCertificate))
            {
                MessageBox.Show("Выберите запись для просмотра", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show(
                $"Свидетельство об окончании:\n\n" +
                $"Студент: {_selectedStudent.FullName}\n" +
                $"Серия: {selectedCertificate.CertificateSeries}\n" +
                $"Номер: {selectedCertificate.CertificateNumber}\n" +
                $"Дата выдачи: {selectedCertificate.IssueDate:dd.MM.yyyy}\n" +
                $"Категория: {selectedCertificate.CategoryCode} - {selectedCertificate.CategoryName}",
                "Просмотр свидетельства",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void PrintCertificate_Click(object sender, RoutedEventArgs e)
        {
            if (!(CertificateGrid.SelectedItem is StudentCertificate selectedCertificate))
            {
                MessageBox.Show("Выберите запись для печати", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // TODO: Реализовать печать
            MessageBox.Show($"Печать свидетельства:\n\n{selectedCertificate.CertificateSeries} {selectedCertificate.CertificateNumber}", "Печать",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            SearchResultsListBox.Visibility = Visibility.Collapsed;
        }

        private void CertificateGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CertificateGrid.SelectedItem != null && _selectedStudent != null)
            {
                EditCertificate_Click(sender, e);
            }
        }
    }
}