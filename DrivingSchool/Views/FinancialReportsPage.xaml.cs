using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DrivingSchool.Models;
using DrivingSchool.Services;

namespace DrivingSchool.Views
{
    public partial class FinancialReportsPage : Page
    {
        private readonly SqlDataService _dataService;
        private StudentCollection _students;
        private PaymentCollection _payments;
        private StudentTuitionCollection _tuitions;
        private TariffCollection _tariffs;
        private StudyGroupCollection _groups;
        private VehicleCategoryCollection _vehicleCategories;

        public FinancialReportsPage(SqlDataService dataService)
        {
            InitializeComponent();
            _dataService = dataService;

            // ИНИЦИАЛИЗАЦИЯ: сразу создаем пустые коллекции
            InitializeEmptyCollections();

            // Загружаем данные
            LoadData();

            // Инициализируем фильтры
            InitializeDateFilters();

            // Генерируем отчет
            GenerateGeneralReport();
        }

        private void InitializeEmptyCollections()
        {
            _students = new StudentCollection { Students = new List<Student>() };
            _payments = new PaymentCollection { Payments = new List<Payment>() };
            _tuitions = new StudentTuitionCollection { Tuitions = new List<StudentTuition>() };
            _tariffs = new TariffCollection { Tariffs = new List<Tariff>() };
            _groups = new StudyGroupCollection { Groups = new List<StudyGroup>() };
            _vehicleCategories = new VehicleCategoryCollection { Categories = new List<VehicleCategory>() };
        }

        private void LoadData()
        {
            try
            {
                var students = _dataService.LoadStudents();
                if (students?.Students != null)
                    _students = students;

                var payments = _dataService.LoadPayments();
                if (payments?.Payments != null)
                    _payments = payments;

                var groups = _dataService.LoadStudyGroups();
                if (groups?.Groups != null)
                    _groups = groups;

                var categories = _dataService.LoadVehicleCategories();
                if (categories?.Categories != null)
                    _vehicleCategories = categories;

                // Загружаем остальные данные через try-catch, т.к. методов может не быть
                try
                {
                    var tuitions = _dataService.LoadStudentTuitions();
                    if (tuitions?.Tuitions != null)
                        _tuitions = tuitions;
                }
                catch
                {
                    // Метод может отсутствовать, оставляем пустую коллекцию
                    _tuitions = new StudentTuitionCollection { Tuitions = new List<StudentTuition>() };
                }

                try
                {
                    var tariffs = _dataService.LoadTariffs();
                    if (tariffs?.Tariffs != null)
                        _tariffs = tariffs;
                }
                catch
                {
                    _tariffs = new TariffCollection { Tariffs = new List<Tariff>() };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки данных: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                // Коллекции уже инициализированы пустыми в конструкторе
            }
        }

        private void InitializeDateFilters()
        {
            try
            {
                var currentDate = DateTime.Now;
                StartDatePicker.SelectedDate = new DateTime(currentDate.Year, currentDate.Month, 1);
                EndDatePicker.SelectedDate = currentDate;
                PeriodComboBox.SelectedIndex = 2; // За текущий месяц
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка инициализации фильтров: {ex.Message}");
            }
        }

        private void PeriodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (PeriodComboBox.SelectedIndex == -1) return;

                var currentDate = DateTime.Now;

                switch (PeriodComboBox.SelectedIndex)
                {
                    case 0: // За сегодня
                        StartDatePicker.SelectedDate = currentDate.Date;
                        EndDatePicker.SelectedDate = currentDate.Date;
                        StartDatePicker.IsEnabled = false;
                        EndDatePicker.IsEnabled = false;
                        break;

                    case 1: // За текущую неделю
                        var diff = (7 + (currentDate.DayOfWeek - DayOfWeek.Monday)) % 7;
                        StartDatePicker.SelectedDate = currentDate.AddDays(-diff).Date;
                        EndDatePicker.SelectedDate = currentDate.Date;
                        StartDatePicker.IsEnabled = false;
                        EndDatePicker.IsEnabled = false;
                        break;

                    case 2: // За текущий месяц
                        StartDatePicker.SelectedDate = new DateTime(currentDate.Year, currentDate.Month, 1);
                        EndDatePicker.SelectedDate = currentDate.Date;
                        StartDatePicker.IsEnabled = false;
                        EndDatePicker.IsEnabled = false;
                        break;

                    case 3: // За текущий квартал
                        var quarter = (currentDate.Month - 1) / 3;
                        var quarterStartMonth = quarter * 3 + 1;
                        StartDatePicker.SelectedDate = new DateTime(currentDate.Year, quarterStartMonth, 1);
                        EndDatePicker.SelectedDate = currentDate.Date;
                        StartDatePicker.IsEnabled = false;
                        EndDatePicker.IsEnabled = false;
                        break;

                    case 4: // За текущий год
                        StartDatePicker.SelectedDate = new DateTime(currentDate.Year, 1, 1);
                        EndDatePicker.SelectedDate = currentDate.Date;
                        StartDatePicker.IsEnabled = false;
                        EndDatePicker.IsEnabled = false;
                        break;

                    case 5: // За все время
                        StartDatePicker.SelectedDate = new DateTime(2020, 1, 1);
                        EndDatePicker.SelectedDate = currentDate.Date;
                        StartDatePicker.IsEnabled = false;
                        EndDatePicker.IsEnabled = false;
                        break;

                    case 6: // Произвольный период
                        StartDatePicker.IsEnabled = true;
                        EndDatePicker.IsEnabled = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в PeriodComboBox_SelectionChanged: {ex.Message}");
            }
        }

