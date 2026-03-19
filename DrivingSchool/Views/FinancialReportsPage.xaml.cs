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
            InitializeEmptyCollections();
            LoadData();
            InitializeDateFilters();
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

                // Загружаем стоимости обучения и тарифы
                try
                {
                    var tuitions = _dataService.LoadStudentTuitions();
                    if (tuitions?.Tuitions != null)
                    {
                        _tuitions = tuitions;

                        // Загружаем оплаты для каждого студента, чтобы рассчитать долг
                        foreach (var tuition in _tuitions.Tuitions)
                        {
                            var studentPayments = _payments?.Payments?
                                .Where(p => p.StudentId == tuition.StudentId)
                                .ToList() ?? new List<Payment>();
                            tuition.PaidAmount = studentPayments.Sum(p => p.Amount);
                        }
                    }
                }
                catch
                {
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
            }
        }

        private void InitializeDateFilters()
        {
            try
            {
                var currentDate = DateTime.Now;
                StartDatePicker.SelectedDate = new DateTime(currentDate.Year, currentDate.Month, 1);
                EndDatePicker.SelectedDate = currentDate;
                PeriodComboBox.SelectedIndex = 2;
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
            if (_payments?.Payments == null || !_payments.Payments.Any())
                return new List<Payment>();

            var query = _payments.Payments.AsEnumerable();

            if (start.HasValue)
                query = query.Where(p => p.PaymentDate.Date >= start.Value.Date);

            if (end.HasValue)
                query = query.Where(p => p.PaymentDate.Date <= end.Value.Date);

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

                // ИСПРАВЛЕНО: Правильный расчет ожидаемого дохода и долгов
                decimal totalExpectedIncome = 0;
                decimal totalPaidAllTime = 0;
                decimal totalDebt = 0;

                if (_tuitions?.Tuitions != null)
                {
                    foreach (var tuition in _tuitions.Tuitions)
                    {
                        var finalAmount = tuition.FullAmount - tuition.Discount;
                        totalExpectedIncome += finalAmount;

                        var studentPayments = _payments?.Payments?
                            .Where(p => p.StudentId == tuition.StudentId)
                            .Sum(p => p.Amount) ?? 0;
                        totalPaidAllTime += studentPayments;

                        var debt = finalAmount - studentPayments;
                        if (debt > 0) totalDebt += debt; // Только положительные долги
                    }
                }

                // Обновляем UI
                if (TotalIncomeText != null)
                    TotalIncomeText.Text = $"{totalIncome:N2} руб.";

                if (TotalPaymentsText != null)
                    TotalPaymentsText.Text = paymentCount.ToString();

                if (AveragePaymentText != null)
                    AveragePaymentText.Text = $"{avgPayment:N2} руб.";

                if (TotalDebtText != null)
                    TotalDebtText.Text = $"{totalDebt:N2} руб.";

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
                    .ThenByDescending(p => p.CreatedDate) // ИСПРАВЛЕНО: учитываем время создания
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
                    .ThenByDescending(p => p.Id)
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

                if (_students?.Students == null)
                {
                    if (StudentsSummaryText != null)
                        StudentsSummaryText.Text = "Нет данных о студентах";
                    return;
                }

                // ИСПРАВЛЕНО: Правильный расчет финансов по студентам
                var studentFinancials = new List<StudentFinancialInfo>();

                foreach (var student in _students.Students)
                {
                    var tuition = _tuitions?.Tuitions?.FirstOrDefault(t => t.StudentId == student.Id);

                    if (tuition == null) continue; // Пропускаем студентов без стоимости обучения

                    var finalAmount = tuition.FullAmount - tuition.Discount;

                    var allStudentPayments = _payments?.Payments?
                        .Where(p => p.StudentId == student.Id)
                        .ToList() ?? new List<Payment>();

                    var studentPeriodPayments = periodPayments
                        .Where(p => p.StudentId == student.Id)
                        .ToList();

                    var totalPaidAllTime = allStudentPayments.Sum(p => p.Amount);
                    var paidInPeriod = studentPeriodPayments.Sum(p => p.Amount);
                    var debt = finalAmount - totalPaidAllTime;

                    studentFinancials.Add(new StudentFinancialInfo
                    {
                        StudentId = student.Id,
                        StudentName = student.FullName,
                        GroupName = GetGroupName(student.GroupId),
                        TotalToPay = finalAmount,
                        TotalPaid = totalPaidAllTime,
                        PaidInPeriod = paidInPeriod,
                        Debt = debt,
                        Status = GetPaymentStatus(debt, finalAmount)
                    });
                }

                studentFinancials = studentFinancials
                    .OrderByDescending(s => Math.Abs(s.Debt)) // Сортируем по модулю долга
                    .ToList();

                if (StudentsDataGrid != null)
                    StudentsDataGrid.ItemsSource = studentFinancials;

                var withDebt = studentFinancials.Count(s => s.Debt > 0);
                var withOverpayment = studentFinancials.Count(s => s.Debt < 0);
                var totalDebt = studentFinancials.Where(s => s.Debt > 0).Sum(s => s.Debt);
                var totalOverpayment = studentFinancials.Where(s => s.Debt < 0).Sum(s => Math.Abs(s.Debt));

                if (StudentsSummaryText != null)
                    StudentsSummaryText.Text = $"Студентов с оплатами: {studentFinancials.Count} | " +
                                              $"С долгом: {withDebt} ({totalDebt:N2} руб.) | " +
                                              $"С переплатой: {withOverpayment} ({totalOverpayment:N2} руб.)";
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

                if (_groups?.Groups == null || _students?.Students == null)
                {
                    if (GroupsDataGrid != null)
                        GroupsDataGrid.ItemsSource = null;
                    return;
                }

                // ИСПРАВЛЕНО: Правильный расчет финансов по группам
                var groupFinancials = new List<GroupFinancialInfo>();

                foreach (var group in _groups.Groups)
                {
                    var groupStudents = _students.Students
                        .Where(s => s.GroupId == group.Id)
                        .ToList();

                    if (!groupStudents.Any()) continue;

                    decimal expectedIncome = 0;
                    decimal actualIncomeAllTime = 0;
                    decimal actualIncomeInPeriod = 0;

                    foreach (var student in groupStudents)
                    {
                        var tuition = _tuitions?.Tuitions?
                            .FirstOrDefault(t => t.StudentId == student.Id);

                        if (tuition != null)
                        {
                            var finalAmount = tuition.FullAmount - tuition.Discount;
                            expectedIncome += finalAmount;
                        }

                        var allStudentPayments = _payments?.Payments?
                            .Where(p => p.StudentId == student.Id)
                            .Sum(p => p.Amount) ?? 0;
                        actualIncomeAllTime += allStudentPayments;

                        var periodStudentPayments = periodPayments
                            .Where(p => p.StudentId == student.Id)
                            .Sum(p => p.Amount);
                        actualIncomeInPeriod += periodStudentPayments;
                    }

                    var debt = expectedIncome - actualIncomeAllTime;
                    var completionRate = expectedIncome > 0 ?
                        (actualIncomeAllTime / expectedIncome * 100) : 0;

                    groupFinancials.Add(new GroupFinancialInfo
                    {
                        GroupName = group.Name ?? "Без названия",
                        StudentCount = groupStudents.Count,
                        Status = group.Status ?? "Неизвестен",
                        ExpectedIncome = expectedIncome,
                        ActualIncome = actualIncomeInPeriod,
                        ActualIncomeAllTime = actualIncomeAllTime,
                        Debt = debt > 0 ? debt : 0, // Только положительный долг
                        Overpayment = debt < 0 ? Math.Abs(debt) : 0,
                        CompletionRate = completionRate
                    });
                }

                groupFinancials = groupFinancials
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

                if (_vehicleCategories?.Categories == null || _students?.Students == null)
                {
                    if (CategoriesDataGrid != null)
                        CategoriesDataGrid.ItemsSource = null;
                    return;
                }

                // ИСПРАВЛЕНО: Правильный расчет по категориям
                var categoriesReport = new List<CategoryFinancialInfo>();

                foreach (var category in _vehicleCategories.Categories)
                {
                    var categoryStudents = _students.Students
                        .Where(s => s.VehicleCategoryId == category.Id)
                        .ToList();

                    if (!categoryStudents.Any()) continue;

                    decimal expectedIncome = 0;
                    decimal actualIncomeAllTime = 0;
                    decimal actualIncomeInPeriod = 0;

                    foreach (var student in categoryStudents)
                    {
                        var tuition = _tuitions?.Tuitions?
                            .FirstOrDefault(t => t.StudentId == student.Id);

                        if (tuition != null)
                        {
                            var finalAmount = tuition.FullAmount - tuition.Discount;
                            expectedIncome += finalAmount;
                        }

                        var allStudentPayments = _payments?.Payments?
                            .Where(p => p.StudentId == student.Id)
                            .Sum(p => p.Amount) ?? 0;
                        actualIncomeAllTime += allStudentPayments;

                        var periodStudentPayments = periodPayments
                            .Where(p => p.StudentId == student.Id)
                            .Sum(p => p.Amount);
                        actualIncomeInPeriod += periodStudentPayments;
                    }

                    var debt = expectedIncome - actualIncomeAllTime;

                    // ИСПРАВЛЕНО: Средняя стоимость обучения, а не средний платеж
                    var averageTuitionCost = categoryStudents
                        .Select(s => _tuitions?.Tuitions?
                            .FirstOrDefault(t => t.StudentId == s.Id))
                        .Where(t => t != null)
                        .Select(t => t.FullAmount - t.Discount)
                        .DefaultIfEmpty(0)
                        .Average();

                    categoriesReport.Add(new CategoryFinancialInfo
                    {
                        CategoryName = $"{category.Code} - {category.FullName}",
                        StudentCount = categoryStudents.Count,
                        ExpectedIncome = expectedIncome,
                        ActualIncome = actualIncomeInPeriod,
                        Debt = debt > 0 ? debt : 0,
                        AverageTuitionCost = averageTuitionCost,
                        CompletionRate = expectedIncome > 0 ?
                            (actualIncomeAllTime / expectedIncome * 100) : 0
                    });
                }

                categoriesReport = categoriesReport
                    .OrderByDescending(c => c.ActualIncome)
                    .ToList();

                if (CategoriesDataGrid != null)
                    CategoriesDataGrid.ItemsSource = categoriesReport;

                var totalStudents = categoriesReport.Sum(c => c.StudentCount);
                var totalIncome = categoriesReport.Sum(c => c.ActualIncome);
                var totalDebt = categoriesReport.Sum(c => c.Debt);

                if (CategoriesSummaryText != null)
                    CategoriesSummaryText.Text = $"Всего студентов: {totalStudents} | " +
                                                $"Доход за период: {totalIncome:N2} руб. | " +
                                                $"Долг: {totalDebt:N2} руб.";
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

        // ИСПРАВЛЕНО: Правильное определение статуса оплаты
        private string GetPaymentStatus(decimal debt, decimal totalToPay)
        {
            if (totalToPay == 0) return "Нет стоимости";
            if (debt > 0) return "Неполная оплата";
            if (debt < 0) return "Переплата";
            return "Полная оплата";
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

    // ИСПРАВЛЕНО: Добавлены новые поля в классы для отчетов
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
        public string DebtString => $"{(Debt > 0 ? Debt : 0):N2} руб.";
        public string OverpaymentString => $"{(Debt < 0 ? -Debt : 0):N2} руб.";
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
        public decimal Overpayment { get; set; }
        public decimal CompletionRate { get; set; }

        public string ExpectedIncomeString => $"{ExpectedIncome:N2} руб.";
        public string ActualIncomeString => $"{ActualIncome:N2} руб.";
        public string ActualIncomeAllTimeString => $"{ActualIncomeAllTime:N2} руб.";
        public string DebtString => $"{Debt:N2} руб.";
        public string OverpaymentString => $"{Overpayment:N2} руб.";
        public string CompletionRateString => $"{CompletionRate:N1}%";
    }

    public class CategoryFinancialInfo
    {
        public string CategoryName { get; set; }
        public int StudentCount { get; set; }
        public decimal ExpectedIncome { get; set; }
        public decimal ActualIncome { get; set; }
        public decimal Debt { get; set; }
        public decimal AverageTuitionCost { get; set; }
        public decimal CompletionRate { get; set; }

        public string ExpectedIncomeString => $"{ExpectedIncome:N2} руб.";
        public string ActualIncomeString => $"{ActualIncome:N2} руб.";
        public string DebtString => $"{Debt:N2} руб.";
        public string AverageTuitionCostString => $"{AverageTuitionCost:N2} руб.";
        public string CompletionRateString => $"{CompletionRate:N1}%";
    }
}