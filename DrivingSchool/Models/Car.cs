using System;

namespace DrivingSchool.Models
{
    public class Car
    {
        public int Id { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string LicensePlate { get; set; }
        public int Year { get; set; }
        public string Color { get; set; }
        public string Category { get; set; }
        public string VIN { get; set; }
        public int InstructorId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // Для отображения
        public string DisplayText => $"{Brand} {Model} ({LicensePlate})";
        public string FullInfo => $"{Brand} {Model}, {Year} г., {Color}, {LicensePlate}";
        public string InstructorName { get; set; }
    }

    public class CarCollection
    {
        public System.Collections.Generic.List<Car> Cars { get; set; } = new System.Collections.Generic.List<Car>();
    }
}