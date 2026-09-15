//06. Branch wise PIV Tabulation ( Both Bank and POS) Report

using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.PIV
{
    public class BranchWisePivBothModel
    {
        public string Issued_cost_center { get; set; }
        public string Piv_no { get; set; }
        public DateTime? Piv_date { get; set; }
        public DateTime? Paid_date { get; set; }
        public string Payment_mode { get; set; }
        public decimal Grand_total { get; set; }
        public string Account_code { get; set; }
        public decimal Amount { get; set; }
        public string Bank_check_no { get; set; }
        public string Issued_cc_name { get; set; }
        public string Company_name { get; set; }
    }
}