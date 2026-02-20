using System;
using System.Collections.Generic;

namespace DrivingSchool.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Position { get; set; }
        public string Status { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public DateTime HireDate { get; set; }
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

        // Вычисляемый стаж
        public int Experience
        {
            get
            {
                var today = DateTime.Today;
                var experience = today.Year - HireDate.Year;
                if (HireDate.Date > today.AddYears(-experience)) experience--;
                return experience;
            }
        }

        // Короткое имя для отображения (Иванов И.И.)
        public string ShortName
        {
            get
            {
                var shortName = LastName;
                if (!string.IsNullOrWhiteSpace(FirstName))
                    shortName += $" {FirstName[0]}.";
                if (!string.IsNullOrWhiteSpace(MiddleName))
                    shortName += $" {MiddleName[0]}.";
                return shortName;
            }
        }
    }

    public class EmployeeCollection
    {
        public List<Employee> Employees { get; set; } = new List<Employee>();
    }
}