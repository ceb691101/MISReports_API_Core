using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class IssueSummaryProvinceModel
    {
        public string MatCd { get; set; }
        public string MatNm { get; set; }
        public string DeptCompName { get; set; }
        public string DeptId { get; set; }
        public decimal? CommitedQty { get; set; }
        public string CompName { get; set; }
    }
}