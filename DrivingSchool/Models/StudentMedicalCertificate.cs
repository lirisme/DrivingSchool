using System;

namespace DrivingSchool.Models
{
    public class StudentMedicalCertificate
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string Series { get; set; }
        public string Number { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ValidUntil { get; set; }
        public string MedicalInstitution { get; set; }
        public string Categories { get; set; }
        public string Region { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // Для отображения
        public string FullNumber => $"{Series} {Number}";
        public bool IsValid => ValidUntil >= DateTime.Today;
        public string StudentName { get; set; }
        public string StatusText => IsValid ? "Действительна" : "Просрочена";
        public string StatusColor => IsValid ? "Green" : "Red";
    }

    public class StudentMedicalCertificateCollection
    {
        public System.Collections.Generic.List<StudentMedicalCertificate> Certificates { get; set; } = new System.Collections.Generic.List<StudentMedicalCertificate>();
    }
}