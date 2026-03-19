using System;

namespace DrivingSchool.Models
{
    public class StudentTuition
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int? TariffId { get; set; }
        public decimal FullAmount { get; set; }
        public decimal Discount { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // Дополнительные поля для отображения
        public string StudentName { get; set; }
        public string TariffName { get; set; }

        // ВЫЧИСЛЯЕМОЕ свойство - правильный расчет итоговой суммы
        public decimal FinalAmount => FullAmount - Discount;

        // Статус оплаты (будет заполняться отдельно)
        public decimal PaidAmount { get; set; }
        public decimal DebtAmount => FinalAmount - PaidAmount;
        public string DebtStatus
        {
            get
            {
                if (FinalAmount == 0) return "Нет стоимости";
                if (DebtAmount > 0) return "Долг";
                if (DebtAmount < 0) return "Переплата";
                return "Оплачено полностью";
            }
        }
    }

    public class StudentTuitionCollection
    {
        public System.Collections.Generic.List<StudentTuition> Tuitions { get; set; } =
            new System.Collections.Generic.List<StudentTuition>();
    }
}