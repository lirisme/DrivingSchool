using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using DrivingSchool.Models;

namespace DrivingSchool.Services
{
    public class ReportService
    {
        private readonly SqlDataService _dataService;
        private List<Student> _studentsCache;
        private List<Payment> _paymentsCache;
        private List<StudyGroup> _groupsCache;
        private List<StudentTuition> _tuitionsCache;

        public ReportService(SqlDataService dataService)
        {
            _dataService = dataService;
        }

        private List<Student> Students
        {
            get
            {
                if (_studentsCache == null)
                {
                    var data = _dataService.LoadStudents();
                    _studentsCache = data != null ? data.Students : new List<Student>();
                }
                return _studentsCache;
            }
        }

        private List<Payment> Payments
        {
            get
            {
                if (_paymentsCache == null)
                {
                    var data = _dataService.LoadPayments();
                    _paymentsCache = data != null ? data.Payments : new List<Payment>();
                }
                return _paymentsCache;
            }
        }

        private List<StudyGroup> Groups
        {
            get
            {
                if (_groupsCache == null)
                {
                    var data = _dataService.LoadStudyGroups();
                    _groupsCache = data != null ? data.Groups : new List<StudyGroup>();
                }
                return _groupsCache;
            }
        }

        private List<StudentTuition> Tuitions
        {
            get
            {
                if (_tuitionsCache == null)
                {
                    var data = _dataService.LoadStudentTuitions();
                    _tuitionsCache = data != null ? data.Tuitions : new List<StudentTuition>();
                }
                return _tuitionsCache;
            }
        }

        public List<Student> GetAllStudents() => Students;
        public List<Payment> GetAllPayments() => Payments;
        public List<StudyGroup> GetAllGroups() => Groups;

        // ФИНАНСОВАЯ СВОДКА ЗА ПЕРИОД
        public FinancialSummary GetFinancialSummary(DateTime startDate, DateTime endDate)
        {
            var payments = Payments
                .Where(p => p.PaymentDate.Date >= startDate.Date && p.PaymentDate.Date <= endDate.Date)
                .ToList();

            var totalIncome = payments.Sum(p => p.Amount);
            var paymentsCount = payments.Count;
            var avgPayment = paymentsCount > 0 ? totalIncome / paymentsCount : 0;

            decimal totalDebt = 0;
            foreach (var student in Students)
            {
                var tuition = Tuitions.FirstOrDefault(t => t.StudentId == student.Id);
                if (tuition != null)
                {
                    var finalAmount = tuition.FullAmount - tuition.Discount;
                    var paid = Payments.Where(p => p.StudentId == student.Id).Sum(p => p.Amount);
                    var debt = finalAmount - paid;
                    if (debt > 0) totalDebt += debt;
                }
            }

            return new FinancialSummary
            {
                PeriodStart = startDate,
                PeriodEnd = endDate,
                TotalIncome = totalIncome,
                PaymentsCount = paymentsCount,
                AveragePayment = avgPayment,
                TotalDebt = totalDebt
            };
        }

        // ДЕТАЛИЗАЦИЯ ПЛАТЕЖЕЙ
        public List<PaymentDetail> GetPaymentDetails(DateTime startDate, DateTime endDate)
        {
            return Payments
                .Where(p => p.PaymentDate.Date >= startDate.Date && p.PaymentDate.Date <= endDate.Date)
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
        }

        // РАЗБИВКА ПО МЕСЯЦАМ
        public List<MonthlyBreakdown> GetMonthlyBreakdown(DateTime startDate, DateTime endDate)
        {
            var payments = Payments
                .Where(p => p.PaymentDate.Date >= startDate.Date && p.PaymentDate.Date <= endDate.Date)
                .ToList();

            return payments
                .GroupBy(p => new { p.PaymentDate.Year, p.PaymentDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new MonthlyBreakdown
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy"),
                    Amount = g.Sum(p => p.Amount),
                    Count = g.Count()
                })
                .ToList();
        }

