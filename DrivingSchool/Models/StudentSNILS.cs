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

        public string StudentName { get; set; }

        public string FormattedNumber
        {
            get
            {
                if (string.IsNullOrEmpty(Number)) return "";
                var digits = new string(Number.Where(char.IsDigit).ToArray());
                if (digits.Length == 11)
                {
                    return $"{digits.Substring(0, 3)}-{digits.Substring(3, 3)}-{digits.Substring(6, 3)} {digits.Substring(9, 2)}";
                }
                return Number;
            }
        }

        // Проверка контрольной суммы СНИЛС
        public bool IsValid
        {
            get
            {
                var digits = new string(Number.Where(char.IsDigit).ToArray());
                if (digits.Length != 11) return false;
                if (digits.All(c => c == digits[0])) return false;

                int sum = 0;
                for (int i = 0; i < 9; i++)
                {
                    sum += (digits[i] - '0') * (9 - i);
                }

                int checkDigit;
                if (sum < 100) checkDigit = sum;
                else if (sum == 100 || sum == 101) checkDigit = 0;
                else
                {
                    checkDigit = sum % 101;
                    if (checkDigit == 100 || checkDigit == 101) checkDigit = 0;
                }

                int actualCheckDigit = int.Parse(digits.Substring(9, 2));
                return checkDigit == actualCheckDigit;
            }
        }
    }

    public class StudentSNILSCollection
    {
        public System.Collections.Generic.List<StudentSNILS> SNILSList { get; set; } = new System.Collections.Generic.List<StudentSNILS>();
    }
}