using System;
using System.Collections.Generic;

namespace DrivingSchool.Models
{
    public class LessonSlot
    {
        public int Id { get; set; }
        public int InstructorId { get; set; }
        public int CarId { get; set; }
        public DateTime LessonDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsAvailable { get; set; }
        public DateTime CreatedDate { get; set; }

        public string InstructorName { get; set; }
        public string CarInfo { get; set; }
        public string TimeDisplay => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
        public string DateDisplay => LessonDate.ToString("dd.MM.yyyy");
    }

    public class LessonSlotCollection
    {
        public List<LessonSlot> Slots { get; set; }
    }
}