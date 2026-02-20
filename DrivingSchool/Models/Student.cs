using System;

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

        // Дополнительные поля для отображения в интерфейсе
        public string CategoryCode { get; set; }
        public string CategoryName { get; set; }
        public string GroupName { get; set; }
        public string InstructorName { get; set; }
        public string CarInfo { get; set; }
    }

    public class StudentCollection
    {
        public System.Collections.Generic.List<Student> Students { get; set; } = new System.Collections.Generic.List<Student>();
    }
}