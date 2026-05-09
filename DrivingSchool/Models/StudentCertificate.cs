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
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        public string StudentName { get; set; }
        public string FullNumber => $"{CertificateSeries} {CertificateNumber}";
        public string CategoryCode { get; set; }
        public string CategoryName { get; set; }
        public string CategoryDisplay => $"{CategoryCode} - {CategoryName}";
    }

    public class StudentCertificateCollection
    {
        public System.Collections.Generic.List<StudentCertificate> Certificates { get; set; } = new System.Collections.Generic.List<StudentCertificate>();
    }
}