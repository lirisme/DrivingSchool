using System;
using System.Collections.Generic;

namespace DrivingSchool.Models
{
    public class DrivingLesson
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int InstructorId { get; set; }
        public int CarId { get; set; }
        public int? SlotId { get; set; }
        public DateTime LessonDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Status { get; set; } // Booked, Completed, Cancelled, NoShow
        public DateTime? CanceledAt { get; set; }
        public bool IsCancelledByStudent { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsExtra { get; set; } // ДОБАВЬТЕ ЭТУ СТРОКУ - для дополнительных уроков

        // Навигационные свойства
        public string StudentName { get; set; }
        public string InstructorName { get; set; }
        public string CarInfo { get; set; }

        public string StatusDisplay
        {
            get
            {
                switch (Status)
                {
                    case "Booked": return IsExtra ? "Доп. забронирован" : "Забронирован";
                    case "Completed": return IsExtra ? "Доп. проведен" : "Проведен";
                    case "Cancelled": return IsExtra ? "Доп. отменен" : "Отменен";
                    case "NoShow": return IsExtra ? "Доп. прогул" : "Пропущен";
                    default: return Status;
                }
            }
        }

        private List<string> _currentTimeSlots = new List<string>();
    }

    public class DrivingLessonCollection
    {
        public List<DrivingLesson> Lessons { get; set; }
    }
}