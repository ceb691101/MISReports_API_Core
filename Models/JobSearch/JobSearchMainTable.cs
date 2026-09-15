using System;

namespace MISReports_Api.Models
{
    public class JobSearchMainTable
    {
        public string ApplicationId { get; set; }
        public string ApplicationNo { get; set; }
        public string IdNo { get; set; }
        public string ProjectNo { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string StreetAddress { get; set; }
        public string Suburb { get; set; }
        public string City { get; set; }

        public string ApplicationTypeDesc { get; set; }

        public string DeptId { get; set; }              // MAX(wld.dept_id)
        public string ExistingAccNo { get; set; }       // MAX(wld.existing_acc_no)

        public string TelephoneNo { get; set; }
        public string MobileNo { get; set; }

        public DateTime? SubmitDate { get; set; }
    }
}