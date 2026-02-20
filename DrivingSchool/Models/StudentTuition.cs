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

        // Вычисляемая итоговая сумма
        public decimal FinalAmount => FullAmount - Discount;

        public string StudentName { get; set; }
        public string TariffName { get; set; }
    }

    public class StudentTuitionCollection
    {
        public System.Collections.Generic.List<StudentTuition> Tuitions { get; set; } = new System.Collections.Generic.List<StudentTuition>();
    }
}