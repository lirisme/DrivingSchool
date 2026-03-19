using System;
using System.Collections.Generic;

namespace DrivingSchool.Models
{
    public class Student
    {
        public int Id { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public DateTime BirthDate { get; set; }
        public string BirthPlace { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Citizenship { get; set; }
        public string Gender { get; set; }
        public int GroupId { get; set; }
        public int VehicleCategoryId { get; set; }
        public int InstructorId { get; set; }
        public int CarId { get; set; }

        // НОВЫЕ ПОЛЯ для стоимости обучения
        public decimal TuitionAmount { get; set; } // Полная стоимость
        public decimal DiscountAmount { get; set; } // Сумма скидки
        public int? TariffId { get; set; } // ID тарифа (если используется)

        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // Вычисляемое полное имя
        public string FullName
        {
            get
            {
                var name = $"{LastName} {FirstName}";
                if (!string.IsNullOrWhiteSpace(MiddleName))
                    name += $" {MiddleName}";
                return name;
            }
        }

        // Вычисляемый возраст
        public int Age
        {
            get
            {
                var today = DateTime.Today;
                var age = today.Year - BirthDate.Year;
                if (BirthDate.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        // ВЫЧИСЛЯЕМЫЕ ПОЛЯ ДЛЯ ОПЛАТЫ
        public decimal FinalAmount => TuitionAmount - DiscountAmount; // Итоговая сумма к оплате

        // Эти поля будут заполняться отдельно из платежей
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount => FinalAmount - PaidAmount;

        public int PaymentProgress
        {
            get
            {
                if (FinalAmount == 0) return 0;
                var progress = (int)((PaidAmount / FinalAmount) * 100);
                return Math.Min(100, Math.Max(0, progress));
            }
        }

        public string PaymentStatus
        {
            get
            {
                if (FinalAmount == 0) return "Не установлена";
                if (RemainingAmount <= 0) return "Оплачено полностью";
                if (PaidAmount == 0) return "Не оплачено";
                return $"Осталось: {RemainingAmount:N2} руб.";
            }
        }

        // Дополнительные поля для отображения в интерфейсе
        public string CategoryCode { get; set; }
        public string CategoryName { get; set; }
        public string GroupName { get; set; }
        public string InstructorName { get; set; }
        public string CarInfo { get; set; }
        public string TariffName { get; set; }

        // Свойства для документов (будут заполняться отдельно)
        public bool HasPassport { get; set; }
        public bool HasSNILS { get; set; }
        public bool HasMedical { get; set; }
        public bool HasAddress { get; set; }
        public bool HasCertificate { get; set; }
        public bool HasDrivingLicense { get; set; }

        // Статус документов
        public string DocumentsStatus
        {
            get
            {
                var total = 0;
                if (HasPassport) total++;
                if (HasSNILS) total++;
                if (HasMedical) total++;
                if (HasAddress) total++;
                if (HasCertificate) total++;
                if (HasDrivingLicense) total++;

                return $"{total}/6";
            }
        }

        public string DocumentsColor
        {
            get
            {
                var total = 0;
                if (HasPassport) total++;
                if (HasSNILS) total++;
                if (HasMedical) total++;
                if (HasAddress) total++;
                if (HasCertificate) total++;
                if (HasDrivingLicense) total++;

                return total == 6 ? "Green" : total >= 4 ? "Orange" : "Red";
            }
        }
    }

    public class StudentCollection
    {
        public List<Student> Students { get; set; } = new List<Student>();
    }
}