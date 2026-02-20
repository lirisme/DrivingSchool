using System;
using System.Linq;

namespace DrivingSchool.Models
{
    public class StudentSNILS
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string Number { get; set; }
        public DateTime? IssueDate { get; set; }
        public string IssuedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // Для отображения
        public string StudentName { get; set; }
        public string FormattedNumber
        {
            get
            {
                if (string.IsNullOrEmpty(Number)) return "";
                // Формат: XXX-XXX-XXX XX
                var digits = new string(Number.Where(char.IsDigit).ToArray());
                if (digits.Length == 11)
                {
                    return $"{digits.Substring(0, 3)}-{digits.Substring(3, 3)}-{digits.Substring(6, 3)} {digits.Substring(9, 2)}";
                }
                return Number;
            }
        }
    }

    public class StudentSNILSCollection
    {
        public System.Collections.Generic.List<StudentSNILS> SNILSList { get; set; } = new System.Collections.Generic.List<StudentSNILS>();
    }
}