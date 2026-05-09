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
                Debug.WriteLine("=== ЗАГРУЗКА ДАННЫХ СВИДЕТЕЛЬСТВ ===");

                _students = _dataService.LoadStudents() ?? new StudentCollection { Students = new List<Student>() };
                Debug.WriteLine($"Загружено студентов: {_students.Students.Count}");

                _categories = _dataService.LoadVehicleCategories() ?? new VehicleCategoryCollection { Categories = new List<VehicleCategory>() };
                Debug.WriteLine($"Загружено категорий: {_categories.Categories.Count}");

                _certificates = _dataService.LoadCertificates() ?? new StudentCertificateCollection { Certificates = new List<StudentCertificate>() };
                Debug.WriteLine($"Загружено свидетельств: {_certificates.Certificates.Count}");

                ApplyFilter();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА загрузки данных: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                _students = new StudentCollection { Students = new List<Student>() };
                _categories = new VehicleCategoryCollection { Categories = new List<VehicleCategory>() };
                _certificates = new StudentCertificateCollection { Certificates = new List<StudentCertificate>() };
            }
        }

        private void ApplyFilter()
        {
            try
            {
                if (_selectedStudent != null)
                {
                    var filtered = _certificates.Certificates
                        .Where(c => c.StudentId == _selectedStudent.Id)
                        .Select(c =>
                        {
                            c.StudentName = _selectedStudent.FullName;
                            var category = _categories.Categories.FirstOrDefault(cat => cat.Id == c.VehicleCategoryId);
                            if (category != null)
                            {
                                c.CategoryName = category.FullName ?? "";
                                c.CategoryCode = category.Code ?? "";
                            }
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
                            if (category != null)
                            {
                                c.CategoryName = category.FullName ?? "";
                                c.CategoryCode = category.Code ?? "";
                            }
                            return c;
                        })
                        .ToList();

                    CertificateGrid.ItemsSource = allCertificates;
                    InfoTextBlock.Text = $"Всего записей: {_certificates.Certificates.Count}. Выберите студента для добавления/редактирования";
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
                SelectedStudentDetails.Text = $"Телефон: {_selectedStudent.Phone}";
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
                var hasSelection = CertificateGrid.SelectedItem != null;

                AddCertificateButton.IsEnabled = hasStudent;
                EditCertificateButton.IsEnabled = hasSelection;
                DeleteCertificateButton.IsEnabled = hasSelection;
                ViewCertificateButton.IsEnabled = hasSelection;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ОШИБКА в UpdateButtonsAvailability: {ex.Message}");
            }
        }

        private void CertificateGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Если выбрано свидетельство - находим студента
            if (CertificateGrid.SelectedItem is StudentCertificate selectedCertificate)
            {
                var student = _students?.Students?.FirstOrDefault(s => s.Id == selectedCertificate.StudentId);
                if (student != null)
                {
                    _selectedStudent = student;
                    UpdateSelectedStudentPanel();
                    ApplyFilter();
                }
            }
            UpdateButtonsAvailability();
        }

        // Клик по пустому месту для сброса выбора
        private void CertificateGrid_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var hit = System.Windows.Media.VisualTreeHelper.HitTest(CertificateGrid, e.GetPosition(CertificateGrid));
            var row = FindVisualParent<DataGridRow>(hit?.VisualHit as System.Windows.DependencyObject);

            if (row == null)
            {
                _selectedStudent = null;
                SelectedStudentPanel.Visibility = Visibility.Collapsed;
                CertificateGrid.SelectedItem = null;
                ApplyFilter();
                e.Handled = true;
            }
        }

        private T FindVisualParent<T>(System.Windows.DependencyObject child) where T : System.Windows.DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;
                child = System.Windows.Media.VisualTreeHelper.GetParent(child);
            }
            return null;
        }

        private void AddCertificate_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedStudent == null)
            {
                MessageBox.Show("Выберите студента", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Создаем ExamService
                var examService = new ExamService(_dataService.GetConnectionString());

                var dialog = new CertificateEditDialog(_dataService, examService, _selectedStudent.Id, _selectedStudent.FullName);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    _certificates = _dataService.LoadCertificates() ?? new StudentCertificateCollection { Certificates = new List<StudentCertificate>() };
                    ApplyFilter();
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

            if (_selectedStudent == null)
            {
                MessageBox.Show("Выберите студента", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var examService = new ExamService(_dataService.GetConnectionString());

                var dialog = new CertificateEditDialog(_dataService, examService, _selectedStudent.Id, _selectedStudent.FullName, selectedCertificate);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    _certificates = _dataService.LoadCertificates() ?? new StudentCertificateCollection { Certificates = new List<StudentCertificate>() };
                    ApplyFilter();
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

            if (_selectedStudent == null)
            {
                MessageBox.Show("Выберите студента", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Удалить свидетельство студента {_selectedStudent.FullName}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    _dataService.DeleteCertificateData(selectedCertificate.Id);
                    _certificates = _dataService.LoadCertificates() ?? new StudentCertificateCollection { Certificates = new List<StudentCertificate>() };
                    ApplyFilter();
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

            if (_selectedStudent == null)
            {
                MessageBox.Show("Выберите студента", "Предупреждение",
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

        private void CertificateGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CertificateGrid.SelectedItem is StudentCertificate selectedCertificate)
            {
                if (_selectedStudent == null || _selectedStudent.Id != selectedCertificate.StudentId)
                {
                    _selectedStudent = _students?.Students?.FirstOrDefault(s => s.Id == selectedCertificate.StudentId);
                    if (_selectedStudent != null)
                    {
                        UpdateSelectedStudentPanel();
                        ApplyFilter();
                    }
                }

                if (_selectedStudent != null)
                {
                    EditCertificate_Click(sender, e);
                }
            }
        }
    }
}