        // СПИСОК ДОЛЖНИКОВ
        public List<DebtorInfo> GetDebtors()
        {
            var result = new List<DebtorInfo>();

            foreach (var student in Students)
            {
                var tuition = Tuitions.FirstOrDefault(t => t.StudentId == student.Id);
                if (tuition == null) continue;

                var finalAmount = tuition.FullAmount - tuition.Discount;
                var paid = Payments.Where(p => p.StudentId == student.Id).Sum(p => p.Amount);
                var debt = finalAmount - paid;

                if (debt > 0)
                {
                    var lastPayment = Payments
                        .Where(p => p.StudentId == student.Id)
                        .OrderByDescending(p => p.PaymentDate)
                        .FirstOrDefault();

                    result.Add(new DebtorInfo
                    {
                        StudentId = student.Id,
                        StudentName = student.FullName,
                        GroupName = GetGroupName(student.GroupId),
                        DebtAmount = debt,
                        LastPaymentDate = lastPayment?.PaymentDate
                    });
                }
            }

            return result.OrderByDescending(d => d.DebtAmount).ToList();
        }

        // ВСЕ СТУДЕНТЫ С ФИНАНСОВОЙ ИНФОРМАЦИЕЙ
        public List<StudentFinancialInfo> GetAllStudentsFinancialInfo()
        {
            var result = new List<StudentFinancialInfo>();

            foreach (var student in Students)
            {
                var tuition = Tuitions.FirstOrDefault(t => t.StudentId == student.Id);
                var finalAmount = tuition != null ? tuition.FullAmount - tuition.Discount : 0;
                var paid = Payments.Where(p => p.StudentId == student.Id).Sum(p => p.Amount);
                var debt = finalAmount - paid;

                result.Add(new StudentFinancialInfo
                {
                    StudentId = student.Id,
                    StudentName = student.FullName,
                    GroupName = GetGroupName(student.GroupId),
                    TotalToPay = finalAmount,
                    TotalPaid = paid,
                    Debt = debt > 0 ? debt : 0,
                    PaymentProgress = finalAmount > 0 ? (int)((paid / finalAmount) * 100) : 0,
                    PaymentStatus = GetPaymentStatus(debt, finalAmount)
                });
            }

            return result.OrderByDescending(s => s.Debt).ToList();
        }

        // ОТЧЕТ ПО ГРУППАМ
        public List<GroupFinancialInfo> GetGroupsFinancialReport()
        {
            var result = new List<GroupFinancialInfo>();

            foreach (var group in Groups)
            {
                var groupStudents = Students.Where(s => s.GroupId == group.Id).ToList();
                if (!groupStudents.Any()) continue;

                decimal expectedIncome = 0;
                decimal actualIncome = 0;

                foreach (var student in groupStudents)
                {
                    var tuition = Tuitions.FirstOrDefault(t => t.StudentId == student.Id);
                    if (tuition != null)
                    {
                        expectedIncome += tuition.FullAmount - tuition.Discount;
                    }
                    actualIncome += Payments.Where(p => p.StudentId == student.Id).Sum(p => p.Amount);
                }

                result.Add(new GroupFinancialInfo
                {
                    GroupName = group.Name,
                    StudentCount = groupStudents.Count,
                    ExpectedIncome = expectedIncome,
                    ActualIncome = actualIncome,
                    Debt = expectedIncome - actualIncome,
                    CompletionRate = expectedIncome > 0 ? (actualIncome / expectedIncome * 100) : 0
                });
            }

            return result.OrderByDescending(g => g.ActualIncome).ToList();
        }

