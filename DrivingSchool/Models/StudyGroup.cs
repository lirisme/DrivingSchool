using System;

namespace DrivingSchool.Models
{
    public class StudyGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Duration { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // Количество студентов (заполняется отдельно)
        public int StudentCount { get; set; }

        // Для отображения
        public string Period => $"{StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}";
        public string DisplayText => $"{Name} ({Status})";
    }

    public class StudyGroupCollection
    {
        public System.Collections.Generic.List<StudyGroup> Groups { get; set; } = new System.Collections.Generic.List<StudyGroup>();
    }
}