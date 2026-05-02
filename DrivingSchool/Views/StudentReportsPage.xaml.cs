using DrivingSchool.Services;
using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DrivingSchool.Views
{
    public partial class StudentReportsPage : Page
    {
        private readonly ReportService _reportService;

        public StudentReportsPage(SqlDataService dataService)
        {
            InitializeComponent();
            _reportService = new ReportService(dataService);

            LoadFilters();
            LoadData();
        }

        private void LoadFilters()
        {
            var groups = _reportService.GetAllGroups();
            GroupFilterBox.ItemsSource = groups;
            GroupFilterBox.DisplayMemberPath = "Name";
            GroupFilterBox.SelectedIndex = -1;
            GenderFilterBox.SelectedIndex = 0;
        }

        private void LoadData()
        {
            try
            {
                // Финансовая информация по студентам
                var students = _reportService.GetAllStudentsFinancialInfo();

                // Поиск
                if (!string.IsNullOrWhiteSpace(SearchBox.Text))
                {
                    var searchText = SearchBox.Text.ToLower();
                    students = students.Where(s => s.StudentName.ToLower().Contains(searchText)).ToList();
                }

                // Фильтр по группе
                if (GroupFilterBox.SelectedItem != null)
                {
                    var selectedGroup = GroupFilterBox.SelectedItem as Models.StudyGroup;
                    if (selectedGroup != null)
                    {
                        students = students.Where(s => s.GroupName == selectedGroup.Name).ToList();
                    }
                }

                StudentsGrid.ItemsSource = students;

                // Демографический отчет
                var demographic = _reportService.GetDemographicReport();
                TotalStudentsText.Text = demographic.TotalStudents.ToString();
                MaleCountText.Text = demographic.MaleCount.ToString();
                FemaleCountText.Text = demographic.FemaleCount.ToString();
                AverageAgeText.Text = demographic.AverageAge.ToString("N1");

                // Возрастное распределение
                var ageData = demographic.AgeDistribution.Select(a => new
                {
                    AgeGroup = a.Key,
                    Count = a.Value,
                    Percent = demographic.TotalStudents > 0 ? ((double)a.Value / demographic.TotalStudents * 100).ToString("N1") + "%" : "0%"
                }).ToList();
                AgeDistributionGrid.ItemsSource = ageData;

                // Статус документов
                var documents = _reportService.GetStudentDocumentStatus();
                if (!string.IsNullOrWhiteSpace(SearchBox.Text))
                {
                    var searchText = SearchBox.Text.ToLower();
                    documents = documents.Where(d => d.StudentName.ToLower().Contains(searchText)).ToList();
                }
                if (GroupFilterBox.SelectedItem != null)
                {
                    var selectedGroup = GroupFilterBox.SelectedItem as Models.StudyGroup;
                    if (selectedGroup != null)
                    {
                        documents = documents.Where(d => d.GroupName == selectedGroup.Name).ToList();
                    }
                }
                DocumentsGrid.ItemsSource = documents;

                // Истекающие документы
                var expiringDocs = _reportService.GetExpiringDocuments(30);
                if (!string.IsNullOrWhiteSpace(SearchBox.Text))
                {
                    var searchText = SearchBox.Text.ToLower();
                    expiringDocs = expiringDocs.Where(d => d.StudentName.ToLower().Contains(searchText)).ToList();
                }
                if (GroupFilterBox.SelectedItem != null)
                {
                    var selectedGroup = GroupFilterBox.SelectedItem as Models.StudyGroup;
                    if (selectedGroup != null)
                    {
                        expiringDocs = expiringDocs.Where(d => d.StudentName.ToLower().Contains(selectedGroup.Name.ToLower())).ToList();
                    }
                }
                ExpiringDocsGrid.ItemsSource = expiringDocs;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке отчета: {ex.Message}", "Ошибка");
            }
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadData();
        }

        private void FilterChanged(object sender, TextChangedEventArgs e)
        {
            LoadData();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            GroupFilterBox.SelectedIndex = -1;
            GenderFilterBox.SelectedIndex = 0;
            LoadData();
        }

        private void ExportStudentsBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    FileName = $"Students_{DateTime.Now:dd_MM_yyyy}.xlsx",
                    Filter = "Excel Files|*.xlsx",
                    DefaultExt = "xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var students = StudentsGrid.ItemsSource as System.Collections.Generic.List<StudentFinancialInfo>;
                    if (students != null && students.Any())
                    {
                        _reportService.ExportStudentsToExcel(students, saveDialog.FileName);
                        MessageBox.Show($"Отчет сохранен: {saveDialog.FileName}", "Успех");
                    }
                    else
                    {
                        MessageBox.Show("Нет данных для экспорта", "Предупреждение");
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка");
            }
        }

        private void ExportExpiringBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    FileName = $"ExpiringDocuments_{DateTime.Now:dd_MM_yyyy}.xlsx",
                    Filter = "Excel Files|*.xlsx",
                    DefaultExt = "xlsx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var expiringDocs = ExpiringDocsGrid.ItemsSource as System.Collections.Generic.List<ExpiringDocumentInfo>;
                    if (expiringDocs != null && expiringDocs.Any())
                    {
                        _reportService.ExportExpiringDocumentsToExcel(expiringDocs, saveDialog.FileName);
                        MessageBox.Show($"Отчет сохранен: {saveDialog.FileName}", "Успех");
                    }
                    else
                    {
                        MessageBox.Show("Нет данных для экспорта", "Предупреждение");
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта: {ex.Message}", "Ошибка");
            }

        }
    }
}