        // ДЕМОГРАФИЧЕСКИЙ ОТЧЕТ
        public DemographicReport GetDemographicReport()
        {
            var students = Students;

            int maleCount = students.Count(s => s.Gender == "Мужской");
            int femaleCount = students.Count(s => s.Gender == "Женский");

            var ageGroups = new Dictionary<string, int>
            {
                {"16-17", 0}, {"18-25", 0}, {"26-35", 0}, {"36-45", 0}, {"46+", 0}
            };

            foreach (var student in students)
            {
                if (student.BirthDate == null || student.BirthDate == DateTime.MinValue) continue;
                var age = CalculateAge(student.BirthDate);
                if (age <= 17) ageGroups["16-17"]++;
                else if (age <= 25) ageGroups["18-25"]++;
                else if (age <= 35) ageGroups["26-35"]++;
                else if (age <= 45) ageGroups["36-45"]++;
                else ageGroups["46+"]++;
            }

            return new DemographicReport
            {
                TotalStudents = students.Count,
                MaleCount = maleCount,
                FemaleCount = femaleCount,
                AverageAge = students.Where(s => s.BirthDate != null && s.BirthDate != DateTime.MinValue)
                             .Any() ? students.Where(s => s.BirthDate != null && s.BirthDate != DateTime.MinValue)
                             .Average(s => CalculateAge(s.BirthDate)) : 0,
                AgeDistribution = ageGroups
            };
        }

        // СТАТУС ДОКУМЕНТОВ
        public List<StudentDocumentStatus> GetStudentDocumentStatus()
        {
            var result = new List<StudentDocumentStatus>();

            foreach (var student in Students)
            {
                int count = 0;
                if (_dataService.LoadStudentPassport(student.Id) != null) count++;
                if (_dataService.LoadStudentSNILS(student.Id) != null) count++;
                if (_dataService.LoadStudentMedical(student.Id) != null) count++;
                if (_dataService.LoadStudentAddress(student.Id) != null) count++;

                result.Add(new StudentDocumentStatus
                {
                    StudentId = student.Id,
                    StudentName = student.FullName,
                    GroupName = GetGroupName(student.GroupId),
                    HasPassport = count >= 1,
                    HasSNILS = count >= 2,
                    HasMedical = count >= 3,
                    HasAddress = count >= 4,
                    DocumentsCount = count
                });
            }

            return result.OrderByDescending(r => r.DocumentsCount).ToList();
        }

        // ИСТЕКАЮЩИЕ ДОКУМЕНТЫ
        public List<ExpiringDocumentInfo> GetExpiringDocuments(int daysThreshold = 30)
        {
            var result = new List<ExpiringDocumentInfo>();
            var today = DateTime.Today;

            var medicals = _dataService.LoadMedicalData();
            foreach (var med in medicals.Certificates)
            {
                var daysLeft = (med.ValidUntil - today).Days;
                if (daysLeft <= daysThreshold)
                {
                    result.Add(new ExpiringDocumentInfo
                    {
                        StudentName = GetStudentName(med.StudentId),
                        DocumentType = "Медицинская справка",
                        DocumentNumber = $"{med.Series} {med.Number}",
                        ExpiryDate = med.ValidUntil,
                        DaysLeft = daysLeft
                    });
                }
            }

            var licenses = _dataService.LoadDrivingLicenses();
            foreach (var license in licenses.Licenses)
            {
                var daysLeft = (license.ExpiryDate - today).Days;
                if (daysLeft <= daysThreshold)
                {
                    result.Add(new ExpiringDocumentInfo
                    {
                        StudentName = GetStudentName(license.StudentId),
                        DocumentType = "Водительское удостоверение",
                        DocumentNumber = $"{license.Series} {license.Number}",
                        ExpiryDate = license.ExpiryDate,
                        DaysLeft = daysLeft
                    });
                }
            }

            return result.OrderBy(d => d.DaysLeft).ToList();
        }

        // ЭКСПОРТ В EXCEL (CSV ФОРМАТ)
        // ЭКСПОРТ ДОЛЖНИКОВ
        public void ExportDebtorsToExcel(List<DebtorInfo> data, string file)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=" + new string('=', 80));
            sb.AppendLine("ЧАСТНОЕ ПРОФЕССИОНАЛЬНОЕ ОБРАЗОВАТЕЛЬНОЕ УЧРЕЖДЕНИЕ «ЭЛИТА АВТО»");
            sb.AppendLine("СПИСОК ДОЛЖНИКОВ");
            sb.AppendLine($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}");
            sb.AppendLine("=" + new string('=', 80));
            sb.AppendLine();
            sb.AppendLine("ФИО студента;Группа;Сумма долга;Дата последнего платежа");
            foreach (var d in data)
                sb.AppendLine($"{d.StudentName};{d.GroupName};{d.DebtAmount:N2};{d.LastPaymentFormatted}");
            sb.AppendLine();
            sb.AppendLine($"Всего должников: {data.Count}");
            sb.AppendLine($"Общая сумма задолженности: {data.Sum(d => d.DebtAmount):N2} руб.");
            sb.AppendLine("=" + new string('=', 80));

