using System;
using System.Collections.Generic;
using System.Linq;
namespace MISReports_Api.Models.Accounts
{
    public class CCDocInquiryPendingModel
    {
        public string Category { get; set; }
        public string DeptId { get; set; }
        public string DocNo { get; set; }
        public DateTime? DocDt { get; set; }
        public string TranStatus { get; set; }
        public string CctName { get; set; }
    }
}