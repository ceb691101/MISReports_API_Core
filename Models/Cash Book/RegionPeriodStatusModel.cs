using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class RegionPeriodStatusModel
    {
        public string DeptId { get; set; }
        public string DeptNm { get; set; }     // Cost Center
        public string CompId { get; set; }     // Branch
        public string FinYear { get; set; }
        public string FinPrd { get; set; }
        public string Status { get; set; }     // Period Status 
        public string CompNm { get; set; }     // Region/Division name
    }
}