        private List<Payment> GetPaymentsInPeriod(DateTime? start, DateTime? end)
        {
            // Проверка на null
            if (_payments?.Payments == null || !_payments.Payments.Any())
                return new List<Payment>();

            var query = _payments.Payments.AsEnumerable();

            if (start.HasValue)
                query = query.Where(p => p.PaymentDate >= start.Value.Date);

            if (end.HasValue)
                query = query.Where(p => p.PaymentDate <= end.Value.Date.AddDays(1).AddTicks(-1));

            return query.ToList();
        }

        private void GenerateGeneralReport()
        {
            try
            {
                var startDate = StartDatePicker.SelectedDate;
                var endDate = EndDatePicker.SelectedDate;

                var periodPayments = GetPaymentsInPeriod(startDate, endDate);

                var totalIncome = periodPayments.Sum(p => p.Amount);
                var paymentCount = periodPayments.Count;
                var avgPayment = paymentCount > 0 ? totalIncome / paymentCount : 0;

                // Расчет задолженности с проверкой на null
                decimal totalExpectedIncome = 0;
                decimal totalPaidAllTime = 0;

                if (_tuitions?.Tuitions != null)
                    totalExpectedIncome = _tuitions.Tuitions.Sum(t => t.FinalAmount);

                if (_payments?.Payments != null)
                    totalPaidAllTime = _payments.Payments.Sum(p => p.Amount);

                var totalDebt = totalExpectedIncome - totalPaidAllTime;

                // Обновляем UI элементы (проверяем, что они существуют)
                if (TotalIncomeText != null)
                    TotalIncomeText.Text = $"{totalIncome:N2} руб.";

                if (TotalPaymentsText != null)
                    TotalPaymentsText.Text = paymentCount.ToString();

                if (AveragePaymentText != null)
                    AveragePaymentText.Text = $"{avgPayment:N2} руб.";

                if (TotalDebtText != null)
                    TotalDebtText.Text = $"{Math.Max(0, totalDebt):N2} руб.";

                // Статистика по типам платежей
                var paymentTypes = periodPayments
                    .GroupBy(p => p.PaymentType ?? "Не указан")
                    .Select(g => new PaymentTypeStat
                    {
                        Type = g.Key,
                        Amount = g.Sum(p => p.Amount),
                        Count = g.Count()
                    })
                    .ToList();

                if (PaymentTypesDataGrid != null)
                    PaymentTypesDataGrid.ItemsSource = paymentTypes;

                // Последние платежи
                var recentPayments = periodPayments
                    .OrderByDescending(p => p.PaymentDate)
                    .Take(20)
                    .Select(p => new PaymentDetail
                    {
                        Id = p.Id,
                        StudentName = GetStudentName(p.StudentId),
                        PaymentDate = p.PaymentDate,
                        Amount = p.Amount,
                        PaymentType = p.PaymentType ?? "Не указан"
                    })
                    .ToList();

                if (RecentPaymentsDataGrid != null)
                    RecentPaymentsDataGrid.ItemsSource = recentPayments;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при формировании общего отчета: {ex.Message}");
                MessageBox.Show($"Ошибка при формировании отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GeneratePaymentsReport()
        {
            try
            {
                var startDate = StartDatePicker.SelectedDate;
                var endDate = EndDatePicker.SelectedDate;

                var periodPayments = GetPaymentsInPeriod(startDate, endDate);

                var detailedPayments = periodPayments
                    .Select(p => new PaymentDetail
                    {
                        Id = p.Id,
                        StudentName = GetStudentName(p.StudentId),
                        PaymentDate = p.PaymentDate,
                        Amount = p.Amount,
                        PaymentType = p.PaymentType ?? "Не указан"
                    })
                    .OrderByDescending(p => p.PaymentDate)
                    .ToList();

                if (PaymentsDataGrid != null)
                    PaymentsDataGrid.ItemsSource = detailedPayments;

                var total = detailedPayments.Sum(p => p.Amount);
                var count = detailedPayments.Count;

                if (PaymentsSummaryText != null)
                    PaymentsSummaryText.Text = $"Всего оплат: {count} на сумму {total:N2} руб.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при формировании отчета по оплатам: {ex.Message}");
                MessageBox.Show($"Ошибка при формировании отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateStudentsReport()
        {
            try
            {
                var startDate = StartDatePicker.SelectedDate;
                var endDate = EndDatePicker.SelectedDate;

                var periodPayments = GetPaymentsInPeriod(startDate, endDate);

                // Проверка на null
                if (_students?.Students == null)
                {
                    if (StudentsSummaryText != null)
                        StudentsSummaryText.Text = "Нет данных о студентах";
                    return;
                }

                var studentFinancials = _students.Students
                    .Select(student =>
                    {
                        var tuition = _tuitions?.Tuitions?.FirstOrDefault(t => t.StudentId == student.Id);

                        var allStudentPayments = _payments?.Payments?
                            .Where(p => p.StudentId == student.Id)
                            .ToList() ?? new List<Payment>();

                        var studentPeriodPayments = periodPayments
                            .Where(p => p.StudentId == student.Id)
                            .ToList();

                        var totalToPay = tuition?.FinalAmount ?? 0;
                        var totalPaidAllTime = allStudentPayments.Sum(p => p.Amount);
                        var paidInPeriod = studentPeriodPayments.Sum(p => p.Amount);
                        var debt = totalToPay - totalPaidAllTime;

                        return new StudentFinancialInfo
                        {
                            StudentId = student.Id,
                            StudentName = student.FullName,
                            GroupName = GetGroupName(student.GroupId),
                            TotalToPay = totalToPay,
                            TotalPaid = totalPaidAllTime,
                            PaidInPeriod = paidInPeriod,
                            Debt = debt,
                            Status = GetPaymentStatus(debt, totalPaidAllTime, totalToPay)
                        };
                    })
                    .Where(s => s.TotalToPay > 0 || s.TotalPaid > 0)
                    .OrderByDescending(s => s.Debt)
                    .ToList();

                if (StudentsDataGrid != null)
                    StudentsDataGrid.ItemsSource = studentFinancials;

                var withDebt = studentFinancials.Count(s => s.Debt > 0);
                var totalDebt = studentFinancials.Where(s => s.Debt > 0).Sum(s => s.Debt);

                if (StudentsSummaryText != null)
                    StudentsSummaryText.Text = $"Студентов с оплатами: {studentFinancials.Count} | " +
                                              $"С долгом: {withDebt} | Общий долг: {totalDebt:N2} руб.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при формировании отчета по студентам: {ex.Message}");
                MessageBox.Show($"Ошибка при формировании отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateGroupsReport()
        {
            try
            {
                var startDate = StartDatePicker.SelectedDate;
                var endDate = EndDatePicker.SelectedDate;

                var periodPayments = GetPaymentsInPeriod(startDate, endDate);

                // Проверка на null
                if (_groups?.Groups == null || _students?.Students == null)
                {
                    if (GroupsDataGrid != null)
                        GroupsDataGrid.ItemsSource = null;
                    return;
                }

                var groupFinancials = _groups.Groups
                    .Select(group =>
                    {
                        var groupStudents = _students.Students
                            .Where(s => s.GroupId == group.Id)
                            .ToList();

                        var groupTuitions = _tuitions?.Tuitions?
                            .Where(t => groupStudents.Any(s => s.Id == t.StudentId))
                            .ToList() ?? new List<StudentTuition>();

                        var allGroupPayments = _payments?.Payments?
                            .Where(p => groupStudents.Any(s => s.Id == p.StudentId))
                            .ToList() ?? new List<Payment>();

                        var groupPeriodPayments = periodPayments
                            .Where(p => groupStudents.Any(s => s.Id == p.StudentId))
                            .ToList();

                        var expectedIncome = groupTuitions.Sum(t => t.FinalAmount);
                        var actualIncomeAllTime = allGroupPayments.Sum(p => p.Amount);
                        var actualIncomeInPeriod = groupPeriodPayments.Sum(p => p.Amount);
                        var debt = expectedIncome - actualIncomeAllTime;

                        return new GroupFinancialInfo
                        {
                            GroupName = group.Name ?? "Без названия",
                            StudentCount = groupStudents.Count,
                            Status = group.Status ?? "Неизвестен",
                            ExpectedIncome = expectedIncome,
                            ActualIncome = actualIncomeInPeriod,
                            ActualIncomeAllTime = actualIncomeAllTime,
                            Debt = debt,
                            CompletionRate = expectedIncome > 0 ? (actualIncomeAllTime / expectedIncome * 100) : 0
                        };
                    })
                    .Where(g => g.StudentCount > 0)
                    .OrderByDescending(g => g.ActualIncome)
                    .ToList();

                if (GroupsDataGrid != null)
                    GroupsDataGrid.ItemsSource = groupFinancials;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при формировании отчета по группам: {ex.Message}");
                MessageBox.Show($"Ошибка при формировании отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateCategoriesReport()
        {
            try
            {
                var startDate = StartDatePicker.SelectedDate;
                var endDate = EndDatePicker.SelectedDate;

                var periodPayments = GetPaymentsInPeriod(startDate, endDate);

                // Проверка на null
                if (_vehicleCategories?.Categories == null || _students?.Students == null)
                {
                    if (CategoriesDataGrid != null)
                        CategoriesDataGrid.ItemsSource = null;
                    return;
                }

                var categoriesReport = _vehicleCategories.Categories
                    .Select(category =>
                    {
                        var categoryStudents = _students.Students
                            .Where(s => s.VehicleCategoryId == category.Id)
                            .ToList();

                        var categoryTuitions = _tuitions?.Tuitions?
                            .Where(t => categoryStudents.Any(s => s.Id == t.StudentId))
                            .ToList() ?? new List<StudentTuition>();

                        var categoryPeriodPayments = periodPayments
                            .Where(p => categoryStudents.Any(s => s.Id == p.StudentId))
                            .ToList();

                        var expectedIncome = categoryTuitions.Sum(t => t.FinalAmount);
                        var actualIncome = categoryPeriodPayments.Sum(p => p.Amount);

                        var allPayments = _payments?.Payments?
                            .Where(p => categoryStudents.Any(s => s.Id == p.StudentId))
                            .Sum(p => p.Amount) ?? 0;

                        var debt = expectedIncome - allPayments;

                        var averagePayment = categoryPeriodPayments.Any() ?
                            categoryPeriodPayments.Average(p => p.Amount) : 0;

                        return new CategoryFinancialInfo
                        {
                            CategoryName = $"{category.Code} - {category.FullName}",
                            StudentCount = categoryStudents.Count,
                            ExpectedIncome = expectedIncome,
                            ActualIncome = actualIncome,
                            Debt = debt,
                            AveragePayment = averagePayment
                        };
                    })
                    .Where(c => c.StudentCount > 0)
                    .OrderByDescending(c => c.ActualIncome)
                    .ToList();

                if (CategoriesDataGrid != null)
                    CategoriesDataGrid.ItemsSource = categoriesReport;

                var totalStudents = categoriesReport.Sum(c => c.StudentCount);
                var totalIncome = categoriesReport.Sum(c => c.ActualIncome);

                if (CategoriesSummaryText != null)
                    CategoriesSummaryText.Text = $"Всего студентов по категориям: {totalStudents} | " +
                                                $"Доход за период: {totalIncome:N2} руб.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при формировании отчета по категориям: {ex.Message}");
                MessageBox.Show($"Ошибка при формировании отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetStudentName(int studentId)
        {
            if (_students?.Students == null)
                return "Неизвестный студент";

            var student = _students.Students.FirstOrDefault(s => s.Id == studentId);
            return student?.FullName ?? "Неизвестный студент";
        }

        private string GetGroupName(int groupId)
        {
            if (_groups?.Groups == null)
                return "Не назначена";

            var group = _groups.Groups.FirstOrDefault(g => g.Id == groupId);
            return group?.Name ?? "Не назначена";
        }

        private string GetPaymentStatus(decimal debt, decimal totalPaid, decimal totalToPay)
        {
            if (totalToPay == 0) return "Нет стоимости";
            if (debt <= 0) return "Оплачено полностью";
            if (totalPaid == 0) return "Не оплачено";
            return "Частично оплачено";
        }

        private void GenerateReportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
                {
                    MessageBox.Show("Выберите период для отчета", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (StartDatePicker.SelectedDate > EndDatePicker.SelectedDate)
                {
                    MessageBox.Show("Дата начала не может быть больше даты окончания", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверяем, выбран ли TabControl и его индекс
                if (ReportTypeTabControl == null)
                    return;

                switch (ReportTypeTabControl.SelectedIndex)
                {
                    case 0:
                        GenerateGeneralReport();
                        break;
                    case 1:
                        GeneratePaymentsReport();
                        break;
                    case 2:
                        GenerateStudentsReport();
                        break;
                    case 3:
                        GenerateGroupsReport();
                        break;
                    case 4:
                        GenerateCategoriesReport();
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при формировании отчета: {ex.Message}");
                MessageBox.Show($"Ошибка при формировании отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Вспомогательные классы для отчетов (оставляем без изменений)
    public class PaymentDetail
    {
        public int Id { get; set; }
        public string StudentName { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentType { get; set; }

        public string PaymentDateString => PaymentDate.ToString("dd.MM.yyyy HH:mm");
        public string AmountString => $"{Amount:N2} руб.";
    }

    public class PaymentTypeStat
    {
        public string Type { get; set; }
        public decimal Amount { get; set; }
        public int Count { get; set; }
        public string AmountString => $"{Amount:N2} руб.";
    }

    public class StudentFinancialInfo
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string GroupName { get; set; }
        public decimal TotalToPay { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal PaidInPeriod { get; set; }
        public decimal Debt { get; set; }
        public string Status { get; set; }

        public string TotalToPayString => $"{TotalToPay:N2} руб.";
        public string TotalPaidString => $"{TotalPaid:N2} руб.";
        public string PaidInPeriodString => $"{PaidInPeriod:N2} руб.";
        public string DebtString => $"{Debt:N2} руб.";
    }

    public class GroupFinancialInfo
    {
        public string GroupName { get; set; }
        public int StudentCount { get; set; }
        public string Status { get; set; }
        public decimal ExpectedIncome { get; set; }
        public decimal ActualIncome { get; set; }
        public decimal ActualIncomeAllTime { get; set; }
        public decimal Debt { get; set; }
        public decimal CompletionRate { get; set; }

        public string ExpectedIncomeString => $"{ExpectedIncome:N2} руб.";
        public string ActualIncomeString => $"{ActualIncome:N2} руб.";
        public string ActualIncomeAllTimeString => $"{ActualIncomeAllTime:N2} руб.";
        public string DebtString => $"{Debt:N2} руб.";
        public string CompletionRateString => $"{CompletionRate:N1}%";
    }

    public class CategoryFinancialInfo
    {
        public string CategoryName { get; set; }
        public int StudentCount { get; set; }
        public decimal ExpectedIncome { get; set; }
        public decimal ActualIncome { get; set; }
        public decimal Debt { get; set; }
        public decimal AveragePayment { get; set; }

        public string ExpectedIncomeString => $"{ExpectedIncome:N2} руб.";
        public string ActualIncomeString => $"{ActualIncome:N2} руб.";
        public string DebtString => $"{Debt:N2} руб.";
        public string AveragePaymentString => $"{AveragePayment:N2} руб.";
    }
}