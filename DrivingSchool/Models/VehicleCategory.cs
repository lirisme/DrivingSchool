using System;
using System.Collections.Generic;

namespace DrivingSchool.Models
{
    public class VehicleCategory
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string FullName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // Для отображения в ComboBox
        public string DisplayText => $"{Code} - {FullName}";
    }

    public class VehicleCategoryCollection
    {
        public List<VehicleCategory> Categories { get; set; } = new List<VehicleCategory>();
    }
}