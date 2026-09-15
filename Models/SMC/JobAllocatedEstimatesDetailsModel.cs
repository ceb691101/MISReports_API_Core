using System;
using System.Collections.Generic;
using System.Linq;

namespace MISReports_Api.Models.Accounts
{
    public class JobAllocatedEstimatesDetailsModel
    {
        public string MatCode { get; set; }
        public decimal? EstQty { get; set; }
        public string MatName { get; set; }
        public decimal? QtyOnHand { get; set; }
        public string CctName { get; set; }
    }
}