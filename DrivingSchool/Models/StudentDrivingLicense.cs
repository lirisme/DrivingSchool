using System;

namespace DrivingSchool.Models
{
    public class StudentDrivingLicense
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string Series { get; set; }
        public string Number { get; set; }
        public string LicenseCateg { get; set; }
        public string IssuedBy { get; set; }
        public string DivisionCode { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int ExperienceYears { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // Для отображения
        public string StudentName { get; set; }
        public string FullNumber => $"{Series} {Number}";
        public bool IsValid => ExpiryDate >= DateTime.Today;
        public string StatusText => IsValid ? "Действительно" : "Просрочено";
        public string StatusColor => IsValid ? "Green" : "Red";
        public string CategoriesDisplay => LicenseCateg?.Replace(",", ", ") ?? "";
    }

    public class StudentDrivingLicenseCollection
    {
        public System.Collections.Generic.List<StudentDrivingLicense> Licenses { get; set; } = new System.Collections.Generic.List<StudentDrivingLicense>();
    }
}