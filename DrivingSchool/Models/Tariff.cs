using System;
using System.Collections.Generic;

namespace DrivingSchool.Models
{
    public class Tariff
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public int DurationMonths { get; set; }
        public int PracticeHours { get; set; }
        public decimal BaseCost { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // Для отображения
        public string DisplayText => $"{Name} - {BaseCost:N0} руб.";
    }

    public class TariffCollection
    {
        public List<Tariff> Tariffs { get; set; } = new List<Tariff>();
    }
}