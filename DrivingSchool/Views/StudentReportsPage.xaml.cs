using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class StudentReportsPage : Page
    {
        private readonly SqlDataService _dataService;
        private StudentCollection _students;
        private PaymentCollection _payments;
        private StudentTuitionCollection _tuitions;
        private StudyGroupCollection _groups;
        private StudentPassportDataCollection _passports;
        private StudentSNILSCollection _snils;
        private StudentMedicalCertificateCollection _medical;
        private StudentRegistrationAddressCollection _addresses;
        private VehicleCategoryCollection _categories;

        private Student _selectedStudentForReport;

        public StudentReportsPage(SqlDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;
            LoadData();
            InitializeFilters();
            GenerateMainReport();
        }

        private void LoadData()
        {
            try
            {
                _students = _dataService.LoadStudents();
                _payments = _dataService.LoadPayments();
                _groups = _dataService.LoadStudyGroups();
                _categories = _dataService.LoadVehicleCategories();

                // Заглушки для остальных данных
                _tuitions = new StudentTuitionCollection { Tuitions = new List<StudentTuition>() };
                _passports = new StudentPassportDataCollection { Passports = new List<StudentPassportData>() };
                _snils = new StudentSNILSCollection { SNILSList = new List<StudentSNILS>() };
                _medical = new StudentMedicalCertificateCollection { Certificates = new List<StudentMedicalCertificate>() };
                _addresses = new StudentRegistrationAddressCollection { Addresses = new List<StudentRegistrationAddress>() };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                InitializeEmptyCollections();
            }
        }

        private void InitializeEmptyCollections()
        {
            _students = new StudentCollection { Students = new List<Student>() };
            _payments = new PaymentCollection { Payments = new List<Payment>() };
            _tuitions = new StudentTuitionCollection { Tuitions = new List<StudentTuition>() };
            _groups = new StudyGroupCollection { Groups = new List<StudyGroup>() };
            _passports = new StudentPassportDataCollection { Passports = new List<StudentPassportData>() };
            _snils = new StudentSNILSCollection { SNILSList = new List<StudentSNILS>() };
            _medical = new StudentMedicalCertificateCollection { Certificates = new List<StudentMedicalCertificate>() };
            _addresses = new StudentRegistrationAddressCollection { Addresses = new List<StudentRegistrationAddress>() };
            _categories = new VehicleCategoryCollection { Categories = new List<VehicleCategory>() };
        }

        private void InitializeFilters()
        {
            // Группы
            GroupFilterComboBox.Items.Clear();
            GroupFilterComboBox.Items.Add("Все группы");
            foreach (var group in _groups.Groups.OrderBy(g => g.Name))
            {
                GroupFilterComboBox.Items.Add(group.Name);
            }
            GroupFilterComboBox.SelectedIndex = 0;

            // Пол
            GenderFilterComboBox.Items.Clear();
            GenderFilterComboBox.Items.Add("Все");
            GenderFilterComboBox.Items.Add("Мужской");
            GenderFilterComboBox.Items.Add("Женский");
            GenderFilterComboBox.SelectedIndex = 0;

            // Возраст
            AgeFilterComboBox.Items.Clear();
            AgeFilterComboBox.Items.Add("Все возраста");
            AgeFilterComboBox.Items.Add("16-17 лет");
            AgeFilterComboBox.Items.Add("18-25 лет");
            AgeFilterComboBox.Items.Add("26-35 лет");
            AgeFilterComboBox.Items.Add("36-45 лет");
            AgeFilterComboBox.Items.Add("Старше 45 лет");
            AgeFilterComboBox.SelectedIndex = 0;

            // Статус оплаты
            PaymentStatusComboBox.Items.Clear();
            PaymentStatusComboBox.Items.Add("Все статусы");
            PaymentStatusComboBox.Items.Add("С задолженностью");
            PaymentStatusComboBox.Items.Add("Полностью оплачено");
            PaymentStatusComboBox.Items.Add("Не оплачено");
            PaymentStatusComboBox.Items.Add("Частично оплачено");
            PaymentStatusComboBox.SelectedIndex = 0;
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
                _selectedStudentForReport = selectedStudent;
                UpdateSelectedStudentPanel();
                SearchResultsListBox.Visibility = Visibility.Collapsed;
                SearchTextBox.Text = string.Empty;
                GenerateMainReport();
            }
        }

        private void UpdateSelectedStudentPanel()
        {
            if (_selectedStudentForReport != null)
            {
                SelectedStudentPanel.Visibility = Visibility.Visible;
                SelectedStudentText.Text = _selectedStudentForReport.FullName;
                SelectedStudentDetails.Text = $"Телефон: {_selectedStudentForReport.Phone} | ID: {_selectedStudentForReport.Id}";
            }
            else
            {
                SelectedStudentPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void ClearSelectedStudent_Click(object sender, RoutedEventArgs e)
        {
            _selectedStudentForReport = null;
            UpdateSelectedStudentPanel();
            GenerateMainReport();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            SearchResultsListBox.Visibility = Visibility.Collapsed;
        }

        private void GenerateMainReport()
        {
            try
            {
                var allReports = _students.Students
                    .Select(student => CreateStudentReport(student))
                    .Where(report => report != null)
                    .ToList();

                var filteredReports = ApplyFilters(allReports);

                UpdateStatistics(filteredReports);
                GenerateDemographicsReport(filteredReports);
                GenerateGroupsReport(filteredReports);
                GenerateDocumentsReport(filteredReports);
                GeneratePaymentsReport(filteredReports);
                GenerateDurationReport(filteredReports);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при формировании отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private StudentReport CreateStudentReport(Student student)
        {
            if (student == null) return null;

            try
            {
                var tuition = _tuitions?.Tuitions?.FirstOrDefault(t => t.StudentId == student.Id);

                var studentPayments = _payments?.Payments?
                    .Where(p => p.StudentId == student.Id)
                    .ToList() ?? new List<Payment>();

                var totalPaid = studentPayments.Sum(p => p.Amount);
                var totalToPay = tuition?.FinalAmount ?? 0;
                var debt = totalToPay - totalPaid;

                var category = _categories?.Categories?.FirstOrDefault(c => c.Id == student.VehicleCategoryId);
                var group = _groups?.Groups?.FirstOrDefault(g => g.Id == student.GroupId);

                return new StudentReport
                {
                    Id = student.Id,
                    FullName = student.FullName,
                    Phone = student.Phone ?? "Не указан",
                    GroupName = group?.Name ?? "Не назначена",
                    Age = student.Age,
                    Gender = student.Gender ?? "Не указан",
                    Citizenship = student.Citizenship ?? "Не указано",
                    CategoryCode = category?.Code ?? "Не указана",
                    TotalToPay = totalToPay,
                    TotalPaid = totalPaid,
                    Debt = debt,
                    LastPaymentDate = studentPayments.Any() ? studentPayments.Max(p => p.PaymentDate) : (DateTime?)null,
                    PaymentCount = studentPayments.Count,
                    PaymentStatus = GetPaymentStatus(debt, totalPaid, totalToPay),
                    HasPassport = _passports?.Passports?.Any(p => p.StudentId == student.Id) ?? false,
                    HasSNILS = _snils?.SNILSList?.Any(s => s.StudentId == student.Id) ?? false,
                    HasMedical = _medical?.Certificates?.Any(m => m.StudentId == student.Id) ?? false,
                    HasAddress = _addresses?.Addresses?.Any(a => a.StudentId == student.Id) ?? false,
                    StudyDuration = CalculateStudyDuration(group),
                    GroupStartDate = group?.StartDate ?? DateTime.MinValue,
                    GroupEndDate = group?.EndDate ?? DateTime.MinValue
                };
            }
            catch
            {
                return null;
            }
        }

        private List<StudentReport> ApplyFilters(List<StudentReport> reports)
        {
            var filtered = reports;

            if (_selectedStudentForReport != null)
            {
                filtered = filtered.Where(r => r.Id == _selectedStudentForReport.Id).ToList();
                return filtered;
            }

            // Фильтр по группе
            if (GroupFilterComboBox.SelectedIndex > 0)
            {
                var selectedGroup = GroupFilterComboBox.SelectedItem?.ToString();
                if (!string.IsNullOrEmpty(selectedGroup))
                {
                    filtered = filtered.Where(r => r.GroupName == selectedGroup).ToList();
                }
            }

            // Фильтр по полу
            if (GenderFilterComboBox.SelectedIndex > 0)
            {
                var selectedGender = GenderFilterComboBox.SelectedItem?.ToString();
                filtered = filtered.Where(r => r.Gender == selectedGender).ToList();
            }

            // Фильтр по возрасту
            if (AgeFilterComboBox.SelectedIndex > 0)
            {
                switch (AgeFilterComboBox.SelectedIndex)
                {
                    case 1: // 16-17 лет
                        filtered = filtered.Where(r => r.Age >= 16 && r.Age <= 17).ToList();
                        break;
                    case 2: // 18-25 лет
                        filtered = filtered.Where(r => r.Age >= 18 && r.Age <= 25).ToList();
                        break;
                    case 3: // 26-35 лет
                        filtered = filtered.Where(r => r.Age >= 26 && r.Age <= 35).ToList();
                        break;
                    case 4: // 36-45 лет
                        filtered = filtered.Where(r => r.Age >= 36 && r.Age <= 45).ToList();
                        break;
                    case 5: // Старше 45
                        filtered = filtered.Where(r => r.Age > 45).ToList();
                        break;
                }
            }

            return filtered;
        }

        private void UpdateStatistics(List<StudentReport> reports)
        {
            TotalStudentsText.Text = reports.Count.ToString();

            var maleCount = reports.Count(r => r.Gender == "Мужской");
            var femaleCount = reports.Count(r => r.Gender == "Женский");

            MaleCountText.Text = maleCount.ToString();
            FemaleCountText.Text = femaleCount.ToString();

            var averageAge = reports.Any() ? reports.Average(r => r.Age) : 0;
            AverageAgeText.Text = averageAge.ToString("N1");

            var withAllDocuments = reports.Count(r => r.HasPassport && r.HasSNILS && r.HasMedical && r.HasAddress);
            WithDocumentsText.Text = withAllDocuments.ToString();
        }

        private void GenerateDemographicsReport(List<StudentReport> reports)
        {
            var ageGroups = new[]
            {
                new { Range = "16-17 лет", Min = 16, Max = 17 },
                new { Range = "18-25 лет", Min = 18, Max = 25 },
                new { Range = "26-35 лет", Min = 26, Max = 35 },
                new { Range = "36-45 лет", Min = 36, Max = 45 },
                new { Range = "Старше 45", Min = 46, Max = 100 }
            };

            var demographics = ageGroups
                .Select(ageGroup => new
                {
                    ВозрастнаяГруппа = ageGroup.Range,
                    Количество = reports.Count(r => r.Age >= ageGroup.Min && r.Age <= ageGroup.Max),
                    Мужчины = reports.Count(r => r.Age >= ageGroup.Min && r.Age <= ageGroup.Max && r.Gender == "Мужской"),
                    Женщины = reports.Count(r => r.Age >= ageGroup.Min && r.Age <= ageGroup.Max && r.Gender == "Женский"),
                    Процент = reports.Count > 0 ? $"{(double)reports.Count(r => r.Age >= ageGroup.Min && r.Age <= ageGroup.Max) / reports.Count * 100:N1}%" : "0%"
                })
                .Where(d => d.Количество > 0)
                .ToList();

            DemographicsDataGrid.ItemsSource = demographics;
        }

        private void GenerateGroupsReport(List<StudentReport> reports)
        {
            var groupsReport = _groups.Groups
                .Select(group =>
                {
                    var groupStudents = reports.Where(r => r.GroupName == group.Name).ToList();
                    return new
                    {
                        Группа = group.Name ?? "Без названия",
                        КоличествоСтудентов = groupStudents.Count,
                        СтатусГруппы = GetGroupStatusName(group.Status),
                        Мужчины = groupStudents.Count(s => s.Gender == "Мужской"),
                        Женщины = groupStudents.Count(s => s.Gender == "Женский"),
                        СреднийВозраст = groupStudents.Any() ? groupStudents.Average(s => s.Age).ToString("N1") : "0",
                        НачалоОбучения = group.StartDate.ToString("dd.MM.yyyy"),
                        КонецОбучения = group.EndDate.ToString("dd.MM.yyyy")
                    };
                })
                .Where(g => g.КоличествоСтудентов > 0)
                .OrderBy(g => g.Группа)
                .ToList();

            GroupsDataGrid.ItemsSource = groupsReport;
        }

        private void GenerateDocumentsReport(List<StudentReport> reports)
        {
            var documentsReport = reports
                .Select(r => new
                {
                    ФИО = r.FullName,
                    Группа = r.GroupName,
                    Паспорт = r.HasPassport ? "✅" : "❌",
                    СНИЛС = r.HasSNILS ? "✅" : "❌",
                    МедСправка = r.HasMedical ? "✅" : "❌",
                    Адрес = r.HasAddress ? "✅" : "❌",
                    ВсегоДокументов = GetDocumentsCount(r), // Считаем для сортировки
                    Статус = GetDocumentsStatus(r)
                })
                .OrderByDescending(x => x.ВсегоДокументов)
                .ThenBy(x => x.ФИО)
                .Select(x => new // Создаем новый анонимный тип без поля для сортировки
                {
                    x.ФИО,
                    x.Группа,
                    x.Паспорт,
                    x.СНИЛС,
                    x.МедСправка,
                    x.Адрес,
                    x.Статус
                })
                .ToList();

            DocumentsDataGrid.ItemsSource = documentsReport;
        }

        private void GeneratePaymentsReport(List<StudentReport> reports)
        {
            var filteredReports = reports.Where(r => r.TotalToPay > 0).ToList();

            if (PaymentStatusComboBox.SelectedIndex > 0)
            {
                var selectedStatus = PaymentStatusComboBox.SelectedItem?.ToString();

                if (selectedStatus == "С задолженностью")
                {
                    filteredReports = filteredReports.Where(r => r.Debt > 0).ToList();
                }
                else if (!string.IsNullOrEmpty(selectedStatus))
                {
                    filteredReports = filteredReports.Where(r => r.PaymentStatus == selectedStatus).ToList();
                }
            }

            var paymentsReport = filteredReports
                .Select(r => new
                {
                    ФИО = r.FullName,
                    Группа = r.GroupName,
                    ВсегоКОплате = $"{r.TotalToPay:N2} руб.",
                    Оплачено = $"{r.TotalPaid:N2} руб.",
                    Задолженность = $"{r.Debt:N2} руб.",
                    СтатусОплаты = r.PaymentStatus,
                    ПоследняяОплата = r.LastPaymentDate?.ToString("dd.MM.yyyy") ?? "Не было"
                })
                .OrderByDescending(r => r.Задолженность)
                .ThenBy(r => r.ФИО)
                .ToList();

            PaymentsDataGrid.ItemsSource = paymentsReport;
        }

        private void GenerateDurationReport(List<StudentReport> reports)
        {
            var durationReport = reports
                .Select(r => new
                {
                    ФИО = r.FullName,
                    Группа = r.GroupName,
                    НачалоОбучения = r.GroupStartDate != DateTime.MinValue ? r.GroupStartDate.ToString("dd.MM.yyyy") : "Не указана",
                    КонецОбучения = r.GroupEndDate != DateTime.MinValue ? r.GroupEndDate.ToString("dd.MM.yyyy") : "Не указана",
                    Продолжительность = r.StudyDuration,
                    Статус = GetStudyStatus(r.GroupStartDate, r.GroupEndDate)
                })
                .OrderBy(r => r.НачалоОбучения)
                .ToList();

            DurationDataGrid.ItemsSource = durationReport;
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GenerateMainReport();
        }

        private void PaymentStatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var allReports = _students.Students
                .Select(student => CreateStudentReport(student))
                .Where(report => report != null)
                .ToList();

            var filteredReports = ApplyFilters(allReports);
            GeneratePaymentsReport(filteredReports);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
            InitializeFilters();
            GenerateMainReport();
            MessageBox.Show("Данные обновлены", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private string CalculateStudyDuration(StudyGroup group)
        {
            if (group == null || group.StartDate == DateTime.MinValue)
                return "Не назначена";

            var today = DateTime.Today;

            if (today < group.StartDate)
            {
                var daysUntilStart = (group.StartDate - today).Days;
                return $"Начнется через {daysUntilStart} дн.";
            }

            if (today > group.EndDate)
            {
                var totalDays = (group.EndDate - group.StartDate).Days;
                return $"Завершено ({totalDays} дн.)";
            }

            var daysPassed = (today - group.StartDate).Days;
            var daysLeft = (group.EndDate - today).Days;

            return $"{daysPassed} дн. (осталось {daysLeft} дн.)";
        }

        private string GetStudyStatus(DateTime startDate, DateTime endDate)
        {
            var today = DateTime.Today;

            if (startDate == DateTime.MinValue) return "Не назначено";
            if (today < startDate) return "Не началось";
            if (today > endDate) return "Завершено";
            return "В процессе";
        }

        private int GetDocumentsCount(StudentReport report)
        {
            int count = 0;
            if (report.HasPassport) count++;
            if (report.HasSNILS) count++;
            if (report.HasMedical) count++;
            if (report.HasAddress) count++;
            return count;
        }

        private string GetDocumentsStatus(StudentReport report)
        {
            int count = GetDocumentsCount(report);

            // Старый синтаксис switch (работает в C# 7.3)
            switch (count)
            {
                case 4:
                    return "Все документы";
                case 3:
                    return "Не хватает 1 документа";
                case 2:
                    return "Не хватает 2 документов";
                case 1:
                    return "Не хватает 3 документов";
                case 0:
                    return "Нет документов";
                default:
                    return "Неизвестно";
            }
        }

        private string GetGroupStatusName(string status)
        {
            // Старый синтаксис switch
            switch (status)
            {
                case "Активна":
                    return "Активная";
                case "Завершена":
                    return "Завершена";
                case "Запланирована":
                    return "Запланирована";
                case "Отменена":
                    return "Отменена";
                default:
                    return status ?? "Неизвестен";
            }
        }

        private string GetPaymentStatus(decimal debt, decimal totalPaid, decimal totalToPay)
        {
            if (totalToPay == 0) return "Нет стоимости";
            if (debt <= 0) return "Полностью оплачено";
            if (totalPaid == 0) return "Не оплачено";
            return "Частично оплачено";
        }
    }

    public class StudentReport
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string GroupName { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public string Citizenship { get; set; }
        public string CategoryCode { get; set; }
        public decimal TotalToPay { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Debt { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public int PaymentCount { get; set; }
        public string PaymentStatus { get; set; }
        public bool HasPassport { get; set; }
        public bool HasSNILS { get; set; }
        public bool HasMedical { get; set; }
        public bool HasAddress { get; set; }
        public string StudyDuration { get; set; }
        public DateTime GroupStartDate { get; set; }
        public DateTime GroupEndDate { get; set; }
    }
}