            System.IO.File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
            MessageBox.Show($"Сохранено: {file}", "Успех");
        }

        // ЭКСПОРТ ПЛАТЕЖЕЙ
        public void ExportPaymentsToExcel(List<PaymentDetail> data, string file)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=" + new string('=', 80));
            sb.AppendLine("ЧАСТНОЕ ПРОФЕССИОНАЛЬНОЕ ОБРАЗОВАТЕЛЬНОЕ УЧРЕЖДЕНИЕ «ЭЛИТА АВТО»");
            sb.AppendLine("ДЕТАЛИЗАЦИЯ ПЛАТЕЖЕЙ");
            sb.AppendLine($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}");
            sb.AppendLine("=" + new string('=', 80));
            sb.AppendLine();
            sb.AppendLine("Дата;Студент;Сумма;Тип платежа");
            foreach (var p in data)
                sb.AppendLine($"{p.PaymentDateString};{p.StudentName};{p.Amount:N2};{p.PaymentType}");
            sb.AppendLine();
            sb.AppendLine($"Всего платежей: {data.Count}");
            sb.AppendLine($"Общая сумма: {data.Sum(p => p.Amount):N2} руб.");
            sb.AppendLine("=" + new string('=', 80));

            System.IO.File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
            MessageBox.Show($"Сохранено: {file}", "Успех");
        }

        // ЭКСПОРТ ПО МЕСЯЦАМ
        public void ExportMonthlyToExcel(List<MonthlyBreakdown> data, string file)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=" + new string('=', 80));
            sb.AppendLine("ЧАСТНОЕ ПРОФЕССИОНАЛЬНОЕ ОБРАЗОВАТЕЛЬНОЕ УЧРЕЖДЕНИЕ «ЭЛИТА АВТО»");
            sb.AppendLine("РАЗБИВКА ПОСТУПЛЕНИЙ ПО МЕСЯЦАМ");
            sb.AppendLine($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}");
            sb.AppendLine("=" + new string('=', 80));
            sb.AppendLine();
            sb.AppendLine("Месяц;Сумма поступлений;Количество платежей");
            foreach (var m in data)
                sb.AppendLine($"{m.MonthName};{m.Amount:N2};{m.Count}");
            sb.AppendLine();
            sb.AppendLine($"Итого за период: {data.Sum(m => m.Amount):N2} руб.");
            sb.AppendLine($"Всего платежей: {data.Sum(m => m.Count)}");
            sb.AppendLine("=" + new string('=', 80));

            System.IO.File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
            MessageBox.Show($"Сохранено: {file}", "Успех");
        }

        // ЭКСПОРТ СТУДЕНТОВ
        public void ExportStudentsToExcel(List<StudentFinancialInfo> data, string file)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=" + new string('=', 80));
            sb.AppendLine("ЧАСТНОЕ ПРОФЕССИОНАЛЬНОЕ ОБРАЗОВАТЕЛЬНОЕ УЧРЕЖДЕНИЕ «ЭЛИТА АВТО»");
            sb.AppendLine("ФИНАНСОВОЕ СОСТОЯНИЕ СТУДЕНТОВ");
            sb.AppendLine($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}");
            sb.AppendLine("=" + new string('=', 80));
            sb.AppendLine();
            sb.AppendLine("ФИО;Группа;Сумма к оплате;Оплачено;Долг;Статус");
            foreach (var s in data)
                sb.AppendLine($"{s.StudentName};{s.GroupName};{s.TotalToPay:N2};{s.TotalPaid:N2};{s.Debt:N2};{s.PaymentStatus}");
            sb.AppendLine();
            sb.AppendLine($"Всего студентов: {data.Count}");
            sb.AppendLine($"Общая сумма к оплате: {data.Sum(s => s.TotalToPay):N2} руб.");
            sb.AppendLine($"Общая сумма оплат: {data.Sum(s => s.TotalPaid):N2} руб.");
            sb.AppendLine($"Общая задолженность: {data.Sum(s => s.Debt):N2} руб.");
            sb.AppendLine("=" + new string('=', 80));

            System.IO.File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
            MessageBox.Show($"Сохранено: {file}", "Успех");
        }

        // ЭКСПОРТ ИСТЕКАЮЩИХ ДОКУМЕНТОВ
        public void ExportExpiringDocumentsToExcel(List<ExpiringDocumentInfo> data, string file)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=" + new string('=', 80));
            sb.AppendLine("ЧАСТНОЕ ПРОФЕССИОНАЛЬНОЕ ОБРАЗОВАТЕЛЬНОЕ УЧРЕЖДЕНИЕ «ЭЛИТА АВТО»");
            sb.AppendLine("ДОКУМЕНТЫ С ИСТЕКАЮЩИМ СРОКОМ ДЕЙСТВИЯ");
            sb.AppendLine($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}");
            sb.AppendLine("=" + new string('=', 80));
            sb.AppendLine();
            sb.AppendLine("ФИО студента;Тип документа;Номер;Дата истечения;Дней осталось");
            foreach (var d in data)
                sb.AppendLine($"{d.StudentName};{d.DocumentType};{d.DocumentNumber};{d.ExpiryDateString};{d.DaysLeft}");
            sb.AppendLine();
            sb.AppendLine($"Всего документов с истекающим сроком: {data.Count}");
            sb.AppendLine("=" + new string('=', 80));

            System.IO.File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
            MessageBox.Show($"Сохранено: {file}", "Успех");
        }

        // ЭКСПОРТ СПИСКА УЧАЩИХСЯ ПО ГРУППЕ
        public void ExportGroupStudentsToExcel(int groupId, string file)
        {
            var group = Groups.FirstOrDefault(g => g.Id == groupId);
            var students = Students.Where(s => s.GroupId == groupId).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("=" + new string('=', 80));
            sb.AppendLine("ЧАСТНОЕ ПРОФЕССИОНАЛЬНОЕ ОБРАЗОВАТЕЛЬНОЕ УЧРЕЖДЕНИЕ «ЭЛИТА АВТО»");
            sb.AppendLine("СПИСОК УЧАЩИХСЯ ПО ГРУППАМ");
            sb.AppendLine($"Дата формирования: {DateTime.Now:dd.MM.yyyy HH:mm}");
            sb.AppendLine("=" + new string('=', 80));
            sb.AppendLine();
            sb.AppendLine($"Наименование группы: {group?.Name ?? "Не указана"}");
            sb.AppendLine($"Категория ТС: {group?.Category ?? "Не указана"}");
            sb.AppendLine($"Дата начала обучения: {group?.StartDate:dd.MM.yyyy}");
            sb.AppendLine($"Дата окончания обучения: {group?.EndDate:dd.MM.yyyy}");
            sb.AppendLine($"Статус группы: {group?.Status ?? "Не указан"}");
            sb.AppendLine();
            sb.AppendLine(new string('-', 80));
            sb.AppendLine("№;ФИО учащегося;Контактный телефон;Email");
            sb.AppendLine(new string('-', 80));

            int i = 1;
            foreach (var s in students)
            {
                sb.AppendLine($"{i};{s.FullName};{s.Phone ?? "Не указан"};{s.Email ?? "Не указан"}");
                i++;
            }

            sb.AppendLine(new string('-', 80));
            sb.AppendLine();
            sb.AppendLine($"Всего учащихся в группе: {students.Count}");
            sb.AppendLine("=" + new string('=', 80));

            System.IO.File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
            MessageBox.Show($"Сохранено: {file}\n\nВсего студентов: {students.Count}", "Успех");
        }

        // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        private string GetStudentName(int studentId)
        {
            return Students.FirstOrDefault(s => s.Id == studentId)?.FullName ?? "Неизвестный";
        }

        private string GetGroupName(int groupId)
        {
            return Groups.FirstOrDefault(g => g.Id == groupId)?.Name ?? "Не назначена";
        }

        private int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age;
        }

        private string GetPaymentStatus(decimal debt, decimal totalToPay)
        {
            if (totalToPay == 0) return "Бесплатно";
            if (debt <= 0) return "Оплачено полностью";
            if (debt == totalToPay) return "Не оплачено";
            return "Частично оплачено";
        }
    }

    // ========== DTO КЛАССЫ ==========
    public class FinancialSummary
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal TotalIncome { get; set; }
        public int PaymentsCount { get; set; }
        public decimal AveragePayment { get; set; }
        public decimal TotalDebt { get; set; }
        public string TotalIncomeFormatted => $"{TotalIncome:N2} руб.";
        public string TotalDebtFormatted => $"{TotalDebt:N2} руб.";
        public string AveragePaymentFormatted => $"{AveragePayment:N2} руб.";
    }

    public class PaymentDetail
    {
        public int Id { get; set; }
        public string StudentName { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentType { get; set; }
        public string PaymentDateString => PaymentDate.ToString("dd.MM.yyyy");
        public string AmountFormatted => $"{Amount:N2} руб.";
    }

    public class MonthlyBreakdown
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; }
        public decimal Amount { get; set; }
        public int Count { get; set; }
        public string AmountFormatted => $"{Amount:N2} руб.";
    }

    public class DebtorInfo
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string GroupName { get; set; }
        public decimal DebtAmount { get; set; }
        public DateTime? LastPaymentDate { get; set; }
        public string DebtFormatted => $"{DebtAmount:N2} руб.";
        public string LastPaymentFormatted => LastPaymentDate?.ToString("dd.MM.yyyy") ?? "Нет платежей";
    }

    public class StudentFinancialInfo
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string GroupName { get; set; }
        public decimal TotalToPay { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal Debt { get; set; }
        public int PaymentProgress { get; set; }
        public string PaymentStatus { get; set; }
        public string TotalToPayFormatted => $"{TotalToPay:N2} руб.";
        public string TotalPaidFormatted => $"{TotalPaid:N2} руб.";
        public string DebtFormatted => $"{Debt:N2} руб.";
    }

    public class GroupFinancialInfo
    {
        public string GroupName { get; set; }
        public int StudentCount { get; set; }
        public decimal ExpectedIncome { get; set; }
        public decimal ActualIncome { get; set; }
        public decimal Debt { get; set; }
        public decimal CompletionRate { get; set; }
        public string ExpectedIncomeFormatted => $"{ExpectedIncome:N2} руб.";
        public string ActualIncomeFormatted => $"{ActualIncome:N2} руб.";
        public string DebtFormatted => $"{Debt:N2} руб.";
        public string CompletionRateFormatted => $"{CompletionRate:N1}%";
    }

    public class DemographicReport
    {
        public int TotalStudents { get; set; }
        public int MaleCount { get; set; }
        public int FemaleCount { get; set; }
        public double AverageAge { get; set; }
        public Dictionary<string, int> AgeDistribution { get; set; }
    }

    public class StudentDocumentStatus
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string GroupName { get; set; }
        public bool HasPassport { get; set; }
        public bool HasSNILS { get; set; }
        public bool HasMedical { get; set; }
        public bool HasAddress { get; set; }
        public int DocumentsCount { get; set; }
        public string PassportStatus => HasPassport ? "✅" : "❌";
        public string SNILSStatus => HasSNILS ? "✅" : "❌";
        public string MedicalStatus => HasMedical ? "✅" : "❌";
        public string AddressStatus => HasAddress ? "✅" : "❌";
        public string OverallStatus => DocumentsCount >= 4 ? "Полный комплект" : $"Не хватает {4 - DocumentsCount}";
    }

    public class ExpiringDocumentInfo
    {
        public string StudentName { get; set; }
        public string DocumentType { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int DaysLeft { get; set; }
        public string ExpiryDateString => ExpiryDate.ToString("dd.MM.yyyy");
    }
}