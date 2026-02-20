using System;

namespace DrivingSchool.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentType { get; set; }
        public DateTime CreatedDate { get; set; }

        public string StudentName { get; set; }
        public string FormattedAmount => $"{Amount:N2} руб.";
    }

    public class PaymentCollection
    {
        public System.Collections.Generic.List<Payment> Payments { get; set; } = new System.Collections.Generic.List<Payment>();
    }
}