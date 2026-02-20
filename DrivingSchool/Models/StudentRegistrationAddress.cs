using System;

namespace DrivingSchool.Models
{
    public class StudentRegistrationAddress
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string Region { get; set; }
        public string City { get; set; }
        public string Street { get; set; }
        public string House { get; set; }
        public string Building { get; set; }
        public string Apartment { get; set; }
        public string PostalCode { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        // Полный адрес
        public string FullAddress
        {
            get
            {
                var address = $"{City}, {Street} {House}";
                if (!string.IsNullOrWhiteSpace(Building))
                    address += $", корп.{Building}";
                if (!string.IsNullOrWhiteSpace(Apartment))
                    address += $", кв.{Apartment}";
                if (!string.IsNullOrWhiteSpace(PostalCode))
                    address = $"{PostalCode}, {address}";
                return address;
            }
        }

        public string StudentName { get; set; }
        public string ShortAddress => $"{City}, {Street} {House}";
    }

    public class StudentRegistrationAddressCollection
    {
        public System.Collections.Generic.List<StudentRegistrationAddress> Addresses { get; set; } = new System.Collections.Generic.List<StudentRegistrationAddress>();
    }
}