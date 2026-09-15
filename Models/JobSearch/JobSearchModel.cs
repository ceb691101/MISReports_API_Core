using System;

namespace MISReports_Api.Models
{
    public class JobSearchModel
    {
        public string ApplicationId { get; set; }
        public string ApplicationNo { get; set; }
        public string ProjectNo { get; set; }
        public string IdNo { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string StreetAddress { get; set; }
        public string Suburb { get; set; }
        public string City { get; set; }
        public string ApplicationTypeDesc { get; set; }
        public DateTime? SubmitDate { get; set; }
        public string Status { get; set; }
        public string CustomerStatus { get; set; }
        public string ApplicationStatus { get; set; }
        public string Telephone { get; set; }
        public string Mobile { get; set; }
    }
}