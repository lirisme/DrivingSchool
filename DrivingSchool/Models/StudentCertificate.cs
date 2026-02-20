using System;

namespace DrivingSchool.Models
{
    public class StudentCertificate
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string CertificateSeries { get; set; }
        public string CertificateNumber { get; set; }
        public DateTime IssueDate { get; set; }
        public int VehicleCategoryId { get; set; }
        public string CategoryCode { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // Для отображения
        public string StudentName { get; set; }
        public string FullNumber => $"{CertificateSeries} {CertificateNumber}";
        public string CategoryDisplay => $"{CategoryCode} - {CategoryName}";
        public string CategoryName { get; set; }
    }

    public class StudentCertificateCollection
    {
        public System.Collections.Generic.List<StudentCertificate> Certificates { get; set; } = new System.Collections.Generic.List<StudentCertificate>();
    }
}