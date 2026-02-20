using System;

namespace DrivingSchool.Models
{
    public class StudentPassportData
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string DocumentType { get; set; }
        public string Series { get; set; }
        public string Number { get; set; }
        public string IssuedBy { get; set; }
        public string DivisionCode { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // Для отображения
        public string FullNumber => $"{Series} {Number}";
        public string StudentName { get; set; }
    }

    public class StudentPassportDataCollection
    {
        public System.Collections.Generic.List<StudentPassportData> Passports { get; set; } = new System.Collections.Generic.List<StudentPassportData>();
    }
}